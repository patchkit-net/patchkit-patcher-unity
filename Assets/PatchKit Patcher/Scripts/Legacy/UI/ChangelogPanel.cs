using UnityEngine;
using UnityEngine.Assertions;

namespace Legacy.UI
{
[RequireComponent(requiredComponent: typeof(Animator))]
public class ChangelogPanel : MonoBehaviour
{
    private Animator _animator;

    private static readonly int AnimatorIsOpened =
        Animator.StringToHash("IsOpened");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        Assert.IsNotNull(value: _animator);

        Switch(isOpened: false);
    }

    public void Open()
    {
        Switch(isOpened: true);
    }

    public void Close()
    {
        Switch(isOpened: false);
    }

    private void Switch(bool isOpened)
    {
        Assert.IsNotNull(value: _animator);

        _animator.SetBool(
            id: AnimatorIsOpened,
            value: isOpened);
    }
}
}