/*
 * Copyright (c) The Knights of Unity
 * http://theknightsofunity.com/
 */

using PatchKit.Logging;
using PatchKit.Patching.AppData.Local;
using PatchKit.Patching.Cancellation;
using UnityEditor;
using UnityEngine;

public class PatchFile : MonoBehaviour
{
    [MenuItem("Tools/Patch File")]
    public static void Do()
    {
        new RsyncFilePatcher(new DummyLogger()).Patch(@"g:\temp\coda\resources.assets", @"g:\temp\coda\18_diff\Client_Data\resources.assets",
            @"g:\temp\coda\resources.assets.18", CancellationToken.Empty);
    }
}