public partial class Patcher
{
    private void Dispose()
    {
        _fileLock?.Dispose();
        _fileLock = null;
    }
}