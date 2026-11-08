using System;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamJam
{
    public sealed class WoolLoading : MonoBehaviour
    {
        public const float DefaultMinimumSeconds = 1.6f;
        public const float LoadedProgress = 0.9f;
        public const float Follow = 10f;
        public const float Snap = 0.002f;
        public const float MaxDelta = 0.1f;
        public RectTransform Fill;
        public Text Label;
        public string NextScene = WoolSession.LobbyScene;
        public float MinimumSeconds = DefaultMinimumSeconds;
        IDisposable clock;
        AsyncOperation operation;
        Image fillImage;
        float elapsed;

        public float Displayed { get; private set; }
        public float Elapsed => elapsed;
        public bool Activated { get; private set; }

        void Start()
        {
            Show(0);
            operation = SceneManager.LoadSceneAsync(NextScene);
            if (operation == null) return;
            operation.allowSceneActivation = false;
            clock = Observable.EveryUpdate(UnityFrameProvider.Update).Subscribe(_ => Tick());
        }

        void Tick()
        {
            var delta = Time.unscaledDeltaTime;
            if (!(delta > 0) || float.IsInfinity(delta)) delta = 0;
            delta = Mathf.Min(delta, MaxDelta);
            elapsed += delta;
            var loaded = WoolEase.Clamp(operation.progress / LoadedProgress);
            var timed = MinimumSeconds > 0 ? WoolEase.Clamp(elapsed / MinimumSeconds) : 1;
            var target = Mathf.Min(loaded, timed);
            var next = Displayed + (target - Displayed) * (1 - Mathf.Exp(-Follow * delta));
            if (target - next < Snap) next = target;
            Show(next);
            if (next < 1) return;
            Stop();
            Activated = true;
            operation.allowSceneActivation = true;
        }

        public void Show(float value)
        {
            Displayed = WoolEase.Clamp(value);
            if (Label != null) Label.text = "Loading " + Mathf.RoundToInt(Displayed * 100) + "%";
            if (Fill == null) return;
            if (fillImage == null) fillImage = Fill.GetComponent<Image>();
            var bar = Fill.parent as RectTransform;
            var span = bar != null ? Mathf.Max(0, bar.rect.width - 2 * Fill.anchoredPosition.x) : 0;
            var height = Fill.rect.height;
            var sprite = fillImage != null ? fillImage.sprite : null;
            var minWidth = 0f;
            if (sprite != null && height > 0)
            {
                fillImage.pixelsPerUnitMultiplier = sprite.rect.height / height;
                minWidth = (sprite.border.x + sprite.border.z) / (sprite.pixelsPerUnit / 100f * fillImage.pixelsPerUnitMultiplier);
            }
            var width = Displayed <= 0 ? 0 : Mathf.Min(span, Mathf.Max(minWidth, span * Displayed));
            Fill.sizeDelta = new Vector2(width, Fill.sizeDelta.y);
        }

        void Stop()
        {
            clock?.Dispose();
            clock = null;
        }

        void OnDestroy()
        {
            Stop();
        }
    }
}
