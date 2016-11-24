using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Data
{
    internal class LocalMetaData
    {
        private readonly object _lock = new object();

        [JsonProperty("_fileVersions")] private Dictionary<string, int> _fileVersionIds = new Dictionary<string, int>();

        public LocalMetaData()
        {
            SetDefaults();
        }

        /// <summary>
        /// Returns files saved in meta data.
        /// </summary>
        public string[] GetFileNames()
        {
            lock (_lock)
            {
                return _fileVersionIds.Select(pair => pair.Key).ToArray();
            }
        }

        /// <summary>
        /// Sets file version id.
        /// </summary>
        public void SetFileVersionId(string fileName, int versionId)
        {
            lock (_lock)
            {
                _fileVersionIds[fileName] = versionId;
            }
        }

        /// <summary>
        /// Returns file versionId. If there's no version id 
        /// </summary>
        [CanBeNull]
        public int? GetFileVersionId(string fileName)
        {
            lock (_lock)
            {
                if (!_fileVersionIds.ContainsKey(fileName))
                {
                    return null;
                }

                return _fileVersionIds[fileName];
            }
        }

        /// <summary>
        /// Erases information about file version id.
        /// </summary>
        public void ClearFileVersionId(string fileName)
        {
            lock (_lock)
            {
                _fileVersionIds.Remove(fileName);
            }
        }

        /// <summary>
        /// Serializes the data to specfied file path.
        /// </summary>
        /// <param name="filePath">The path.</param>
        public void Serialize(string filePath)
        {
            lock (_lock)
            {
                File.WriteAllText(filePath, JsonConvert.SerializeObject(this, Formatting.None));
            }
        }

        /// <summary>
        /// Deserializes the data from specified path. If files doesn't exists or is corrupted then default data is set.
        /// </summary>
        /// <param name="filePath">The path.</param>
        public void Deserialize(string filePath)
        {
            bool setDefaults = true;

            try
            {
                lock (_lock)
                {
                    JsonConvert.PopulateObject(File.ReadAllText(filePath), this);
                }

                setDefaults = false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                Debug.LogWarning("Failed to deserialize app meta data.");
            }

            if (setDefaults)
            {
                Debug.Log("Setting default app meta data.");

                SetDefaults();
            }
        }

        private void SetDefaults()
        {
            lock (_lock)
            {
                _fileVersionIds = new Dictionary<string, int>();
            }
        }
    }
}