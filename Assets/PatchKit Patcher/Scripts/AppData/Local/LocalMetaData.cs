using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="ILocalMetaData"/>.
    /// </summary>
    /// <seealso cref="ILocalMetaData" />
    public class LocalMetaData : ILocalMetaData
    {
        /// <summary>
        /// Data structure stored in file.
        /// </summary>
        private struct Data
        {
            [JsonProperty("file_id")]
            public string fileId;

            [JsonProperty("version")]
            public string version;

            [JsonProperty("product_key")]
            public string productKey;

            [JsonProperty("product_key_encryption")]
            public string productKeyEncryption;

            [JsonProperty("_fileVersions")]
            public Dictionary<string, int> FileVersionIds;
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(LocalMetaData));

        private readonly string _filePath;

        private Data _data;
        private string _deprecatedFilePath;

        public LocalMetaData(string filePath, string deprecatedFilePath)
        {
            Checks.ArgumentNotNullOrEmpty(filePath, "filePath");
            Checks.ArgumentNotNullOrEmpty(deprecatedFilePath, "deprecatedFilePath");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(filePath, "filePath");
            DebugLogger.LogVariable(deprecatedFilePath, "deprecatedFilePath");

            _filePath = filePath;
            _deprecatedFilePath = deprecatedFilePath;

            UpdateData();
        }

        public string[] GetRegisteredEntries()
        {
            return _data.FileVersionIds.Select(pair => pair.Key).ToArray();
        }

        public void RegisterEntry(string entryName, int versionId)
        {
            Checks.ArgumentNotNullOrEmpty(entryName, "fileName");
            Checks.ArgumentValidVersionId(versionId, "versionId");

            // TODO: Uncomment this after fixing directory registration in install content command
            Assert.IsFalse(entryName.EndsWith("/"),
                "Cannot register directory as entry due to problem with content installation command. See code to learn more.");

            DebugLogger.Log(string.Format("Adding or updating file {0} to version {1}.", entryName, versionId));

            _data.FileVersionIds[entryName] = versionId;

            SaveData();
        }

        public void UnregisterEntry(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

            DebugLogger.Log(string.Format("Removing file {0}", fileName));

            _data.FileVersionIds.Remove(fileName);

            SaveData();
        }

        public bool IsEntryRegistered(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

            return _data.FileVersionIds.ContainsKey(fileName);
        }

        public int GetEntryVersionId(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");
            Assert.IsTrue(IsEntryRegistered(fileName), string.Format("File doesn't exist in meta data - {0}", fileName));

            return _data.FileVersionIds[fileName];
        }

        public string GetFilePath()
        {
            return _filePath;
        }

        private void SaveData()
        {
            DebugLogger.Log("Saving.");

            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_data, Formatting.None));
        }

        private void UpdateData()
        {
            DebugLogger.Log("Loading.");

            if (!File.Exists(_filePath))
            {
                if (File.Exists(_deprecatedFilePath))
                {
                    File.Move(_deprecatedFilePath, _filePath);
                }
            }

            if (TryLoadDataFromFile())
            {
                DebugLogger.Log("Loaded from file.");
            }
            else
            {
                DebugLogger.Log("Cannot load from file.");

                LoadEmptyData();
            }

            // if app uses product key
            _data.productKey = "ABCD-EFGH-IJKL";
            _data.productKeyEncryption = "none";
        }

        private void LoadEmptyData()
        {
            _data = new Data
            {
                FileVersionIds = new Dictionary<string, int>()
            };
        }

        private bool TryLoadDataFromFile()
        {
            DebugLogger.Log("Trying to load from file.");

            if (!File.Exists(_filePath)) return false;

            _data = new Data();

            try
            {
                _data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(_filePath));
                _data.fileId = _data.fileId.Equals(JsonConvert.Null) ? "patcher_data" : _data.fileId;
                _data.version = _data.fileId.Equals(JsonConvert.Null) ? "1.0" : _data.fileId;

                return true;
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);

                return false;
            }
        }
    }
}