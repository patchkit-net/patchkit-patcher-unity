using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Debugging;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private struct InitializationData
    {
        public InitializationData(
            [NotNull] string appSecret,
            [NotNull] string appPath,
            string lockFilePath,
            int? appOverrideLatestVersionId,
            bool? isOnline,
            bool automaticallyUpdateApp,
            bool automaticallyStartApp)
        {
            AppSecret = appSecret;
            AppPath = appPath;
            LockFilePath = lockFilePath;
            AppOverrideLatestVersionId = appOverrideLatestVersionId;
            IsOnline = isOnline;
            AutomaticallyUpdateApp = automaticallyUpdateApp;
            AutomaticallyStartApp = automaticallyStartApp;
        }

        [NotNull]
        public string AppSecret { get; }

        [NotNull]
        public string AppPath { get; }

        public string LockFilePath { get; }

        public int? AppOverrideLatestVersionId { get; }

        public bool? IsOnline { get; }

        public bool AutomaticallyUpdateApp { get; }

        public bool AutomaticallyStartApp { get; }

        public override string ToString()
        {
            return $"{{ \"AppSecret\": {AppSecret}, " +
                $"\"AppPath\": {AppPath}, " +
                $"\"LockFilePath\": {LockFilePath?.SurroundWithQuotes() ?? "null"}, " +
                $"\"AppOverrideLatestVersionId\": {AppOverrideLatestVersionId?.ToString() ?? "null"}, " +
                $"\"IsOnline\": {IsOnline?.ToString() ?? "null"}, " +
                $"\"AutomaticallyUpdateApp\": {AutomaticallyUpdateApp}, " +
                $"\"AutomaticallyStartApp\": {AutomaticallyStartApp} }}";
        }
    }

    private async void Initialize()
    {
        if (_hasInitializeTask || _isInitialized)
        {
            return;
        }

        Debug.Log(message: "Initializing...");

        _hasInitializeTask = true;
        SendStateChanged();

        InitializationData? data;

        try
        {
            Assert.raiseExceptions = true;
            Application.runInBackground = true;
            UnitySystemConsoleRedirector.Redirect();

            Debug.Log(message: $"Version: {Version.Text}");
            Debug.Log(
                message:
                $"Runtime version: {EnvironmentInfo.GetRuntimeVersion()}");
            Debug.Log(
                message:
                $"System version: {EnvironmentInfo.GetSystemVersion()}");
            Debug.Log(
                message:
                $"System information: {EnvironmentInfo.GetSystemInformation()}");

            InitializeLibPatchKitApps();

#if UNITY_EDITOR
            data = LoadEditorInitializationData();
#else
            data = LoadCommandLineInitializationData();
#endif

            if (!data.HasValue)
            {
                Debug.LogWarning(
                    message:
                    "Initialization failed: data wasn't loaded. Most probably it means that patcher has been started without launcher.");

                SendError(Error.StartedWithoutLauncher);

                return;
            }

            Debug.Log(message: $"Initialization data = {data}");

            _hasApp = true;
            _appSecret = data.Value.AppSecret;
            _appPath = data.Value.AppPath;
            _lockFilePath = data.Value.LockFilePath;
            _appOverrideLatestVersionId = data.Value.AppOverrideLatestVersionId;
            _isOnline = data.Value.IsOnline ?? true;
            SendStateChanged();

            if (_lockFilePath != null)
            {
                Debug.Log(
                    message: $"Getting file lock at '{_lockFilePath}'...");

                Assert.IsNull(_fileLock);

                try
                {
                    _fileLock = await LibPatchKitApps.GetFileLockAsync(
                        path: _lockFilePath,
                        cancellationToken: CancellationToken.None);
                    SendStateChanged();

                    Debug.Log(message: "Successfully got file lock.");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log(
                        message:
                        "Failed to get file lock: operation cancelled.");
                }
                catch (LibPatchKitAppsInternalErrorException)
                {
                    Debug.LogWarning(
                        message: "Failed to get file lock: internal error.");
                }
                catch (LibPatchKitAppsFileAlreadyInUseException)
                {
                    Debug.LogError(
                        message: "Failed to get file lock: already in use.");

                    SendError(Error.MultipleInstances);

                    return;
                }
                catch (LibPatchKitAppsNotExistingFileException)
                {
                    Debug.Log(
                        message:
                        "Failed to get file lock: file doesn't exist.");
                }
            }

            Debug.Log(message: "Successfully initialized.");

            _isInitialized = true;
            SendStateChanged();
        }
        catch (Exception e)
        {
            Debug.LogError(message: "Failed to initialize: unknown error.");
            Debug.LogException(exception: e);

            SendError(Error.CriticalError);

            return;
        }
        finally
        {
            _hasInitializeTask = false;
            SendStateChanged();
        }

        var fetchTasks = Task.WhenAll(
            FetchAppInfoAsync(),
            FetchAppVersionsAsync(),
            FetchAppLatestVersionIdAsync(),
            FetchAppInstalledVersionIdAsync());

        if (data.Value.AutomaticallyUpdateApp)
        {
            Debug.Log(message: "Automatically updating app...");

            await UpdateAppAsync();
        }

        if (data.Value.AutomaticallyStartApp)
        {
            Debug.Log(message: "Automatically starting app...");

            await StartAppAsync();
        }

        await fetchTasks;
    }

    private void InitializeLibPatchKitApps()
    {
        Debug.Log(message: "Initializing libpkapps...");

        bool is64Bit = IntPtr.Size == 8;

        if (Application.platform == RuntimePlatform.LinuxEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: is64Bit
                    ? LibPatchKitAppsPlatformType.Linux64
                    : LibPatchKitAppsPlatformType.Linux32);
        }

        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: is64Bit
                    ? LibPatchKitAppsPlatformType.Win32
                    : LibPatchKitAppsPlatformType.Win64);
        }

        if (Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: LibPatchKitAppsPlatformType.Osx64);
        }

        Debug.Log(message: "libpkapps initialized.");
    }

    public bool AutomaticallyStartApp;
    public bool AutomaticallyUpdateApp = true;

