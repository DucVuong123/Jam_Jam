using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        static int[] motionPicked = Array.Empty<int>();

        public static string PlayMotionArm(int level)
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null, "WoolScene is running");
            scene.LoadLevel(level);
            var game = scene.State;
            var data = scene.Levels[scene.CurrentLevel].Data;
            Expect(scene.Cushion != null &&
                Mathf.Abs(scene.Cushion.localScale.x - (data.Columns * WoolRouting.CellSize + WoolScene.CushionPad)) < 0.001f &&
                Mathf.Abs(scene.Cushion.localScale.z - (data.Rows * WoolRouting.CellSize + WoolScene.CushionPad)) < 0.001f,
                "Board Cushion fits level " + (scene.CurrentLevel + 1));
            var picked = new List<int>();
            for (var pass = 0; pass < 2 && picked.Count < 2; pass++)
                for (var b = 0; b < game.Blocks.Count && picked.Count < 2; b++)
                {
                    if (!game.CanSelect(b)) continue;
                    if (pass == 0 && picked.Count == 0)
                    {
                        var opposite = false;
                        for (var c = b + 1; c < game.Blocks.Count; c++)
                            if (game.CanSelect(c) && game.Blocks[c].Definition.Direction == WoolShape.Opposite(game.Blocks[b].Definition.Direction)) opposite = true;
                        if (!opposite) continue;
                    }
                    if (pass == 0 && picked.Count == 1 && game.Blocks[b].Definition.Direction != WoolShape.Opposite(game.Blocks[picked[0]].Definition.Direction)) continue;
                    scene.Select(b);
                    if (game.Blocks[b].Phase == 1) picked.Add(b);
                }

            Expect(picked.Count == 2, "Two clear blocks depart back to back");
            motionPicked = picked.ToArray();
            Time.timeScale = 0.1f;
            return "MOTION ARMED / blocks " + picked[0] + " " + picked[1];
        }

        public static string PlayMotionMid()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            Expect(motionPicked.Length == 2, "Run PlayMotionArm first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            var feel = scene.GetComponent<WoolFeel>();
            var game = scene.State;
            var a = game.Blocks[motionPicked[0]];
            var b = game.Blocks[motionPicked[1]];
            Expect(a.Phase == 1 && b.Phase == 1, "Both blocks are still on their routes");
            Expect(a.Travel > WoolFeel.LaunchPopTime && b.Travel > WoolFeel.LaunchPopTime, "Both blocks advanced (travel " + a.Travel.ToString("F3") + " / " + b.Travel.ToString("F3") + ")");
            var seen = 0;
            foreach (var view in UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None))
            {
                if (view.Index != motionPicked[0] && view.Index != motionPicked[1]) continue;
                var block = game.Blocks[view.Index];
                WoolRouting.Evaluate(block.Route, block.Travel * WoolRouting.Speed, out var point, out _);
                var position = view.transform.position;
                Expect(Mathf.Abs(position.x - point.X) < 0.001f && Mathf.Abs(position.z - point.Z) < 0.001f && Mathf.Abs(position.y - 0.12f) < 0.0001f, "Block " + view.Index + " rides flat on its route");
                Expect(Vector3.Distance(view.transform.localScale, Vector3.one) < 0.0001f && feel.BlockScale(view.Index) == 1, "Block " + view.Index + " keeps full size");
                Expect(Vector3.Dot(view.transform.up, Vector3.up) > 0.9999f, "Block " + view.Index + " only turns around the vertical axis");
                seen++;
            }
            Expect(seen == 2, "Both moving block views are visible");
            return "MOTION MID PASS / travel " + a.Travel.ToString("F2") + " s";
        }

        public static string PlayMotionEnd()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            Expect(motionPicked.Length == 2, "Run PlayMotionArm first");
            var game = UnityEngine.Object.FindFirstObjectByType<WoolScene>().State;
            foreach (var index in motionPicked)
            {
                var block = game.Blocks[index];
                var slot = WoolRouting.SlotCenter(block.Slot);
                var end = block.Route[block.Route.Count - 1];
                Expect(block.Phase == 2 && block.Travel == block.Duration && end.X == slot.X && end.Z == slot.Z, "Block " + index + " reached slot " + block.Slot);
                Expect(game.Spools[block.Slot].Block != index || game.Spools[block.Slot].Arrival == 0, "Spool " + block.Slot + " arrived for block " + index);
            }
            Time.timeScale = 1;
            var result = "MOTION END PASS / slots " + game.Blocks[motionPicked[0]].Slot + " " + game.Blocks[motionPicked[1]].Slot;
            motionPicked = Array.Empty<int>();
            return result;
        }
    }
}
