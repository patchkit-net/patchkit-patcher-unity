using PatchKit.Unity.Patcher.UI.Animations;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEventsController : MonoBehaviour, IBeginDragHandler, IScrollHandler
{
    public AnimationManager AnimationManager;

    public void OnBeginDrag(PointerEventData eventData)
    {
        AnimationManager.StopScrolling();
    }

    public void OnScroll(PointerEventData eventData)
    {
        AnimationManager.StopScrolling();
    }
}
