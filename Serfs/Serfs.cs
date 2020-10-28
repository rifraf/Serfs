using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SERFS {

    // ---------------------------------------------------------------------------
    public interface IStreamDecoder {
        Stream Decode(Stream stream);
    }

    // ---------------------------------------------------------------------------
    internal class NullDecoder : IStreamDecoder {
        public Stream Decode(Stream stream) {return stream;}
    }

    // ---------------------------------------------------------------------------
    /// <summary>
    /// Handles the resource information for a given assembly/resource prefix set
    /// </summary>
    public class AssemblyInfo {
        private readonly Assembly _assembly;
        private readonly string[] _all_resource_names;
        private readonly string _resource_prefix;
        private IStreamDecoder _decoder;

        /// <summary>
        /// Create an AssemblyInfo
        /// </summary>
        /// <param name="sourceAssembly">The assembly whose embedded resources we want</param>
        /// <param name="resourcePrefix">The name that prefixes the files. Normally the default namespace</param>
        /// <param name="decoder">Instance of IStreamDecoder</param>
        public AssemblyInfo(Assembly sourceAssembly, string resourcePrefix, IStreamDecoder decoder) {
            _assembly = sourceAssembly;
            _resource_prefix = resourcePrefix;
            _all_resource_names = _assembly.GetManifestResourceNames();
            _decoder = decoder;
            Array.Sort(_all_resource_names);    // Just to help during development. It's easier to find files in order...
        }

        /// <summary>
        /// Is this instance handling the given assembly/resource prefix
        /// </summary>
        /// <param name="sourceAssembly">The assembly whose embedded resources we are handling</param>
        /// <param name="resourcePrefix">The resource prefix</param>
        /// <returns>True iff both params match</returns>
        public bool IsHandling(Assembly sourceAssembly, string resourcePrefix) {
            return (_assembly == sourceAssembly) && (_resource_prefix == resourcePrefix);
        }

        private readonly List<string> _folders = new List<string>();
        /// <summary>
        /// Adds the name of a resource folder to act as the 'root' of the file heirarchy.
        /// </summary>
        /// <param name="topFolder">Name of folder. e.g. Files/Apps</param>
        /// <returns>Returns itself so you can chain mounts.</returns>
        public AssemblyInfo Mount(string topFolder) {
            // Strip any leading \ or /
            if ((topFolder[0] == '/') || (topFolder[0] == '\\')) {
                topFolder = topFolder.Substring(1);
            }
            if (!_folders.Contains(topFolder)) {
                _folders.Add(topFolder.Length == 0 ? "" : topFolder + '/');
            }
            return this;
        }

        /// <summary>
        /// Scans each mounted folder to see if it contains the file described by 'path'. Opens a stream if found.
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <returns>An open stream or null</returns>
        public Stream OpenRead(string path) {
            if (_folders.Count == 0) {
                return OpenRead("", path);  // No specific mount
            }
            foreach (string folder in _folders) {
                Stream stream = OpenRead(folder, path);
                if (stream != null) {
                    return stream;
                }
            }
            return null;
        }

        /// <summary>
        /// Decoder to user when reading files
        /// </summary>
        public IStreamDecoder Decoder {
            set { _decoder = value; }
        }

        /// <summary>
        /// Scans a specific folder to see if it contains the file described by 'path'. Opens a stream if found.
        /// </summary>
        /// <param name="folder">The folder to check</param>
        /// <param name="path">The path to the file</param>
        /// <returns>An open stream or null</returns>
        public Stream OpenRead(string folder, string path) {
            string name = FindResourceName(folder, path);
            return (name == null) ? null : _decoder.Decode(_assembly.GetManifestResourceStream(name));
        }

        /// <summary>
        /// Find the name of the embedded resource that contains the file requested
        /// </summary>
        /// <param name="folder">The folder to check</param>
        /// <param name="path">The path to the file</param>
        /// <returns>A name or null</returns>
        public string FindResourceName(string folder, string path) {
            string name = PathToResourceName(folder, path);
            // Do a case insensitive compare to find the resource
            foreach (string n in _all_resource_names) {
                if (String.Compare(name, n, true, CultureInfo.InvariantCulture) == 0) {
                    return n;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the name of the embedded resource that contains the file requested
        /// </summary>
        /// <param name="path">The full path to the file</param>
        /// <returns>A name or null</returns>
        public string FindResourceName(string path) {
            if (_folders.Count == 0) {
                return FindResourceName("", path);  // No specific mount
            }
            foreach (string folder in _folders) {
                string name = FindResourceName(folder, path);
                if (name != null) {
                    return name;
                }
            }
            return null;
        }

        /// <summary>
        /// See if the folder exists
        /// </summary>
        /// <param name="folder">The folder to check</param>
        /// <param name="path">The path</param>
        public bool FolderExists(string folder, string path) {
            if (!path.EndsWith("/") && !path.EndsWith("\\")) {
                path += '/';
            }
            string name = PathToResourceName(folder, path);
            // Do a case insensitive compare to find the resource
            foreach (string n in _all_resource_names) {
                if (n.StartsWith(name, true, CultureInfo.InvariantCulture)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// See if the folder exists
        /// </summary>
        /// <param name="path">The path</param>
        public bool FolderExists(string path) {
            if (_folders.Count == 0) {
                return FolderExists("", path);
            }
            foreach (string folder in _folders) {
                if (FolderExists(folder, path)) {
                    return true;
                }
            }
            return false;
        }

        internal void AddResourceNames(string folder, string baseName, ref List<string> names) {
            // We need to form a prefix that represents the root of the folder/baseName
            // but in the correct embedded resource form
            if (!baseName.EndsWith("/") && !baseName.EndsWith("\\")) {
                baseName += '/';
            }
            string pattern = PathToResourceName(folder, baseName + "*");
            string prefix = pattern.Remove(pattern.Length - 1);
            int prefix_length = prefix.Length;
            foreach (string n in _all_resource_names) {
                if (n.StartsWith(prefix, true, CultureInfo.InvariantCulture)) {
                    names.Add(n.Substring(prefix_length));
                }
            }            
        }

        internal void AddResourceNames(string baseName, ref List<string> names) {
            if (_folders.Count == 0) {
                AddResourceNames("", baseName, ref names);  // No specific mount
            }
            foreach (string folder in _folders) {
                AddResourceNames(folder, baseName, ref names);
            }
        }

        /// <summary>
        /// Works out what the embedded file name will be for a given folder and path
        /// </summary>
        private string PathToResourceName(string folder, string requestedFilePath) {
            requestedFilePath = RegularizeRequestedPath(requestedFilePath);
            ExtractDirectoryAndFile(requestedFilePath, folder, out var directory, out var filename);

            string foldername = PathToResourceFolderName(directory);
            return String.Format("{0}.{1}{2}", _resource_prefix, foldername, filename);
        }

        // Split into directory and file part
        private static void ExtractDirectoryAndFile(string requestedFilePath, string folder, out string directory, out string filename) {
            string full_path = folder + requestedFilePath;
            
            // Normalize folder separators
            full_path = full_path.Replace(@"\", "/");

            // Split on final '/'
            int split_point = full_path.LastIndexOf("/");
            if (split_point < 0) {
                directory = "";
                filename = requestedFilePath;
            } else {
                if (split_point + 1 == full_path.Length) {
                    directory = full_path;
                    filename = "";
                } else {
                    directory = full_path.Remove(split_point + 1);
                    filename = full_path.Substring(split_point + 1);
                }
            }
        }

        private static string RegularizeRequestedPath(string requestedFilePathPath) {
            // /, ./, \ and .\ don't mean anything to Serfs because we are always at 'root'
            if (requestedFilePathPath.StartsWith("./") || requestedFilePathPath.StartsWith(".\\")) {
                requestedFilePathPath = requestedFilePathPath.Substring(2);
            }
            if (requestedFilePathPath.StartsWith("/") || requestedFilePathPath.StartsWith("\\")) {
                requestedFilePathPath = requestedFilePathPath.Substring(1);
            }
            return requestedFilePathPath;
        }

        private static string PathToResourceFolderName(string directory) {

            // Folders are dot separated and can't have spaces or '-'
            string result = directory.Replace('/', '.').Replace(' ', '_').Replace('-', '_');

            // Folders starting with numeric have _ prefixed
            result = Regex.Replace(result, @"\.(\d)", @"._$1");

            return result;
        }
    }

    // ---------------------------------------------------------------------------
    /// <summary>
    /// Simple Embedded Resource File System
    /// </summary>
    public class Serfs {
        private readonly List<AssemblyInfo> _assembly_infos = new List<AssemblyInfo>();
        private Assembly _entry_assembly;
        private bool _ignore_missing_assemblies;
        private IStreamDecoder _decoder;

        /// <summary>
        /// Create Serfs instance based on the embedded resource files in the
        /// specified assembly
        /// </summary>
        /// <param name="assembly">The specific assembly</param>
        /// <param name="resourcePrefix">The prefix for resources (normally the default namespace)</param>
        /// <param name="folder">The resource folder that is to act as the root directory</param>
        public Serfs(Assembly assembly, string resourcePrefix, string folder) {
            InitializeInstance(assembly, resourcePrefix, folder);
        }

        /// <summary>
        /// Create Serfs instance based on the entry assembly (main program) unless
        /// none is reported (typically during tests), in which case use caller
        /// </summary>
        /// <param name="folder">The resource folder that is to act as the root directory</param>
        public Serfs(string folder) {
            _entry_assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            InitializeInstance(_entry_assembly, _entry_assembly.GetName().Name, folder);
        }

        private void InitializeInstance(Assembly root, string resourcePrefix, string folder) {
            _entry_assembly = root;
            _decoder = new NullDecoder();
            AssemblyInfo info = AddAssembly(_entry_assembly, resourcePrefix);
            if (folder != null) {
                info.Mount(folder);
            }
        }

        /// <summary>
        /// Decoder to user when reading files
        /// </summary>
        public IStreamDecoder Decoder {
            set {
                _decoder = value;
                foreach (AssemblyInfo assembly_info in _assembly_infos) {
                    assembly_info.Decoder = _decoder;
                }
            }
        }

        /// <summary>
        /// Add a new assembly to the list of assemblies being tracked.
        /// </summary>
        /// <param name="assembly">The specific assembly</param>
        /// <param name="resourcePrefix">The prefix for resources (normally the default namespace)</param>
        /// <returns>AssemblyInfo describing the addition.</returns>
        public AssemblyInfo AddAssembly(Assembly assembly, string resourcePrefix) {
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                if (assembly_info.IsHandling(assembly, resourcePrefix)) {
                    return assembly_info;
                }
            }
            AssemblyInfo info = new AssemblyInfo(assembly, resourcePrefix, _decoder);
            _assembly_infos.Add(info);
            return info;
        }

        /// <summary>
        /// Add a new assembly to the list of assemblies being tracked.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly. Its resource prefix must match the default namespace</param>
        /// <returns></returns>
        public AssemblyInfo AddAssembly(string assemblyName) {
            return AddAssembly(assemblyName, null);
        }

        /// <summary>
        /// Add a new assembly to the list of assemblies being tracked.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <param name="resourcePrefix">The prefix for resources (normally the default namespace)</param>
        /// <returns></returns>
        public AssemblyInfo AddAssembly(string assemblyName, string resourcePrefix) {
            Assembly assembly;
            try {
                assembly = AppDomain.CurrentDomain.Load(assemblyName);
            } catch (FileNotFoundException) {
                if (_ignore_missing_assemblies) {
                    assembly = _entry_assembly;
                } else {
                    return null;
                }
            }
            return AddAssembly(assembly, resourcePrefix ?? assemblyName);
        }

        /// <summary>
        /// Normally adding an assembly that cannot be found causes the add to be ignored. If,
        /// however, we have used ILMerge, assemblies get merged into one, so we use the assembly
        /// selected during initialization. To support this, we must set IgnoreMissingAssemblies
        /// </summary>
        public bool IgnoreMissingAssemblies { get { return _ignore_missing_assemblies; } set { _ignore_missing_assemblies = value; } }
        /// <summary>
        /// Attach a named embedded resource folder as a 'root' folder in the Serfs directory structure
        /// More than one can be attached.
        /// </summary>
        /// <param name="topFolder">Name of resource folder. e.g. Files/Apps</param>
        /// <returns></returns>
        public AssemblyInfo Mount(string topFolder) {
            return _assembly_infos[0].Mount(topFolder);
        }

        /// <summary>
        /// Scan the attached assemblies and folders for the named file, returning
        /// an opened stream if found.
        /// </summary>
        public Stream OpenRead(string path) {
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                Stream stream = assembly_info.OpenRead(path);
                if (stream != null) {
                    return stream;
                }
            }
            return null;
        }

        /// <summary>
        /// Scan the attached assemblies and folders for the named file, returning
        /// true iff found.
        /// </summary>
        public bool Exists(string path) {
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                if (assembly_info.FindResourceName(path) != null) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Scan the attached assemblies and folders for the named folder, returning
        /// true iff found.
        /// </summary>
        public bool FolderExists(string path) {
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                if (assembly_info.FolderExists(path)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get a list of the embedded resource files that are accessible from '/'
        /// The names are *internal resource names*, so have '.' separators and
        /// may have other escape characters
        /// </summary>
        public string[] ResourceNames() {
            return ResourceNames("/");
        }

        /// <summary>
        /// Get a list of the embedded resource files that are accessible from a
        /// given root folder.
        /// The names are *internal resource names*, so have '.' separators and
        /// may have other escape characters
        /// </summary>
        public string[] ResourceNames(string baseName) {
            List<string> names = new List<string>();
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                assembly_info.AddResourceNames(baseName, ref names);
            }
            return names.ToArray();
        }

        /// <summary>
        /// Scan the attached assemblies and folders for the named file, returning
        /// its contents as a string if found.
        /// </summary>
        public string Read(string path) {
            using (Stream stream = OpenRead(path)) {
                if (stream != null) {
                    StreamReader reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            return null;
        }

        /// <summary>
        /// Scan the attached assemblies and folders for the named file, returning
        /// its contents as a string if found. \r\n is converted to \n
        /// </summary>
        public string ReadText(string path) {
            using (Stream stream = OpenRead(path)) {
                if (stream != null) {
                    StreamReader reader = new StreamReader(stream);
                    return reader.ReadToEnd().Replace("\r\n", "\n");
                }
            }
            return null;
        }

    }
}
