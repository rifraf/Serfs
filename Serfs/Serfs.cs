using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace SERFS {

    // ---------------------------------------------------------------------------
    public class AssemblyInfo {
        private readonly Assembly _assembly;
        private readonly string[] _all_resource_names;
        private readonly string _namespace_prefix;

        public AssemblyInfo(Assembly a) {
            _assembly = a;
            _all_resource_names = _assembly.GetManifestResourceNames();  // only returns files that exist
            if (_all_resource_names.Length > 0) {
                string first_name = _all_resource_names[0];
                _namespace_prefix = first_name.Remove(first_name.IndexOf("."));
            }
        }

        public Stream OpenRead(string path) {
            foreach (string folder in _folders) {
                Stream stream = OpenRead(folder, path);
                if (stream != null) {
                    return stream;
                }
            }
            return null;
        }

        public Stream OpenRead(string folder, string path) {
            string name = PathToResourceName(folder, path);
            // Do a case insensitive compare to find the resource
            foreach (string n in _all_resource_names) {
                if (String.Compare(name, n, true, CultureInfo.InvariantCulture) == 0) {
                    return _assembly.GetManifestResourceStream(n);
                }
            }
            return null;
        }

        private string PathToResourceName(string folder, string path) {
            if (path.StartsWith("./") || path.StartsWith(".\\")) {
                path = path.Substring(2);
            }
            string requested_path = String.Format("{0}{1}", folder, path);
            // Normalize folder separator
            requested_path = requested_path.Replace(@"\", "/");
            int split_point = requested_path.LastIndexOf("/");
            if (split_point < 0) {
                return String.Format("{0}.{1}", _namespace_prefix, requested_path);
            }
            // Folders are dot separated and can't have spaces
            string basic_path = requested_path.Remove(split_point);
            string basic_filename = requested_path.Substring(split_point + 1);
            basic_path = basic_path.Replace('/', '.').Replace(' ', '_');
            return String.Format("{0}.{1}.{2}", _namespace_prefix, basic_path, basic_filename);
        }

        private readonly List<string> _folders = new List<string>();
        public AssemblyInfo Mount(string topFolder) {
            _folders.Add(topFolder.Length == 0 ? "" : topFolder + '.');
            return this;
        }

    }

    // ---------------------------------------------------------------------------
    public class Serfs {
        private readonly List<AssemblyInfo> _assembly_infos = new List<AssemblyInfo>();

        public Serfs(string folder) {
            _assembly_infos.Add(new AssemblyInfo(Assembly.GetCallingAssembly()));
            _assembly_infos[0].Mount(folder);
        }

        public AssemblyInfo Mount(string topFolder) {
            return _assembly_infos[0].Mount(topFolder);
        }

        public AssemblyInfo AddAssembly(string name) {
            return AddAssembly(name, String.Empty);
        }

        public AssemblyInfo AddAssembly(string name, string folder) {
            Assembly assembly;
            try {
                assembly = AppDomain.CurrentDomain.Load(name);
            } catch (FileNotFoundException) {
                return null;
            }
            AssemblyInfo info = new AssemblyInfo(assembly);
            info.Mount(folder);
            _assembly_infos.Add(info);
            return info;
        }

        public Stream OpenRead(string path) {
            foreach (AssemblyInfo assembly_info in _assembly_infos) {
                Stream stream = assembly_info.OpenRead(path);
                if (stream != null) {
                    return stream;
                }
            }
            return null;
        }

        public string Read(string path) {
            using (Stream stream = OpenRead(path)) {
                if (stream != null) {
                    StreamReader reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            return null;
        }
    }
}
