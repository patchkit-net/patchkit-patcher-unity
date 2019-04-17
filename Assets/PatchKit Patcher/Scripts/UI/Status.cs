﻿using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class Status : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: Text);

            switch (state.Kind)
            {
                case PatcherStateKind.Initializing:
                    Text.text = "Initializing...";
                    break;
                case PatcherStateKind.UpdatingApp:
                    Text.text = "Updating...";
                    break;
                case PatcherStateKind.StartingApp:
                    Text.text = "Starting...";
                    break;
                case PatcherStateKind.Quitting:
                    Text.text = "Quitting...";
                    break;
                default:
                    Text.text = string.Empty;
                    break;
            }
        };
    }
}
}