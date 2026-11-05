using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam
{
    public sealed class WoolSlotView : MonoBehaviour, IPointerClickHandler
    {
        public WoolScene Owner;
        public int Index;
        public TextMesh Label;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Owner != null && Index >= Owner.State.OpenSlots) Owner.Unlock();
        }
    }
}
