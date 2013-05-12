using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace ILRepackTask
{
    public class GetMostQualifiedNamesForAssemblies : Task
    {
        private IFileSystem _fileSystem;
        public GetMostQualifiedNamesForAssemblies() : this(new FileSystem())
        {
            
        }

        internal GetMostQualifiedNamesForAssemblies(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        [Required]
        public ITaskItem[] AssemblyFiles { get; set; }

        [Output]
        public ITaskItem[] MostQualifiedNames { get; set; }

        public override bool Execute()
        {
            var assemblyFiles = AssemblyFiles.Select(a => _fileSystem.Path.GetFullPath(a.ItemSpec));
            var assmDefs = GetAssembliesFromFilePaths(assemblyFiles);
            MostQualifiedNames = assmDefs.Select(def => new TaskItem(def.Definition.FullName)).Cast<ITaskItem>().ToArray();
            return true;
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
    }
}
