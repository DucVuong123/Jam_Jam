using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayFeel()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is running");
            var feel = scene.GetComponent<WoolFeel>();
            Expect(feel != null, "WoolFeel is added at runtime");
            var camera = Camera.main;
            var origin = camera.transform.position;
            var rotation = camera.transform.rotation;
            var size = camera.orthographicSize;
            Expect(feel.Trauma == 0, "Fresh level starts without trauma");
            Physics.SyncTransforms();
            var views = Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None);
            var blocked = -1;
            foreach (var view in views)
            {
                if (scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                blocked = view.Index;
                break;
            }
            Expect(blocked >= 0, "A blocked block was clicked through EventSystem");
            Expect(feel.Trauma > 0.17f, "Blocked click adds trauma");
            feel.Step(scene.State, 0.05f);
            Expect(feel.BlockFlash(blocked) > 0, "Blocked block flashes red");
            Expect(Vector3.Distance(camera.transform.position, origin) > 0.0001f, "Camera leaves its origin while shaking");
            for (var i = 0; i < 30; i++) feel.Step(scene.State, 0.05f);
            Expect(Vector3.Distance(camera.transform.position, origin) < 0.0001f && Quaternion.Angle(camera.transform.rotation, rotation) < 0.01f && Mathf.Abs(camera.orthographicSize - size) < 0.0001f, "Camera returns exactly to its origin after 1.5 s");
            Expect(feel.BlockFlash(blocked) == 0 && feel.Trauma == 0, "Flash and trauma settle");
            var launched = -1;
            foreach (var view in views)
            {
                if (!scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                launched = view.Index;
                break;
            }
            Expect(launched >= 0, "A clear block was clicked through EventSystem");
            var puffs = feel.PuffCount;
            feel.Step(scene.State, 0.02f);
            Expect(feel.PuffCount > puffs, "Launch emits Puff particles");
            feel.Step(scene.State, 0.02f);
            Expect(feel.BlockScale(launched) > 1, "Launched block pops");
            for (var i = 0; i < 10; i++) { scene.State.Step(0.02f); feel.Step(scene.State, 0.02f); }
            Expect(feel.BlockScale(launched) == 1 && Mathf.Abs(scene.BlockAt(launched).y - 0.12f) < 0.0001f, "Flying block is back to full size and stays flat");
            feel.Blocked(blocked);
            feel.Step(scene.State, 0.02f);
            Expect(Vector3.Distance(camera.transform.position, origin) > 0.0001f, "Camera shakes before replay");
            scene.Replay();
            Expect(camera.transform.position == origin && camera.transform.rotation == rotation && camera.orthographicSize == size && feel.Trauma == 0, "Replay mid-shake restores the camera exactly");
            return "PLAY FEEL PASS / blocked " + blocked + " / launched " + launched;
        }

        public static string PlayFinish(bool win)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var feel = scene.GetComponent<WoolFeel>();
            var game = scene.State;
            var camera = Camera.main;
            var origin = camera.transform.position;
            var size = camera.orthographicSize;
            for (var frame = 0; frame < 30000 && game.Status == RoundStatus.Playing; frame++)
            {
                if (win)
                    for (var b = 0; b < game.Blocks.Count; b++)
                        if (game.CanSelect(b) && game.HasVisibleColor(game.Blocks[b].Definition.Color)) { game.Select(b); break; }
                game.Step(0.02f);
            }
            Expect(game.Status == (win ? RoundStatus.Won : RoundStatus.Lost), "Round ends " + (win ? "Won" : "Lost") + " but was " + game.Status);
            feel.Step(game, 0.02f);
            feel.Step(game, 0.02f);
            feel.Step(game, 0.02f);
            if (win) Expect(feel.SparkleCount > 0 && feel.CameraZoom > 0 && camera.orthographicSize < size, "Win sparkles and zooms in");
            else Expect(feel.Trauma > 0 && feel.DragonScale > 1 && feel.PuffCount > 0, "Loss shakes, swells the dragon and puffs");
            for (var i = 0; i < 60; i++) feel.Step(game, 0.02f);
            Expect(Vector3.Distance(camera.transform.position, origin) < 0.0001f && Mathf.Abs(camera.orthographicSize - size) < 0.0001f, "Camera settles back to its origin");
            Expect(feel.CameraZoom == 0 && feel.DragonScale == 1, "Zoom and dragon punch settle");
            return "PLAY FINISH PASS / " + game.Status;
        }
    }
}
