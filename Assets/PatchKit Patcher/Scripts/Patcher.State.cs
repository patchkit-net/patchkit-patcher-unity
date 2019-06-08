using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using System.Threading;

public partial class Patcher
{
    private bool _hasInitializeTask = false;
    private bool _isInitialized = false;

    private bool _hasApp = false;
    private string _appPath = null;
    private string _appSecret = null;
    private string _appName = null;
    private PatchKit.Api.Models.App? _appInfo = null;
    private PatchKit.Api.Models.AppVersion[] _appVersions = null;
    private string _appLicenseKey = null;
    private int? _appInstalledVersionId = null;
    private int? _appLatestVersionId = null;
    private int? _appOverrideLatestVersionId = null;

    private bool _hasAppUpdateTask = false;
    private long _appUpdateTaskInstalledBytes = 0;
    private long _appUpdateTaskTotalBytes = 0;
    private double _appUpdateTaskBytesPerSecond = 0.0;
    private CancellationTokenSource _appUpdateTaskCts = null;

    private bool _hasAppStartTask = false;

    private bool _hasAppFetchInfoTask = false;

    private bool _hasAppFetchVersionsTask = false;

    private bool _hasAppFetchInstalledVersionIdTask = false;

    private bool _hasAppFetchLatestVersionIdTask = false;

    private bool _isOnline = true;
    
    private bool _hasQuitTask = false;
    private bool _hasQuit = false;

    private bool _hasRestartWithHigherPermissionsTask = false;

    private bool _hasRestartWithLauncherTask = false;

    private string _lockFilePath = null;
    private LibPatchKitAppsFileLock _fileLock = null;
}