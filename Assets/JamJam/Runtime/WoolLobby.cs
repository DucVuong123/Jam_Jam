using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLobby : MonoBehaviour
    {
        public WoolLevel[] Levels;
        public WoolLevelCard CardPrefab;
        public RectTransform CardContainer;
        public ScrollRect Scroll;
        public RectTransform Kitten;
        public GameObject LobbyPanel;
        public GameObject LevelsPanel;
        public GameObject SettingsPanel;
        public GameObject ConfirmPanel;
        public Button PlayButton;
        public Text PlayLabel;
        public Button LevelsButton;
        public Button SettingsButton;
        public Button BackButton;
        public Button CloseSettingsButton;
        public Button ResetButton;
        public Button ConfirmYesButton;
        public Button ConfirmNoButton;
        public Toggle SoundToggle;
        public Toggle MusicToggle;
        public Toggle VibrationToggle;
        readonly List<WoolLevelCard> cards=new List<WoolLevelCard>();
        WoolProgress progress;
        WoolSettings settings;
        IDisposable clock;
        Vector2 kittenRest;
        double lastTime;
        float elapsed;
        bool loading;

        void Awake()
        {
            var prefs=new WoolPlayerPrefs();
            progress=new WoolProgress(prefs,Levels.Length);
            settings=new WoolSettings(prefs);
            kittenRest=Kitten.anchoredPosition;
        }

        void OnEnable()
        {
            lastTime=Time.unscaledTimeAsDouble;
            clock?.Dispose();
            clock=Observable.Interval(TimeSpan.FromSeconds(1d/60d),UnityTimeProvider.UpdateIgnoreTimeScale).Subscribe(_=>Tick());
        }

        void OnDisable() { clock?.Dispose(); clock=null; }

        void Start()
        {
            PlayButton.onClick.AddListener(Play);
            LevelsButton.onClick.AddListener(ShowLevels);
            BackButton.onClick.AddListener(ShowLobby);
            SettingsButton.onClick.AddListener(OpenSettings);
            CloseSettingsButton.onClick.AddListener(CloseSettings);
            ResetButton.onClick.AddListener(AskReset);
            ConfirmYesButton.onClick.AddListener(ConfirmReset);
            ConfirmNoButton.onClick.AddListener(CancelReset);
            SoundToggle.onValueChanged.AddListener(value=>settings.Sound=value);
            MusicToggle.onValueChanged.AddListener(value=>settings.Music=value);
            VibrationToggle.onValueChanged.AddListener(value=>settings.Vibration=value);
            for(var i=0;i<Levels.Length;i++)
            {
                var card=Instantiate(CardPrefab,CardContainer);
                card.name="LevelCard_"+(i+1).ToString("00");
                var index=i;
                card.Button.onClick.AddListener(()=>Choose(index));
                cards.Add(card);
            }
            SettingsPanel.SetActive(false);
            ConfirmPanel.SetActive(false);
            ShowLobby();
        }

        public void Play() { Load(progress.Current); }

        public void Choose(int index)
        {
            if(index<0 || index>=cards.Count) return;
            if(!progress.IsUnlocked(index)) { cards[index].Shake=0.35f; return; }
            Load(index);
        }

        public void ShowLobby() { LobbyPanel.SetActive(true); LevelsPanel.SetActive(false); Refresh(); }
        public void ShowLevels() { LobbyPanel.SetActive(false); LevelsPanel.SetActive(true); Refresh(); ScrollTo(progress.Current); }

        public void ScrollTo(int index)
        {
            if (Scroll == null || cards.Count == 0) return;
            index = Mathf.Clamp(index, 0, cards.Count - 1);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(CardContainer);
            var viewport = Scroll.viewport != null ? Scroll.viewport : (RectTransform)Scroll.transform;
            var range = CardContainer.rect.height - viewport.rect.height;
            Scroll.velocity = Vector2.zero;
            if (range <= 0) { Scroll.verticalNormalizedPosition = 1; return; }
            var card = (RectTransform)cards[index].transform;
            var center = -card.anchoredPosition.y - card.rect.height * (0.5f - card.pivot.y);
            var offset = Mathf.Clamp(center - viewport.rect.height * 0.5f, 0, range);
            Scroll.verticalNormalizedPosition = 1 - offset / range;
        }

        public void OpenSettings()
        {
            SoundToggle.SetIsOnWithoutNotify(settings.Sound);
            MusicToggle.SetIsOnWithoutNotify(settings.Music);
            VibrationToggle.SetIsOnWithoutNotify(settings.Vibration);
            ConfirmPanel.SetActive(false);
            SettingsPanel.SetActive(true);
        }

        public void CloseSettings() { ConfirmPanel.SetActive(false); SettingsPanel.SetActive(false); }
        public void AskReset() { ConfirmPanel.SetActive(true); }
        public void CancelReset() { ConfirmPanel.SetActive(false); }

        public void ConfirmReset()
        {
            progress.Reset();
            ConfirmPanel.SetActive(false);
            Refresh();
        }

        void Load(int index)
        {
            if(loading) return;
            loading=true;
            WoolSession.SelectedLevel=index;
            SceneManager.LoadScene(WoolSession.GameScene);
        }

        void Refresh()
        {
            PlayLabel.text="PLAY  "+(progress.Current+1).ToString("00");
            for(var i=0;i<cards.Count;i++)
            {
                var cleared=progress.IsCleared(i);
                cards[i].Bind(i,Levels[i].Title,progress.IsUnlocked(i),cleared,i==progress.Current && !cleared);
            }
        }

        void Tick()
        {
            var now=Time.unscaledTimeAsDouble;
            var delta=Mathf.Clamp((float)(now-lastTime),0,0.1f);
            lastTime=now;
            elapsed+=delta;
            Kitten.anchoredPosition=kittenRest+Vector2.up*(Mathf.Sin(elapsed*3)*12);
            foreach(var card in cards) card.Animate(delta);
        }
    }
}
