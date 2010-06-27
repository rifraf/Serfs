using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SERFS {
    using MbUnit.Framework;

    [TestFixture]
    class TestSerfs {
        private Serfs _serfs;

        [SetUp]
        public void Setup() {
            _serfs = new Serfs("TestTemplates");
        }

        [Test]
        public void OpenValidFileGetsStream() {
            using (Stream stream = _serfs.OpenRead("test.txt")) {
                Assert.IsNotNull(stream);
            }
        }

        [Test]
        public void ReadValidFileGetsString() {
            string s = _serfs.Read("test.txt");
            Assert.AreEqual("Hello Serfs\r\n", s);
        }

        [Test]
        public void ReadTextValidFileGetsString() {
            string s = _serfs.ReadText("test.txt");
            Assert.AreEqual("Hello Serfs\n", s);
        }

        [Test]
        public void OpenInvalidFileGetsNull() {
            using (Stream stream = _serfs.OpenRead("not_here_test.txt")) {
                Assert.IsNull(stream);
            }
        }

        [Test]
        public void ReadInvalidFileGetsNull() {
            string s = _serfs.Read("not test.txt");
            Assert.IsNull(s);
        }

        [Test]
        public void ReadErfStream() {
            using (Stream stream = _serfs.OpenRead("test.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs", reader.ReadLine());
            }
        }

        [Test]
        public void ReadErfStreamCaseInsensitive() {
            using (Stream stream = _serfs.OpenRead("TeSt.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs", reader.ReadLine());
            }
        }

        [Test]
        public void ReadPathWithDotsAndSpaces() {
            using (Stream stream = _serfs.OpenRead("A folder with . and spaces/A file with . and spaces.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("I am a file with . and spaces.txt", reader.ReadLine());
            }
        }

        [Test]
        public void ReadFolderStartingWithNumeric() {
            using (Stream stream = _serfs.OpenRead("1.2.3/test.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("A test file in a folder with numerics", reader.ReadLine());
            }
        }

        [Test]
        public void ReadFileStartingWithNumeric() {
            using (Stream stream = _serfs.OpenRead("A folder with . and spaces/404.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("This file starts with a numeric", reader.ReadLine());
            }
        }

        [Test]
        public void ReadPathUsingBackslash() {
            using (Stream stream = _serfs.OpenRead("A folder with . and spaces\\A file with . and spaces.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("I am a file with . and spaces.txt", reader.ReadLine());
            }
        }

        [Test]
        public void UnmountedFoldersAreNotFound() {
            using (Stream stream = _serfs.OpenRead("moretest.txt")) {
                Assert.IsNull(stream);
            }
        }

        [Test]
        public void AdditionalMountedFoldersAreFound() {
            _serfs.Mount("MoreTemplates").Mount("ExtraTemplates");
            using (Stream stream = _serfs.OpenRead("moretest.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello again Serfs", reader.ReadLine());
            }
            using (Stream stream = _serfs.OpenRead("extratest.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello again extra Serfs", reader.ReadLine());
            }
        }

        [Test]
        public void AdditionalAssembliesAreFound() {
            _serfs.AddAssembly("ResourcesForSerfsTest", "ResourcesForSerfsTest").Mount("Files");
            using (Stream stream = _serfs.OpenRead("HelloSerfs.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs!", reader.ReadLine());
            }
        }

        [Test]
        public void AdditionalAssembliesWithDefaultPath() {
            _serfs.AddAssembly("ResourcesForSerfsTest");
            using (Stream stream = _serfs.OpenRead("Files/HelloSerfs.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs!", reader.ReadLine());
            }
        }

        [Test]
        public void AdditionalAssembliesNotFound() {
            Assert.IsFalse(_serfs.IgnoreMissingAssemblies);
            Assert.IsNull(_serfs.AddAssembly("AnAssemblyThatDoesNotExist"));
        }

        [Test]
        public void MissingAssembliesCanBeIgnored() {
            _serfs.IgnoreMissingAssemblies = true;
            Assert.IsTrue(_serfs.IgnoreMissingAssemblies);
            Assert.IsNotNull(_serfs.AddAssembly("AnAssemblyThatDoesNotExist"));
        }

        [Test]
        public void DuplicateAdditionalAssemblies() {
            _serfs.AddAssembly("ResourcesForSerfsTest");
            _serfs.AddAssembly("ResourcesForSerfsTest");
            using (Stream stream = _serfs.OpenRead("Files/HelloSerfs.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs!", reader.ReadLine());
            }
        }

        [Test]
        public void LeadingDotSlashIsOk() {
            string s = _serfs.Read("./test.txt");
            Assert.AreEqual("Hello Serfs\r\n", s);
        }

        [Test]
        public void LeadingDotBackSlashIsOk() {
            string s = _serfs.Read(".\\test.txt");
            Assert.AreEqual("Hello Serfs\r\n", s);
        }

        [Test]
        public void FilesCanBeOutsideFolders() {
            _serfs.Mount("/");
            string s = _serfs.Read("FileOutSideFolder.txt");
            Assert.StartsWith("I am FileOutSideFolder", s);
        }

        [Test]
        public void CanSpecifyAssemblyPrefixAndFolder() {
            Assembly resources = AppDomain.CurrentDomain.Load("ResourcesForSerfsTest");
            _serfs = new Serfs(resources, "ResourcesForSerfsTest", "Files");
            using (Stream stream = _serfs.OpenRead("HelloSerfs.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("Hello Serfs!", reader.ReadLine());
            }
        }

        [Test]
        public void CanUseDifferentDecoderForStream() {
            _serfs.Decoder = new UpCaseDecoder();
            using (Stream stream = _serfs.OpenRead("Test.txt")) {
                StreamReader reader = new StreamReader(stream);
                Assert.AreEqual("HELLO SERFS", reader.ReadLine());
            }
        }

        [Test]
        public void CanUseDifferentDecoderForString() {
            _serfs.Decoder = new UpCaseDecoder();
            string s = _serfs.Read("test.txt");
            Assert.AreEqual("HELLO SERFS\r\n", s);
        }

        private class UpCaseDecoder : IStreamDecoder {
            public Stream Decode(Stream stream) {
                StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd().ToUpperInvariant();
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            }
        }

        [Test]
        public void CanCheckIfFileExists() {
            Assert.IsTrue(_serfs.Exists(".\\test.txt"));
            Assert.IsFalse(_serfs.Exists(".\\test.text"));
            Assert.IsTrue(_serfs.Exists("/test.txt"));            
        }

        [Test]
        public void CanCheckIfFolderExists() {
            Assert.IsTrue(_serfs.FolderExists(".\\"));
            Assert.IsFalse(_serfs.FolderExists(".\\test.txt"));
            Assert.IsTrue(_serfs.FolderExists("/"));
            Assert.IsFalse(_serfs.FolderExists("/test.txt"));
            Assert.IsTrue(_serfs.FolderExists("/1.2.3"));
            Assert.IsTrue(_serfs.FolderExists("1.2.3"));
            Assert.IsTrue(_serfs.FolderExists("\\A folder with . and spaces"));
            Assert.IsTrue(_serfs.FolderExists("A folder with . and spaces"));
        }

        [Test]
        public void CanGetAllResourceNames() {
            _serfs.AddAssembly("ResourcesForSerfsTest", "ResourcesForSerfsTest").Mount("Files");
            string[] resources = _serfs.ResourceNames();

            Assert.AreEqual(5, resources.Length);
            Assert.Contains(resources, "test.txt");
            Assert.Contains(resources, "HelloSerfs.txt");
            Assert.Contains(resources, "_1._2._3.test.txt");
        }

        [Test]
        public void CanGetResourceNameSubset() {
            _serfs.AddAssembly("ResourcesForSerfsTest", "ResourcesForSerfsTest").Mount("Files");

            string[] resources = _serfs.ResourceNames("A folder with . and spaces");
            Assert.AreEqual(2, resources.Length);
            Assert.Contains(resources, "404.txt");
            Assert.Contains(resources, "A file with . and spaces.txt");

            resources = _serfs.ResourceNames("/A folder with . and spaces");
            Assert.AreEqual(2, resources.Length);
            Assert.Contains(resources, "404.txt");
            Assert.Contains(resources, "A file with . and spaces.txt");

            resources = _serfs.ResourceNames("\\A folder with . and spaces");
            Assert.AreEqual(2, resources.Length);
            Assert.Contains(resources, "404.txt");
            Assert.Contains(resources, "A file with . and spaces.txt");

        }
    }
}

