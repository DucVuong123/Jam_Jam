using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam
{
    public sealed class WoolBlockView : MonoBehaviour, IPointerClickHandler
    {
        public WoolScene Owner;
        public int Index;
        public Transform Body;
        public Transform Arrow;
        public float Shake;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Owner != null) Owner.Select(Index);
        }
    }
}
