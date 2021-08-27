using System;
using System.Collections;
using Newtonsoft.Json;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.UI;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class RealeasesList : MonoBehaviour
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RealeasesList));

        private RealeasesElement[] RealeasesElements;
        public void AddButtons(ChangelogEntry[] changelogEntrys)
        {
            RealeasesElements = new RealeasesElement[changelogEntrys.Length];

            for(int i = 0; i < changelogEntrys.Length; i++)
            {
                RealeasesElements[i].Text.SetText(changelogEntrys[i].VersionId.ToString());
            }
            throw new NotImplementedException();
        }
    }
}
