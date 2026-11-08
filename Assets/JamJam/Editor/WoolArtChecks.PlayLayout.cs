using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayLayout()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            scene.LoadLevel(0);
            var feel = scene.GetComponent<WoolFeel>();
            Expect(feel != null, "WoolFeel is added at runtime");
            var camera = Camera.main;
            Expect(camera != null, "Main camera exists");
            var rotation = Quaternion.Euler(WoolStage.Pitch, 0, 0);
            var position = new Vector3(0, 0, WoolStage.TargetZ) - rotation * Vector3.forward * WoolStage.Distance;
            ExpectStagePose(camera, position, rotation, WoolStage.Size(camera.aspect), "Level load");
            Expect(Mathf.Abs(scene.CameraFitAspect - camera.aspect) < 1e-4f, "Scene remembers the fitted aspect");
            try
            {
                feel.Blocked(0);
                feel.Step(scene.State, 0.02f);
                Expect(feel.Trauma > 0 && Vector3.Distance(camera.transform.position, position) > 0.0001f, "Camera shakes before the aspect change");
                camera.aspect = 0.45f;
                scene.FitCamera();
                Expect(Mathf.Abs(scene.CameraFitAspect - 0.45f) < 1e-4f, "Scene refits to the new aspect");
                Expect(feel.Trauma > 0, "Shake keeps running through the refit");
                feel.Blocked(0);
                for (var i = 0; i < 30; i++) feel.Step(scene.State, 0.05f);
                Expect(feel.Trauma == 0, "Trauma settles after 1.5 s");
                ExpectStagePose(camera, position, rotation, WoolStage.Size(0.45f), "After shake at aspect 0.45");
            }
            finally
            {
                camera.ResetAspect();
                scene.FitCamera();
            }
            ExpectStagePose(camera, position, rotation, WoolStage.Size(camera.aspect), "After aspect reset");
            return "PLAY LAYOUT PASS";
        }

        public static string PlayThread()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            scene.LoadLevel(0);
            var game = scene.State;
            var render = typeof(WoolScene).GetMethod("RenderFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Expect(render != null, "WoolScene.RenderFrame exists");
            var block = -1;
            for (var b = 0; b < game.Blocks.Count; b++)
                if (game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color) && game.Select(b)) { block = b; break; }
            Expect(block >= 0, "A valid block launches on level 0");
            var slot = game.Blocks[block].Slot;
            var spool = game.Spools[slot];
            const float delta = 1f / 60f;
            var frames = 0;
            while (spool.Target < 0 && frames < 6000) { game.Step(delta); render.Invoke(scene, new object[] { delta }); frames++; }
            Expect(spool.Target >= 0, "Spool " + slot + " gets a target");
            var line = scene.World.Find("Thread_" + slot);
            Expect(line != null && line.GetComponent<LineRenderer>() != null, "Thread_" + slot + " line exists");
            Expect(Mathf.Abs(line.GetComponent<LineRenderer>().startWidth - 0.2f) < 1e-5f, "Thread start width 0.2 but was " + line.GetComponent<LineRenderer>().startWidth);
            Expect(scene.ThreadVisible(slot), "Thread shows once the spool has a target");
            var previous = scene.ThreadEnd(slot);
            var wasVisible = true;
            var hidden = 0;
            var untargeted = 0;
            var counted = 0;
            var maxStep = 0f;
            while (spool.Remaining > 0 && counted < 20000)
            {
                Expect(game.Status == RoundStatus.Playing, "Round still playing while spool " + slot + " pulls, status " + game.Status);
                game.Step(delta);
                render.Invoke(scene, new object[] { delta });
                counted++;
                if (spool.Remaining == 0) break;
                var visible = scene.ThreadVisible(slot);
                if (spool.Target >= 0) Expect(visible, "Thread visible at frame " + counted + " while target " + spool.Target + " and remaining " + spool.Remaining);
                else untargeted++;
                if (!visible) hidden++;
                var end = scene.ThreadEnd(slot);
                if (visible && wasVisible)
                {
                    var step = Vector3.Distance(previous, end);
                    maxStep = Mathf.Max(maxStep, step);
                    Expect(step <= 0.5f, "Thread end jumped " + step.ToString("F3") + " at frame " + counted);
                }
                previous = end;
                wasVisible = visible;
            }
            Expect(spool.Remaining == 0, "Spool " + slot + " empties within the frame budget");
            Expect(hidden == untargeted, "Thread hidden " + hidden + " frames but untargeted " + untargeted);
            Expect(!scene.ThreadVisible(slot), "Thread hides once the spool is empty");
            return "PLAY THREAD PASS / " + counted + " frames / max step " + maxStep.ToString("F3");
        }

        public static string PlaySpeed()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var tick = typeof(WoolScene).GetMethod("Tick", flags);
            var last = typeof(WoolScene).GetField("lastTime", flags);
            Expect(tick != null && last != null, "WoolScene.Tick and lastTime exist");
            try
            {
                WoolScene.ResetSpeed();
                scene.LoadLevel(0);
                Expect(WoolScene.Speed == 1 && scene.SpeedLabel != null && scene.SpeedLabel.text == "x1", "Speed starts at x1");
                var game = scene.State;
                var start = game.Elapsed;
                RunTicks(scene, tick, last, 20, 0.05);
                var normal = game.Elapsed - start;
                scene.ToggleSpeed();
                Expect(WoolScene.Speed == 2 && scene.SpeedLabel.text == "x2", "Toggle shows x2");
                start = game.Elapsed;
                RunTicks(scene, tick, last, 20, 0.05);
                var fast = game.Elapsed - start;
                var ratio = fast / normal;
                Expect(normal > 0.9f && ratio >= 1.8f && ratio <= 2.2f, "x2 doubles Elapsed: x1 " + normal.ToString("F3") + " x2 " + fast.ToString("F3"));
                scene.Pause();
                start = game.Elapsed;
                RunTicks(scene, tick, last, 10, 0.05);
                Expect(game.Elapsed == start, "Pause holds Elapsed at x2");
                scene.Resume();
                start = game.Elapsed;
                RunTicks(scene, tick, last, 1, 0.1);
                var longFrame = game.Elapsed - start;
                Expect(longFrame >= 0.19f && longFrame <= 0.21f, "A 0.1 s frame at x2 advances 0.2 s but advanced " + longFrame.ToString("F3"));
                start = game.Elapsed;
                RunTicks(scene, tick, last, 1, 0.5);
                var clamped = game.Elapsed - start;
                Expect(clamped >= 0.19f && clamped <= 0.21f, "A 0.5 s hitch at x2 is clamped to 0.2 s but advanced " + clamped.ToString("F3"));
                scene.LoadLevel(1);
                Expect(WoolScene.Speed == 2 && scene.SpeedLabel.text == "x2", "Speed and label stay x2 after LoadLevel");
                return "PLAY SPEED PASS / x1 " + normal.ToString("F2") + " s / x2 " + fast.ToString("F2") + " s / long frame " + longFrame.ToString("F2") + " s";
            }
            finally
            {
                if (WoolScene.Speed != 1) scene.ToggleSpeed();
                WoolScene.ResetSpeed();
            }
        }

        public static string PlayUnlock()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            scene.LoadLevel(0);
            var game = scene.State;
            Expect(game.OpenSlots == 4 && game.Spools.Count == 6, "Level 0 opens 4 of 6 slots");
            scene.UnlockButton(0, out var zero);
            scene.UnlockButton(4, out var four);
            scene.UnlockButton(5, out var five);
            Expect(zero != null && four != null && five != null && four.name == "Unlock_4", "One Unlock button per slot");
            Expect(!zero.gameObject.activeSelf, "Open slot 0 has no Unlock button");
            Expect(four.gameObject.activeSelf && four.interactable, "Unlock_4 is active and clickable");
            Expect(five.gameObject.activeSelf && !five.interactable && Mathf.Abs(five.GetComponent<CanvasGroup>().alpha - WoolScene.UnlockDisabledAlpha) < 1e-4f, "Unlock_5 shows faded and not clickable");
            var camera = Camera.main;
            var pad = camera.WorldToScreenPoint(WoolScene.SlotPosition(4));
            var button = RectTransformUtility.WorldToScreenPoint(null, four.transform.position);
            Expect(button.y < pad.y && Mathf.Abs(button.x - pad.x) <= 120, "Unlock_4 sits under pad 4: button " + button + " pad " + (Vector2)pad);
            var neighbour = RectTransformUtility.WorldToScreenPoint(null, five.transform.position);
            var width = ((RectTransform)four.transform).rect.width * four.transform.lossyScale.x;
            Expect(Mathf.Abs(neighbour.x - button.x) >= width, "Unlock_4 and Unlock_5 do not overlap: gap " + Mathf.Abs(neighbour.x - button.x).ToString("F0") + " width " + width.ToString("F0"));
            scene.Pause();
            four.onClick.Invoke();
            Expect(game.OpenSlots == 4, "Unlock does nothing while paused");
            scene.Resume();
            five.onClick.Invoke();
            Expect(game.OpenSlots == 4, "Unlock_5 cannot skip slot 4");
            four.onClick.Invoke();
            four.onClick.Invoke();
            Expect(game.OpenSlots == 5, "Two quick taps on Unlock_4 open only slot 4 but OpenSlots is " + game.OpenSlots);
            var render = typeof(WoolScene).GetMethod("RenderFrame", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            render.Invoke(scene, new object[] { 0f });
            Expect(!four.gameObject.activeSelf && five.gameObject.activeSelf && five.interactable && five.GetComponent<CanvasGroup>().alpha == 1, "Unlock_4 hides and Unlock_5 becomes clickable");
            return "PLAY UNLOCK PASS / button " + button.ToString("F0") + " pad " + ((Vector2)pad).ToString("F0");
        }

        static void RunTicks(WoolScene scene, System.Reflection.MethodInfo tick, System.Reflection.FieldInfo last, int count, double delta)
        {
            for (var i = 0; i < count; i++)
            {
                last.SetValue(scene, Time.timeAsDouble - delta);
                tick.Invoke(scene, null);
            }
        }

        static void ExpectStagePose(Camera camera, Vector3 position, Quaternion rotation, float size, string label)
        {
            Expect(Vector3.Distance(camera.transform.position, position) < 1e-4f, label + ": camera position " + camera.transform.position.ToString("F4") + " expected " + position.ToString("F4"));
            Expect(Quaternion.Angle(camera.transform.rotation, rotation) < 0.01f, label + ": camera rotation " + camera.transform.eulerAngles.ToString("F2"));
            Expect(Mathf.Abs(camera.orthographicSize - size) < 1e-4f, label + ": camera size " + camera.orthographicSize.ToString("F4") + " expected " + size.ToString("F4"));
        }
    }
}
