using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.Application
{
    internal class ApplicationDataCache
    {
        [JsonProperty]
        private Dictionary<string, int> _fileVersions = new Dictionary<string, int>();

        private readonly string _path;

        internal ApplicationDataCache(string path)
        {
            _path = path;
            Deserialize();
        }

        /// <summary>
        /// Returns files saved in cache.
        /// </summary>
        public IEnumerable<string> GetFileNames()
        {
            lock (_fileVersions)
            {
                return _fileVersions.Select(pair => pair.Key);
            }
        }

        /// <summary>
        /// If all files have the same version then it is returned. Otherwise, returned value is <c>null</c>.
        /// </summary>
        public int? GetCommonVersion()
        {
            lock (_fileVersions)
            {
                int? version = null;

                foreach (var file in _fileVersions)
                {
                    if (file.Value == -1)
                    {
                        return null;
                    }
                    if (version == null)
                    {
                        version = file.Value;
                    }
                    else if (version != file.Value)
                    {
                        return null;
                    }
                }

                return version;
            }
        }

        /// <summary>
        /// Sets file version.
        /// </summary>
        public void SetFileVersion(string fileName, int version)
        {
            lock (_fileVersions)
            {
                _fileVersions[fileName] = version;
                Serialize();
            }
        }

        /// <summary>
        /// Returns file version. If there's no version 
        /// </summary>
        [CanBeNull]
        public int? GetFileVersion(string fileName)
        {
            lock (_fileVersions)
            {
                if (!_fileVersions.ContainsKey(fileName))
                    return null;
                return _fileVersions[fileName];
            }
        }

        /// <summary>
        /// Erases information about file version.
        /// </summary>
        public void ClearFileVersion(string fileName)
        {
            lock (_fileVersions)
            {
                _fileVersions.Remove(fileName);
            }
        }

        private void Serialize()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.None));
        }

        private void Deserialize()
        {
            try
            {
                if (File.Exists(_path))
                {
                    JsonConvert.PopulateObject(File.ReadAllText(_path), this);
                }
            }
            catch (Exception)
            {
                _fileVersions = new Dictionary<string, int>();
            }
        }
    }
}
