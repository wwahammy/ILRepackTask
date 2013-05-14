using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILRepacking;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace ILRepackTask
{
// ReSharper disable InconsistentNaming
    public class ILRepackTask : Task

    {
        private ILRepack ILMerger;
        private ITaskItem[] m_assemblies = new ITaskItem[0];
        private string m_attributeFile;
        private bool m_closed;
        private bool m_copyAttributes;
        private bool m_debugInfo = true;
        private string m_excludeFile;
        private bool m_internalize;
        private string m_keyFile;
        private string[] m_libraryPath = new string[0];
        private bool m_log;
        private string m_logFile;
        private string m_outputFile;
        private ILRepack.Kind m_targetKind;
        private bool m_parallel = true;
        private bool m_xmlDocumentation;
        // ReSharper restore InconsistentNaming
        public virtual string AttributeFile
        {
            get { return m_attributeFile; }
            set { m_attributeFile = BuildPath(ConvertEmptyToNull(value)); }
        }

        public virtual bool Closed
        {
            get { return m_closed; }
            set { m_closed = value; }
        }

        public virtual bool CopyAttributes
        {
            get { return m_copyAttributes; }
            set { m_copyAttributes = value; }
        }

        public virtual bool DebugInfo
        {
            get { return m_debugInfo; }
            set { m_debugInfo = value; }
        }

        public virtual string ExcludeFile
        {
            get { return m_excludeFile; }
            set { m_excludeFile = BuildPath(ConvertEmptyToNull(value)); }
        }

        public virtual bool Internalize
        {
            get { return m_internalize; }
            set { m_internalize = value; }
        }

        public virtual string[] LibraryPath
        {
            get { return m_libraryPath; }
            set { m_libraryPath = value; }
        }

        public virtual bool ShouldLog
        {
            get { return m_log; }
            set { m_log = value; }
        }

        public virtual string LogFile
        {
            get { return m_logFile; }
            set { m_logFile = BuildPath(ConvertEmptyToNull(value)); }
        }

        [Required]
        public virtual string OutputFile
        {
            get { return m_outputFile; }
            set { m_outputFile = BuildPath(ConvertEmptyToNull(value)); }
        }

        public virtual bool Parallel
        {
            get { return m_parallel; }
            set { m_parallel = value; }
        }

        public virtual string SnkFile
        {
            get { return m_keyFile; }
            set { m_keyFile = BuildPath(ConvertEmptyToNull(value)); }
        }

        public virtual bool XmlDocumentation
        {
            get { return m_xmlDocumentation; }
            set { m_xmlDocumentation = value; }
        }

        [Required]
        public virtual ITaskItem[] InputAssemblies
        {
            get { return m_assemblies; }
            set { m_assemblies = value; }
        }

        public virtual string TargetKind
        {
            get { return m_targetKind.ToString(); }
            set
            {
                if (Enum.IsDefined(typeof (ILRepack.Kind), value))
                {
                    m_targetKind = (ILRepack.Kind) Enum.Parse(typeof (ILRepack.Kind), value);
                }
                else
                {
                    Log.LogWarning(
                        "TargetKind should be [Exe|Dll|WinExe|SameAsPrimaryAssembly]; set to SameAsPrimaryAssembly");
                    m_targetKind = ILRepack.Kind.SameAsPrimaryAssembly;
                }
            }
        }

        public override bool Execute()
        {
            ILMerger = new ILRepack
                           {
                               AttributeFile = m_attributeFile,
                               Closed = m_closed,
                               CopyAttributes = m_copyAttributes,
                               DebugInfo = m_debugInfo,
                               ExcludeFile = m_excludeFile,
                               Internalize = m_internalize,
                               LogFile = m_logFile,
                               Log = m_log,
                               OutputFile = m_outputFile,
                               KeyFile = m_keyFile,
                               TargetKind = m_targetKind,
                               Parallel = m_parallel,
                               XmlDocumentation =  m_xmlDocumentation
                           };


            ILMerger.SetInputAssemblies(GetUniqueAssemblies(GetAssembliesFromFilePaths(m_assemblies.Select(a => a.ItemSpec))).ToArray());

            IEnumerable<string> searchPath = new[] {"."}.Concat(LibraryPath.Select(BuildPath));

            ILMerger.SetSearchDirectories(searchPath.ToArray());

            try
            {
                Log.LogMessage(MessageImportance.Normal, "Merging {0} assembl{1} to '{2}'.", m_assemblies.Length,
                               (m_assemblies.Length != 1) ? "ies" : "y", m_outputFile);
                Log.LogMessage(MessageImportance.Normal, "Using parallel: {0}", m_parallel);
                ILMerger.Merge();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                return false;
            }

            return true;
        }

        private static string ConvertEmptyToNull(string iti)
        {
            return string.IsNullOrEmpty(iti) ? null : iti;
        }

        private string BuildPath(string iti)
        {
            return string.IsNullOrEmpty(iti)
                       ? null
                       : Path.Combine(BuildEngine.ProjectFileOfTaskNode, iti);
        }


        internal IEnumerable<string> GetUniqueAssemblies(IEnumerable<AssemblyNameReferenceWithPath> assemblies)
        {
            return assemblies.Distinct(new Comparer()).Select(i => i.Path);
        }

        internal IEnumerable<AssemblyNameReferenceWithPath> GetAssembliesFromFilePaths(IEnumerable<string> paths)
        {
            foreach (var p in paths)
            {
                AssemblyDefinition assmDef = null;
                try
                {
                    using (var s = File.OpenRead(p))
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
                    yield return new AssemblyNameReferenceWithPath { Name = assmDef.FullName, Path = p };
                }
            }
        }

        private class Comparer : IEqualityComparer<AssemblyNameReferenceWithPath>
        {
            public bool Equals(AssemblyNameReferenceWithPath x, AssemblyNameReferenceWithPath y)
            {
                return x.Name == y.Name;
            }

            public int GetHashCode(AssemblyNameReferenceWithPath obj)
            {
                return obj.Name.GetHashCode();
            }
        }


    }

    internal class AssemblyNameReferenceWithPath
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

}