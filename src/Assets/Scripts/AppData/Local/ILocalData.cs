namespace PatchKit.Unity.Patcher.AppData.Local
{
    internal interface ILocalData
    {
        ILocalMetaData MetaData { get; }
        ITemporaryData TemporaryData { get; }
        IDownloadData DownloadData { get; }

        void CreateDirectory(string dirName);

        void DeleteDirectory(string dirName);

        bool DirectoryExists(string dirName);

        bool IsDirectoryEmpty(string dirName);

        void CreateOrUpdateFile(string fileName, string sourceFilePath);

        void DeleteFile(string fileName);

        bool FileExists(string fileName);

        string GetFilePath(string fileName);

        bool IsInstalled();

        int GetInstalledVersion();
    }
}