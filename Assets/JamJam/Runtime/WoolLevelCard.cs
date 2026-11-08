using UnityEngine;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLevelCard : MonoBehaviour
    {
        public const float PulseScale = 0.04f;
        public const float PulsePeriod = 1.2f;
        public Button Button;
        public RectTransform Content;
        public CanvasGroup Group;
        public Text Number;
        public Text Title;
        public GameObject Check;
        public GameObject Lock;
        public GameObject Current;
        public Image Face;
        public Image Edge;
        public Sprite ClearedArt;
        public Sprite CurrentArt;
        public Sprite LockedArt;
        public Sprite Plain;
        public float ArtScale=1;
        public float Shake;
        bool pulsing;
        float pulse;

        public bool Pulsing => pulsing;

        public void Bind(int index,string title,bool unlocked,bool cleared,bool current)
        {
            Number.text=(index+1).ToString("00");
            Title.text=unlocked?title:"Locked";
            Check.SetActive(cleared);
            Lock.SetActive(!unlocked);
            Current.SetActive(current);
            Group.alpha=unlocked?1:WoolUiTheme.LockedAlpha;
            WoolUiTheme.CardColors(unlocked,cleared,current,out var face,out var edge);
            var art=cleared?ClearedArt:unlocked?CurrentArt:LockedArt;
            if(Edge!=null)
            {
                if(art!=null) { Edge.sprite=art; Edge.color=Color.white; Edge.pixelsPerUnitMultiplier=ArtScale; }
                else { if(Plain!=null) Edge.sprite=Plain; Edge.color=edge; Edge.pixelsPerUnitMultiplier=1; }
            }
            if(Face!=null) { Face.color=face; Face.enabled=art==null; }
            var juice=GetComponent<WoolButtonJuice>();
            if(juice!=null) juice.enabled=unlocked;
            pulsing=current;
            if(pulsing) return;
            pulse=0;
            Content.localScale=Vector3.one;
        }

        public void Animate(float delta)
        {
            Shake=Mathf.Max(0,Shake-delta);
            Content.anchoredPosition=new Vector2(Mathf.Sin(Shake*95)*Shake*40,0);
            if(!pulsing) return;
            pulse=Mathf.Repeat(pulse+delta,PulsePeriod);
            Content.localScale=Vector3.one*PulseAt(pulse);
        }

        public static float PulseAt(float time)
        {
            return 1+PulseScale*0.5f*(1-Mathf.Cos(time/PulsePeriod*Mathf.PI*2));
        }
    }
}