#if UNITY_EDITOR
    public string EditorAppSecret;
    public int EditorOverrideAppLatestVersionId;

    private InitializationData? LoadEditorInitializationData()
    {
        Debug.Log(message: "Loading initialization data from editor...");

        Assert.IsNotNull(value: Application.dataPath);
        Assert.IsNotNull(value: EditorAppSecret);

        var result = new InitializationData(
            appPath: Application.dataPath.Replace(
                oldValue: "/Assets",
                newValue: $"/Temp/PatcherApp{EditorAppSecret}"),
            appSecret: EditorAppSecret,
            lockFilePath: null,
            appOverrideLatestVersionId: EditorOverrideAppLatestVersionId > 0
                ? (int?) EditorOverrideAppLatestVersionId
                : null,
            isOnline: null,
            automaticallyStartApp: AutomaticallyStartApp,
            automaticallyUpdateApp: AutomaticallyUpdateApp);

        Debug.Log(message: "Initialization data loaded.");

        return result;
    }
#endif

    private InitializationData? LoadCommandLineInitializationData()
    {
        Debug.Log(message: "Loading initialization data from command line...");

        var args = Environment.GetCommandLineArgs().ToList();

        Debug.Log(
            message:
            $"Command line args: {string.Join(separator: " ", values: args)}");

        bool areReadable = args.Contains(item: "--readable");

        string appSecret = null;
        string appPath = null;
        bool? isOnline = null;
        string lockFilePath = null;
        int? overrideAppLatestVersionId = null;

        for (int i = 0; i < args.Count; i++)
        {
            if (i + 1 < args.Count)
            {
                if (args[index: i] == "--lockfile")
                {
                    lockFilePath = args[index: i + 1];

                    i++;
                    continue;
                }

                if (args[index: i] == "--installdir")
                {
                    appPath = MakeAppPathAbsolute(
                        relativeAppDataPath: args[index: i + 1]);

                    i++;
                    continue;
                }

                if (args[index: i] == "--secret")
                {
                    appSecret = areReadable
                        ? args[index: i + 1]
                        : DecodeSecret(encodedSecret: args[index: i + 1]);

                    i++;
                    continue;
                }
            }

            if (args[index: i] == "--online")
            {
                isOnline = true;
            }
            else if (args[index: i] == "--offline")
            {
                isOnline = false;
            }
        }

        string forceAppSecret;
        if (EnvironmentInfo.TryReadEnvironmentVariable(
            argumentName: EnvironmentVariables.ForceSecretEnvironmentVariable,
            value: out forceAppSecret))
        {
            Debug.Log(
                message:
                $"Using app secret from environment variable: {EnvironmentVariables.ForceSecretEnvironmentVariable}={forceAppSecret}");

            appSecret = forceAppSecret;
        }

        string forceOverrideLatestVersionIdString;
        if (EnvironmentInfo.TryReadEnvironmentVariable(
            argumentName: EnvironmentVariables.ForceVersionEnvironmentVariable,
            value: out forceOverrideLatestVersionIdString))
        {
            int forceOverrideLatestVersionId;

            if (int.TryParse(
                s: forceOverrideLatestVersionIdString,
                result: out forceOverrideLatestVersionId))
            {
                Debug.Log(
                    message:
                    $"Using override latest version id from environment variable: {EnvironmentVariables.ForceVersionEnvironmentVariable}={forceOverrideLatestVersionId}");


                overrideAppLatestVersionId = forceOverrideLatestVersionId;
            }
        }

        if (appSecret == null)
        {
            Debug.LogWarning(
                message:
                "Failed to load command line initialization data: app secret is null.");

            return null;
        }

        if (appPath == null)
        {
            Debug.LogWarning(
                message:
                "Failed to load command line initialization data: app path is null.");

            return null;
        }

        var result = new InitializationData(
            appPath: appPath,
            appSecret: appSecret,
            lockFilePath: lockFilePath,
            appOverrideLatestVersionId: overrideAppLatestVersionId,
            isOnline: isOnline,
            automaticallyStartApp: AutomaticallyStartApp,
            automaticallyUpdateApp: AutomaticallyUpdateApp);

        Debug.Log(
            message: "Successfully loaded command line initialization data.");

        return result;
    }

    private static string MakeAppPathAbsolute(string relativeAppDataPath)
    {
        if (relativeAppDataPath == null)
        {
            return null;
        }

        string path = Path.GetDirectoryName(path: Application.dataPath);

        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            path = Path.GetDirectoryName(path: path);
        }

        Assert.IsNotNull(path);

        return Path.Combine(
            path1: path,
            path2: relativeAppDataPath);
    }

    private static string DecodeSecret(string encodedSecret)
    {
        if (encodedSecret == null)
        {
            return null;
        }

        var bytes = Convert.FromBase64String(s: encodedSecret);

        for (int i = 0; i < bytes.Length; ++i)
        {
            byte b = bytes[i];
            bool lsb = (b & 1) > 0;
            b >>= 1;
            b |= (byte) (lsb ? 128 : 0);
            b = (byte) ~b;
            bytes[i] = b;
        }

        var chars = new char[bytes.Length / sizeof(char)];
        Buffer.BlockCopy(
            src: bytes,
            srcOffset: 0,
            dst: chars,
            dstOffset: 0,
            count: bytes.Length);

        return new string(value: chars);
    }
}