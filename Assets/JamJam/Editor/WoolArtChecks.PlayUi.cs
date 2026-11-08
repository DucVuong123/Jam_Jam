using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayUi()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null && scene.State.Status == RoundStatus.Playing && !scene.State.Paused, "WoolScene is playing");
            var juice = scene.PauseButton.GetComponent<WoolButtonJuice>();
            if (juice == null) juice = scene.PauseButton.gameObject.AddComponent<WoolButtonJuice>();
            var target = juice.transform;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            juice.OnPointerDown(pointer);
            Expect(juice.Animating && juice.Pressed, "Press starts the squash");
            juice.Step(0.03f);
            Expect(target.localScale.x < 1 && target.localScale.x > WoolButtonJuice.PressedScale, "Squash is half way after 0.03 s but scale was " + target.localScale.x);
            juice.Step(0.05f);
            Expect(Mathf.Abs(target.localScale.x - WoolButtonJuice.PressedScale) < 1e-5f && !juice.Animating, "Squash settles at 0.92 and unsubscribes");
            juice.OnPointerUp(pointer);
            juice.Step(0.09f);
            Expect(target.localScale.x > 1, "Release overshoots past 1 but scale was " + target.localScale.x);
            juice.Step(0.2f);
            Expect(target.localScale.x == 1 && !juice.Animating && !juice.Pressed, "Release returns exactly to 1");
            juice.OnPointerUp(pointer);
            Expect(!juice.Animating, "Release without a press does nothing");
            juice.OnPointerDown(pointer);
            juice.OnPointerExit(pointer);
            juice.Step(1);
            Expect(target.localScale.x == 1 && !juice.Pressed, "Pointer exit releases the button");
            var card = scene.PausePanel.transform.Find("Card");
            var group = scene.PausePanel.GetComponent<CanvasGroup>();
            if (group == null) group = scene.PausePanel.AddComponent<CanvasGroup>();
            var pop = card.GetComponent<WoolPopIn>();
            if (pop == null) { pop = card.gameObject.AddComponent<WoolPopIn>(); pop.Fade = group; }
            scene.Pause();
            Expect(scene.PausePanel.activeSelf && pop.Animating && Mathf.Abs(card.localScale.x - WoolPopIn.DefaultFromScale) < 1e-4f && group.alpha == 0, "Pause card starts at 0.8 and transparent");
            Expect(scene.RetryButton.gameObject.activeInHierarchy && scene.RetryButton.transform.parent == card, "Retry shows inside the pause card");
            pop.Step(0.1f);
            Expect(Mathf.Abs(group.alpha - 0.5f) < 0.01f && card.localScale.x > WoolPopIn.DefaultFromScale && card.localScale.x < 1.1f, "Overlay is half faded after 0.1 s");
            pop.Step(0.25f);
            Expect(Mathf.Abs(card.localScale.x - 1) < 0.01f && group.alpha == 1 && !pop.Animating, "Pause card settles at scale 1 and alpha 1");
            scene.Resume();
            Expect(!scene.PausePanel.activeSelf && card.localScale.x == 1 && !scene.State.Paused, "Resume hides the card at rest");
            Expect(Mathf.Abs(WoolLevelCard.PulseAt(0) - 1) < 1e-5f && Mathf.Abs(WoolLevelCard.PulseAt(0.6f) - 1.04f) < 1e-4f && Mathf.Abs(WoolLevelCard.PulseAt(1.2f) - 1) < 1e-4f, "Current card pulse runs 1 to 1.04 over 1.2 s");
            return "PLAY UI PASS / juice / pop-in / pulse";
        }

        public static string PlayResultArm(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            scene.Replay();
            var game = scene.State;
            for (var frame = 0; frame < 30000 && game.Status == RoundStatus.Playing; frame++)
            {
                if (win)
                    for (var b = 0; b < game.Blocks.Count; b++)
                        if (game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color)) { game.Select(b); break; }
                game.Step(0.02f);
            }
            Expect(game.Status == (win ? RoundStatus.Won : RoundStatus.Lost), "Round ends " + (win ? "Won" : "Lost") + " but was " + game.Status);
            Expect(!scene.ResultPending && !scene.ResultPanel.activeSelf, "Nothing is shown before the next Tick");
            Time.timeScale = 0.25f;
            return "ARMED / " + game.Status;
        }

        public static string PlayResultEarly(bool replay)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var delay = scene.State.Status == RoundStatus.Won ? WoolScene.WinResultDelay : WoolScene.LoseResultDelay;
            var elapsed = Time.timeAsDouble - scene.ResultQueuedAt;
            Expect(scene.ResultPending, "Result is queued after the round ends");
            Expect(!scene.ResultPanel.activeSelf && elapsed < delay, "ResultPanel is still hidden " + elapsed.ToString("F2") + " s after the end");
            if (replay)
            {
                scene.Replay();
                Expect(!scene.ResultPending && !scene.ResultPanel.activeSelf && scene.State.Status == RoundStatus.Playing, "Replay while waiting cancels the result");
            }
            Time.timeScale = 1;
            return "EARLY PASS / " + elapsed.ToString("F2") + " s" + (replay ? " / replayed" : "");
        }

        public static string PlayResultLate(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var delay = win ? WoolScene.WinResultDelay : WoolScene.LoseResultDelay;
            var elapsed = Time.timeAsDouble - scene.ResultQueuedAt;
            Expect(elapsed >= delay + 0.8f, "Wait longer: only " + elapsed.ToString("F2") + " s since the round ended");
            Expect(scene.ResultPanel.activeSelf && !scene.ResultPending, "ResultPanel is shown after the delay");
            Expect(scene.WonContent.activeSelf == win && scene.LostContent.activeSelf == !win && scene.NextButton.gameObject.activeSelf == win, "Result content matches " + (win ? "Won" : "Lost"));
            var card = scene.ResultPanel.transform.Find("Card");
            Expect(Mathf.Abs(card.localScale.x - 1) < 0.01f, "Result card settles at scale 1");
            return "PLAY RESULT PASS / " + (win ? "Won" : "Lost") + " / " + elapsed.ToString("F2") + " s";
        }

        public static string PlayResultQuiet()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(!scene.ResultPanel.activeSelf && !scene.ResultPending && scene.State.Status == RoundStatus.Playing, "Cancelled result never appears");
            return "PLAY RESULT QUIET PASS";
        }
    }
}
