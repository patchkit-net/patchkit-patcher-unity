using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="ILocalMetaData"/>.
    /// </summary>
    /// <seealso cref="ILocalMetaData" />
    public class LocalMetaData : ILocalMetaData
    {
        private readonly ILogger _logger;
        private const string DeprecatedCachePatchKitKey = "patchkit-key";

        /// <summary>
        /// Data structure stored in file.
        /// </summary>
        private struct Data
        {
            [DefaultValue("patcher_data")]
            [JsonProperty("file_id", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string FileId;

            [DefaultValue("1.0")] [JsonProperty("version", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string Version;

            [DefaultValue("")] [JsonProperty("product_key", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string ProductKey;

            [DefaultValue("none")]
            [JsonProperty("product_key_encryption", DefaultValueHandling = DefaultValueHandling.Populate)]
            public string ProductKeyEncryption;

            [JsonProperty("_fileVersions")] public Dictionary<string, int> FileVersionIds;
        }

        private readonly string _filePath;
        private readonly string _deprecatedFilePath;

        private Data _data;

        public LocalMetaData([NotNull] string filePath, [NotNull] string deprecatedFilePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            if (deprecatedFilePath == null)
            {
                throw new ArgumentNullException("deprecatedFilePath");
            }

            _filePath = filePath;
            _deprecatedFilePath = deprecatedFilePath;
            _logger = PatcherLogManager.DefaultLogger;

            LoadData();
        }

        public string[] GetRegisteredEntries()
        {
            return _data.FileVersionIds.Select(pair => pair.Key).ToArray();
        }

        public void RegisterEntry([NotNull] string entryName, int versionId)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }

            if (versionId <= 0)
            {
                throw new ArgumentOutOfRangeException("versionId");
            }

            if (entryName.EndsWith("/"))
            {
                throw new InvalidOperationException(
                    "Cannot register directory as entry due to problem with content installation command. See code to learn more.");
            }

            try
            {
                _logger.LogDebug(string.Format("Registering entry {0} as version {1}.", entryName, versionId));

                _data.FileVersionIds[entryName] = versionId;

                SaveData();

                _logger.LogDebug("Entry registered.");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to register entry.", e);
                throw;
            }
        }

        public void UnregisterEntry([NotNull] string entryName)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }

            try
            {
                _logger.LogDebug(string.Format("Unregistering entry {0}", entryName));

                _data.FileVersionIds.Remove(entryName);

                SaveData();

                _logger.LogDebug("Entry unregistered.");
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to unregister entry.", e);
                throw;
            }
        }

        public bool IsEntryRegistered([NotNull] string entryName)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }

            return _data.FileVersionIds.ContainsKey(entryName);
        }

        public int GetEntryVersionId([NotNull] string entryName)
        {
            if (entryName == null)
            {
                throw new ArgumentNullException("entryName");
            }

            if (!IsEntryRegistered(entryName))
            {
                throw new LocalMetaDataEntryNotFoundException(
                    string.Format("Entry {0} doesn't exists in local meta data.", entryName));
            }

            return _data.FileVersionIds[entryName];
        }

        public string GetFilePath()
        {
            return _filePath;
        }

        public void SetProductKey(string productKey)
        {
            _data.ProductKey = productKey;

            SaveData();
        }

        public string GetProductKey()
        {
            return _data.ProductKey;
        }

        private void CreateDataDir()
        {
            string dirPath = Path.GetDirectoryName(_filePath);
            if (dirPath != null)
            {
                DirectoryOperations.CreateDirectory(dirPath, CancellationToken.Empty);
            }
        }

        private void SaveData()
        {
            _logger.LogDebug(string.Format("Saving data to {0}", _filePath));

            CreateDataDir();
            Files.WriteAllText(_filePath, JsonConvert.SerializeObject(_data, Formatting.None));

            _logger.LogDebug("Data saved.");
        }

        private void LoadData()
        {
            _logger.LogDebug("Loading data...");

            _logger.LogDebug("Checking whether data file exists...");
            _logger.LogTrace("filePath = " + _filePath);
            if (!File.Exists(_filePath))
            {
                _logger.LogDebug("Data file doesn't exist. Chechking whether deprecated data file exists...");
                _logger.LogTrace("deprecatedFilePath = " + _deprecatedFilePath);
                if (File.Exists(_deprecatedFilePath))
                {
                    _logger.LogDebug("Deprecated data file exists. Moving it to a new location...");
                    CreateDataDir();
                    FileOperations.Move(_deprecatedFilePath, _filePath, CancellationToken.Empty);
                    _logger.LogDebug("Deprecated data file moved.");

                    if (TryLoadDataFromFile())
                    {
                        return;
                    }
                }
                else
                {
                    _logger.LogDebug("Deprecated data file doesn't exist.");
                }
            }
            else
            {
                _logger.LogDebug("Data file exists.");

                if (TryLoadDataFromFile())
                {
                    return;
                }
            }

            LoadEmptyData();
        }

        private void LoadEmptyData()
        {
            _logger.LogDebug("Loading empty data...");

            _data = JsonConvert
                .DeserializeObject<Data>("{}"); // Json Deserializer will fill default property values defined in struct
            _data.FileVersionIds = new Dictionary<string, int>();

            _logger.LogDebug("Empty data loaded.");
        }

        //TODO: Change from "TryXXXXX" method to method that throws exceptions.
        private bool TryLoadDataFromFile()
        {
            try
            {
                _logger.LogDebug("Trying to load data from file...");

                //TODO: Assert that file exists.

                _data = new Data();

                _logger.LogDebug("Loading content from file...");
                var fileContent = File.ReadAllText(_filePath);
                _logger.LogDebug("File content loaded.");
                _logger.LogTrace("fileContent = " + fileContent);

                _logger.LogDebug("Deserializing data...");
                _data = JsonConvert.DeserializeObject<Data>(fileContent);
                _logger.LogDebug("Data deserialized.");

                if (string.IsNullOrEmpty(_data.ProductKey)
                    && UnityEngine.PlayerPrefs.HasKey(DeprecatedCachePatchKitKey))
                {
                    _logger.LogDebug("Retrieving deprecated product key from player prefs...");
                    _logger.LogTrace("deprecatedCachePatchKitKey = " + DeprecatedCachePatchKitKey);
                    _data.ProductKey = UnityEngine.PlayerPrefs.GetString(DeprecatedCachePatchKitKey);
                    UnityEngine.PlayerPrefs.DeleteKey(DeprecatedCachePatchKitKey);
                    _logger.LogDebug("Product key retrieved and deleted from player prefs.");
                    _logger.LogDebug(_data.ProductKey);
                }

                _logger.LogDebug("Data loaded from file.");

                return true;
            }
            catch (Exception e)
            {
                //TODO: Check what exceptions are reasonable to be caught here.
                _logger.LogWarning("Failed to load data from file.", e);

                return false;
            }
        }
    }
}