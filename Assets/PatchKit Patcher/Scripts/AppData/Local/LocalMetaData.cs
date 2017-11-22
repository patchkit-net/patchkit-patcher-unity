using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
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
            [DefaultValue("patcher_data")]
            [JsonProperty("file_id", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string fileId;

            [DefaultValue("1.0")]
            [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string version;

            [DefaultValue("")]
            [JsonProperty("product_key", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string productKey;

            [DefaultValue("none")]
            [JsonProperty("product_key_encryption", DefaultValueHandling = DefaultValueHandling.Populate)]
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

            LoadData();
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

        private void LoadData()
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
        }

        private void LoadEmptyData()
        {
            _data = JsonConvert.DeserializeObject<Data>("{}"); // Json Deserializer will fill default property values defined in struct
            _data.FileVersionIds = new Dictionary<string, int>();

#if UNITY_5_3_OR_NEWER // LEGACY: fill productKey from unity prefs
            _data.productKey = UnityEngine.PlayerPrefs.GetString(ValidateLicenseCommand.CachePatchkitKey, string.Empty);
#endif
        }

        private bool TryLoadDataFromFile()
        {
            DebugLogger.Log("Trying to load from file.");

            if (!File.Exists(_filePath)) return false;

            _data = new Data();

            try
            {
                _data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(_filePath));

#if UNITY_5_3_OR_NEWER // LEGACY: fill productKey from unity prefs
                _data.productKey = string.IsNullOrEmpty(_data.productKey)
                    ? UnityEngine.PlayerPrefs.GetString(ValidateLicenseCommand.CachePatchkitKey, string.Empty)
                    : string.Empty;
#endif
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