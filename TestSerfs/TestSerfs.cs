using System.IO;

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
            _serfs.AddAssembly("ResourcesForSerfsTest", "Files");
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
            Assert.IsNull(_serfs.AddAssembly("AnAssemblyThatDoesNotExist"));
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

    }
}
