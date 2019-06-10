using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher : MonoBehaviour
{
    private static Patcher _instance;

    [NotNull]
    private readonly List<Action> _dispatcher =
        new List<Action>();

    [NotNull]
    public static Patcher Instance
    {
        get
        {
            Assert.IsNotNull(value: _instance);
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        while (_dispatcher.Count > 0)
        {
            _dispatcher[0]?.Invoke();
            _dispatcher.RemoveAt(0);
        }
    }

    //TODO: Remove after moving to Legacy
    public void Quit()
    {
        RequestQuit();
    }

    //TODO: Remove after moving to Legacy
    public void CancelUpdateApp()
    {
        RequestCancelUpdateApp();
    }

    //TODO: Block application from quit without invoking Quit()
}