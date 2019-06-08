using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public partial class Patcher
{
    private async Task<bool> QuitAsync()
    {
        if (!CanPerformNewForegroundTask())
        {
            return false;
        }

        Debug.Log(message: "Quitting...");

        _hasQuitTask = true;
        SendStateChanged();

        try
        {
            _fileLock?.Dispose();
            _fileLock = null;
            SendStateChanged();

            DeleteLockFileIfExists();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

            Debug.Log(message: "Successfully quit.");

            _hasQuit = true;
            SendStateChanged();
        }
        catch (System.Exception e)
        {
            Debug.LogError(message: "Failed to quit: unknown error.");
            Debug.LogException(exception: e);
        }
        finally
        {
            _hasQuitTask = false;
            SendStateChanged();
        }

        return true;
    }

    private void DeleteLockFileIfExists()
    {
        if (_lockFilePath == null)
        {
            return;
        }

        Debug.Log(message: $"Deleting lock file at {_lockFilePath}...");

        try
        {
            if (File.Exists(_lockFilePath))
            {
                File.Delete(_lockFilePath);

                Debug.Log(message: "Successfully deleted lock file.");
            }
            else
            {
                Debug.Log(
                    message:
                    "Failed to delete lock file: it already doesn't exist.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                message: "Failed to delete lock file: unknown error.");
            Debug.LogException(exception: e);
        }
    }
}