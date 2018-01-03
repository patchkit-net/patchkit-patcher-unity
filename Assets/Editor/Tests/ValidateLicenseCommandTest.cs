using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Api;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;

class ValidateLicenseCommandTest
{
    private PatchKit.Logging.ILogger _logger;
    private PatchKit.IssueReporting.IIssueReporter _issueReporter;
    private IStatusMonitor _statusMonitor;
    private MockCache _cache;
    
    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<PatchKit.Logging.ILogger>();
        _issueReporter = Substitute.For<PatchKit.IssueReporting.IIssueReporter>();
        _statusMonitor = Substitute.For<IStatusMonitor>();
        _cache = new MockCache();
    }
    
    [Test]
    public void Execute_CachesKeyAndKeySecret()
    {
        const string key = "this-key-should-be-cached";
        const string keySecret = "this-key-secret-should-be-cached";
        
        for (int i = 0; i < 2; i++)
        {
            var licenseDialog = Substitute.For<ILicenseDialog>();
            licenseDialog.Display(Arg.Any<LicenseDialogMessageType>()).ReturnsForAnyArgs(new LicenseDialogResult()
            {
                Key = key,
                Type = LicenseDialogResultType.Confirmed
            });

            var remoteMetaData = Substitute.For<IRemoteMetaData>();
            remoteMetaData.GetAppInfo().Returns(new App()
            {
                UseKeys = true
            });
            remoteMetaData.GetKeySecret(key, Arg.Any<string>()).Returns(keySecret);
            
            var command = new ValidateLicenseCommand(licenseDialog, remoteMetaData, _cache, _logger, _issueReporter);
            command.Prepare(_statusMonitor);
            command.Execute(CancellationToken.Empty);
            
            if (i == 0)
            {
                licenseDialog.Received(1).Display(Arg.Any<LicenseDialogMessageType>());
                Assert.IsTrue(_cache.Dictionary.ContainsValue(key));
                Assert.IsTrue(_cache.Dictionary.ContainsValue(keySecret));
            }
            else
            {
                licenseDialog.Received(1).SetKey(key);
                licenseDialog.DidNotReceive().Display(Arg.Any<LicenseDialogMessageType>());
                Assert.IsTrue(_cache.Dictionary.ContainsValue(key));
                Assert.IsTrue(_cache.Dictionary.ContainsValue(keySecret));
            }
        }
    }

    [Test]
    public void Execute_ProperlyHandlesSitauationWhenKeysAreNotUsed()
    {
        var licenseDialog = Substitute.For<ILicenseDialog>();
        
        var remoteMetaData = Substitute.For<IRemoteMetaData>();
        remoteMetaData.GetAppInfo().Returns(new App()
        {
            UseKeys = false
        });
        
        var command = new ValidateLicenseCommand(licenseDialog, remoteMetaData, _cache, _logger, _issueReporter);
        command.Prepare(_statusMonitor);
        command.Execute(CancellationToken.Empty);

        Assert.AreEqual(command.KeySecret, null);
        remoteMetaData.DidNotReceive().GetKeySecret(Arg.Any<string>(), Arg.Any<string>());
        licenseDialog.DidNotReceive().Display(Arg.Any<LicenseDialogMessageType>());
    }
    
    [TestCase(404, LicenseDialogMessageType.InvalidLicense)]
    [TestCase(403, LicenseDialogMessageType.ServiceUnavailable)]
    [TestCase(410, LicenseDialogMessageType.BlockedLicense)]
    public void Execute_DisplaysDialogMessageForApiError(int statusCode, LicenseDialogMessageType messageType)
    {
        const string key = "key";
        const string keySecret = "key-secret";
        
        var licenseDialog = Substitute.For<ILicenseDialog>();
        licenseDialog.Display(Arg.Any<LicenseDialogMessageType>()).ReturnsForAnyArgs(new LicenseDialogResult()
        {
            Key = key,
            Type = LicenseDialogResultType.Confirmed
        });

        var remoteMetaData = Substitute.For<IRemoteMetaData>();
        remoteMetaData.GetAppInfo().Returns(new App()
        {
            UseKeys = true
        });

        bool firstAttempt = true;
        
        remoteMetaData.GetKeySecret(key, Arg.Any<string>()).Returns(info =>
        {
            if (!firstAttempt)
            {
                return keySecret;
            }
            
            firstAttempt = false;
            throw new ApiResponseException(statusCode);
        });

        var command = new ValidateLicenseCommand(licenseDialog, remoteMetaData, _cache, _logger, _issueReporter);
        command.Prepare(_statusMonitor);
        command.Execute(CancellationToken.Empty);
        
        licenseDialog.Received(1).Display(LicenseDialogMessageType.None);
        licenseDialog.Received(1).Display(messageType);
        licenseDialog.DidNotReceive().Display(Arg.Is<LicenseDialogMessageType>(type => type != LicenseDialogMessageType.None &&
                                             type != messageType));
    }
    
    [Test]
    public void Execute_DisplaysProperDialogMessageForConnectionError()
    {
        const string key = "key";
        const string keySecret = "key-secret";
        
        var licenseDialog = Substitute.For<ILicenseDialog>();
        licenseDialog.Display(Arg.Any<LicenseDialogMessageType>()).ReturnsForAnyArgs(new LicenseDialogResult()
        {
            Key = key,
            Type = LicenseDialogResultType.Confirmed
        });

        var remoteMetaData = Substitute.For<IRemoteMetaData>();
        remoteMetaData.GetAppInfo().Returns(new App()
        {
            UseKeys = true
        });

        bool firstAttempt = true;
        
        remoteMetaData.GetKeySecret(key, Arg.Any<string>()).Returns(info =>
        {
            if (!firstAttempt)
            {
                return keySecret;
            }
            
            firstAttempt = false;
            throw new ApiConnectionException(new List<Exception>(), new List<Exception>());
        });

        var command = new ValidateLicenseCommand(licenseDialog, remoteMetaData, _cache, _logger, _issueReporter);
        command.Prepare(_statusMonitor);
        command.Execute(CancellationToken.Empty);
        
        licenseDialog.Received(1).Display(LicenseDialogMessageType.None);
        licenseDialog.Received(1).Display(LicenseDialogMessageType.ServiceUnavailable);
        licenseDialog.DidNotReceive().Display(Arg.Is<LicenseDialogMessageType>(type => type != LicenseDialogMessageType.None &&
                                                                                       type != LicenseDialogMessageType.ServiceUnavailable));
    }
}