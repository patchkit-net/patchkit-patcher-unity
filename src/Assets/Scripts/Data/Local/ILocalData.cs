namespace PatchKit.Unity.Patcher.Data.Local
{
    public interface ILocalData
    {
        ILocalMetaData MetaData { get; }

        void CreateDirectory(string dirName);

        void DeleteDirectory(string dirName);

        bool DirectoryExists(string dirName);

        bool IsDirectoryEmpty(string dirName);

        void CreateOrUpdateFile(string fileName, string sourceFilePath);

        void DeleteFile(string fileName);

        bool FileExists(string fileName);

        string GetFilePath(string fileName);
    }
}