using System;
using System.Collections.Generic;

using System.IO.Abstractions;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace ILRepackTask
{
    public class FindAssembliesInOutputDirTask : Task
    {
        private readonly IFileSystem _fileSystem;

        [Required]
        public ITaskItem[] InputAssemblies { get; set; }

        [Required]
        public string OutputAssemblyName { get; set; }

        [Required]
        public string[] OutputDir { get; set; }

        [Output]
        public ITaskItem[] AssembliesInOutputDir { get; set; }

        private TaskLoggingHelper _log;

        public FindAssembliesInOutputDirTask() : this(new FileSystem())
        {
            

        }

        /// <summary>
        /// Used primarily for Testing
        /// </summary>
        /// <param name="fileSystem"></param>
        public FindAssembliesInOutputDirTask(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public override bool Execute()
        {
            SetupLogAsNecessary();
            
            var assemblyFiles =
                OutputDir.SelectMany(
                    p => _fileSystem.Directory.GetFiles(p, "*.dll").Concat(_fileSystem.Directory.GetFiles(p, "*.exe")));
            
            var allAssembliesInOutput = GetAssembliesFromFilePaths(assemblyFiles);

            var referencedAssembliesInOutputDir = CutDownInputAssembliesInOutput(allAssembliesInOutput);
            var list = referencedAssembliesInOutputDir.Select(a => new TaskItem(a.Path)).Cast<ITaskItem>().ToList();
            list.Add(new TaskItem(OutputAssemblyName));
            AssembliesInOutputDir = list.ToArray();
            return !_log.HasLoggedErrors;
        }

        private void SetupLogAsNecessary()
        {
            _log = Log ?? new TaskLoggingHelper(this);
        }

        private IEnumerable<AssemblyDefinitionWithPath> CutDownInputAssembliesInOutput(IEnumerable<AssemblyDefinitionWithPath> validAssemblies)
        {
            var assemblyDefinitions = validAssemblies as AssemblyDefinitionWithPath[] ?? validAssemblies.ToArray();
            foreach (var a in InputAssemblies)
            {
                var assemblyName = GetAssemblyNameReference(a.ItemSpec);
                if (assemblyName != null)
                {
                    var result =
                        assemblyDefinitions.FirstOrDefault(
                            validAssm => FuzzyMatchAssemblyNames(assemblyName, validAssm.Definition.Name));
                    if (result != null)
                    {
                        yield return result;
                    }
                }

            }
            
        }

        private IEnumerable<AssemblyDefinitionWithPath> GetAssembliesFromFilePaths(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                AssemblyDefinition assmDef = null;
                try
                {
                    using (var s = _fileSystem.File.OpenRead(p))
                    {
                        assmDef = AssemblyDefinition.ReadAssembly(s);
                    }
                }
// ReSharper disable EmptyGeneralCatchClause
                catch
// ReSharper restore EmptyGeneralCatchClause
                {
                    
                }

                if (assmDef != null)
                {
                    yield return new AssemblyDefinitionWithPath { Definition = assmDef, Path = p };
                }
            }
        }

       private AssemblyNameReference GetAssemblyNameReference(string fullName)
       {
           try
           {
               return AssemblyNameReference.Parse(fullName);
               
           }
           catch (Exception)
           {
               _log.LogError(String.Format("We couldn't parse an AssemblyNameReference from {0}", fullName));
               return null;
           }
       
       }

       private bool FuzzyMatchAssemblyNames(AssemblyNameReference one, AssemblyNameReference two)
       {
           var namesEqual = one.Name == two.Name;
           var pkt = one.PublicKeyToken == null || !one.PublicKeyToken.Any()|| one.PublicKeyToken.SequenceEqual(two.PublicKeyToken);
           var versions = one.Version == null || one.Version == two.Version;
           var culture = VerifyIfCulturesAreEquivalent(one.Culture, two.Culture);

           return namesEqual &&
                  pkt && versions &&
                  culture;
       }

        public bool VerifyIfCulturesAreEquivalent(string first, string second)
        {
            var firstNeutral = String.IsNullOrWhiteSpace(first) || first == "neutral";

            var secondNeutral = String.IsNullOrWhiteSpace(first) || first == "neutral";

            if (firstNeutral && secondNeutral)
                return true;
            return first == second;
        }

        
    }

    internal class AssemblyDefinitionWithPath
    {
        public AssemblyDefinition Definition { get; set; }
        public string Path { get; set; }
    }
}
