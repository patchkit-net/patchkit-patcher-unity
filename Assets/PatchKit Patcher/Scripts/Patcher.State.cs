using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

public partial class Patcher
{
    private PatcherState _state;

    [NotNull]
    public PatcherState State
    {
        get
        {
            Assert.IsNotNull(value: _state);
            return _state;
        }
    }

    public delegate void OnPatcherStateChanged([NotNull] PatcherState state);

    public event OnPatcherStateChanged StateChanged;

    private void OnStateChanged()
    {
        // TODO: Manually invoke through invocation list, and surround with try
        StateChanged?.Invoke(state: State);
    }

    private void ModifyState([NotNull] Action x)
    {
        lock (State)
        {
            x();
            State.HasChanged = true;
        }
    }

    private void UpdateState()
    {
        if (State.HasChanged)
        {
            State.HasChanged = false;
            OnStateChanged();
        }
    }
}