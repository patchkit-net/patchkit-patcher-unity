using System.IO;
using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class Pack1Meta
    {
        #region Fields

        public string Version { get; set; }

        public string Encryption { get; set; }

        public string Iv { get; set; }

        public FileEntry[] Files { get; set; }

        #endregion


        #region Methods

        public static Pack1Meta ParseFromFile(string filename)
        {
            var text = File.ReadAllText(filename);
            return Parse(text);
        }

        public static Pack1Meta Parse(string content)
        {
            UnityEngine.Debug.Log(content);
            return JsonConvert.DeserializeObject<Pack1Meta>(content);
        }

        #endregion


        #region Inner Types

        public class FileEntry
        {
            public string Name { get; set; }

            public string Type { get; set; }

            public string Target { get; set; }

            public string Mode { get; set; }

            public long? Offset { get; set; }

            public long? Size { get; set; }

            public override string ToString()
            {
                return string.Format("Name: {0}, Type: {1}, Target: {2}, Mode: {3}, Offset: {4}, Size: {5}",
                    Name, Type, Target, Mode, Offset, Size);
            }
        }

        #endregion
    }
}