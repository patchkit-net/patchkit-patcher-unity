using System;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

class ValidateLicenseCommandTest
{
    [Test]
    public void Execute()
    {
        var licenseDialog = Substitute.For<ILicenseDialog>();
        licenseDialog.Display(Arg.Any<LicenseDialogMessageType>()).Returns(info => new LicenseDialogResult
        {
            Key = "correct_key",
            Type = LicenseDialogResultType.Confirmed
        });

        var remoteMetaData = Substitute.For<IRemoteMetaData>();
        remoteMetaData.GetKeySecret("correct_key").Returns("correct_key_secret");

        var progressMonitor = Substitute.For<IStatusMonitor>();
        progressMonitor.CreateGeneralStatusReporter(Arg.Any<double>()).Returns(info =>
        {
            var generalStatusRepo
        })

        var command = new ValidateLicenseCommand(licenseDialog, remoteMetaData);


    }
}