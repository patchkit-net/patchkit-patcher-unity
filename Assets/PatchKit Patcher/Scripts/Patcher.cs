using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher : MonoBehaviour
{
    private static Patcher _instance;

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