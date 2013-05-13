using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Text;
using ILRepackTask;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Xunit;

namespace ILRepackTests
{
    public class Tests : IDisposable
    {
        readonly DirectoryInfo _testDestFolder = new DirectoryInfo(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "TEST"));
        DirectoryInfo _testSourceFolder = new DirectoryInfo(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "BasicSampleOutput"));
        private IFileSystem _fileSystem = null;
        public Tests()
        {

/*
            _fileSystem =
                CreateFileSystem(new []
                                     {
                                         "BasicSample.dll", "BasicSample.pdb", "Microsoft.Practices.ServiceLocation.dll",
                                         "Microsoft.Practices.ServiceLocation.pdb"
                                     });

            */
        }

        private static MockFileSystem CreateFileSystem(IEnumerable<string> fileNames)
        {
            var output = new Dictionary<string, MockFileData>();
            foreach (var f in fileNames)
            {
                output[@"C:\" + f] =
                    new MockFileData(GetByteArrayFromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("ILRepackTests.BasicSampleOutput." + f)));
            }

            return new MockFileSystem(output);
        }

        private static byte[] GetByteArrayFromStream(Stream s)
        {
            var ret = new MemoryStream();
            s.CopyTo(ret);
            return ret.ToArray();
        }

        [Fact]
        public void BasicTestCreate()
        {
            var basicSampleTaskItem = new TaskItem("BasicSample");
            var cSharpTaskItem = new TaskItem("Microsoft.CSharp");
            var serviceTaskItem = new TaskItem(
                "Microsoft.Practices.ServiceLocation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");


            var task = new FindAssembliesInOutputDirTask(_fileSystem)
                           {
                               InputAssemblies = new ITaskItem[]
                                                     {
                                                         basicSampleTaskItem,
                                                         cSharpTaskItem,
                                                         serviceTaskItem
                                                     },
                               OutputDir = new[]
                                               {
                                                  @"C:\"
                                               }
                           };

            Assert.True(task.Execute());
            var outputs = task.AssembliesInOutputDir;
            Assert.Contains(basicSampleTaskItem, outputs);
            Assert.Contains(serviceTaskItem, outputs);
            Assert.DoesNotContain(cSharpTaskItem, outputs);

        }

        [Fact]
        private void ComparerTest()
        {
            var name = new AssemblyNameReferenceWithPath
                {
                    Name = 
                        "Microsoft.Practices.ServiceLocation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    Path = @"C:\1.dll"
                };

            var name1 = new AssemblyNameReferenceWithPath
                {
                    Name =
                        "Microsoft.Practices.ServiceLocation, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35",
                    Path = @"C:\2.dll"
                };

            var task = new ILRepackTask.ILRepackTask();
            var res = task.GetUniqueAssemblies(new[] {name, name1}).ToArray();
            Assert.Equal(1, res.Length);



        }
        

        public void Dispose()
        {
            
        }
    }
}
