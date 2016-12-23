using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class LocalMetaData : ILocalMetaData
    {
        private struct Data
        {
            [JsonProperty("_fileVersions")]
            public Dictionary<string, int> FileVersionIds;
        }

        private readonly DebugLogger _debugLogger;

        private readonly string _filePath;

        private Data _data;

        public LocalMetaData(string filePath)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("filePath = " + filePath);

            _filePath = filePath;
            LoadData();
        }

        public IEnumerator<string> GetFileNames()
        {
            return _data.FileVersionIds.Select(pair => pair.Key).GetEnumerator();
        }

        public void AddOrUpdateFile(string fileName, int versionId)
        {
            _debugLogger.Log(string.Format("Adding or updating file {0} to version {1}.", fileName, versionId));

            _data.FileVersionIds[fileName] = versionId;

            SaveData();
        }

        public void RemoveFile(string fileName)
        {
            _debugLogger.Log(string.Format("Removing file {0}", fileName));

            _data.FileVersionIds.Remove(fileName);

            SaveData();
        }

        public bool FileExists(string fileName)
        {
            return _data.FileVersionIds.ContainsKey(fileName);
        }

        public int GetFileVersion(string fileName)
        {
            if (!_data.FileVersionIds.ContainsKey(fileName))
            {
                throw new InvalidOperationException(string.Format("File doesn't exist in database - {0}", fileName));
            }

            return _data.FileVersionIds[fileName];
        }

        private void SaveData()
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_data, Formatting.None));
        }

        private void LoadData()
        {
            if (File.Exists(_filePath) && LoadDataFromFile())
            {
                return;
            }

            LoadEmptyData();
        }

        private void LoadEmptyData()
        {
            _data = new Data
            {
                FileVersionIds = new Dictionary<string, int>()
            };
        }

        private bool LoadDataFromFile()
        {
            _data = new Data();

            try
            {
                _data = JsonConvert.DeserializeObject<Data>(File.ReadAllText(_filePath));
                return true;
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(this, exception);

                return false;
            }
        }
    }
}