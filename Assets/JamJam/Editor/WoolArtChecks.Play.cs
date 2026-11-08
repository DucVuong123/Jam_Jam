using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string PlayBlocks()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = UnityEngine.Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is running");
            var picked = -1;
            for (var i = 0; i < scene.Levels.Length && picked < 0; i++)
            {
                var fresh = new WoolGame(scene.Levels[i].Data);
                for (var b = 0; b < fresh.Blocks.Count; b++)
                    if (!fresh.CanSelect(b)) { picked = i; break; }
            }
            if (picked < 0) throw new System.InvalidOperationException("no level has a blocked block");
            scene.LoadLevel(picked);
            Physics.SyncTransforms();
            var tint = new MaterialPropertyBlock();
            var views = UnityEngine.Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None);
            foreach (var view in views)
            {
                var definition = scene.State.Blocks[view.Index].Definition;
                var collider = view.GetComponent<BoxCollider>();
                var cell = WoolRouting.CellSize;
                Expect(Vector3.Distance(collider.size, new Vector3((definition.Width - WoolBlockMesh.Inset * 2) * cell, WoolBlockMesh.Thickness + 0.2f, (definition.Height - WoolBlockMesh.Inset * 2) * cell)) < 0.001f, "Collider size of block " + view.Index);
                var filter = view.Body.GetComponent<MeshFilter>();
                Expect(filter != null && filter.sharedMesh != null, "Block " + view.Index + " body mesh");
                var size = filter.sharedMesh.bounds.size;
                Expect(Mathf.Abs(size.x - (definition.Width - WoolBlockMesh.Inset * 2) * cell) < 0.01f && Mathf.Abs(size.z - (definition.Height - WoolBlockMesh.Inset * 2) * cell) < 0.01f, "Block " + view.Index + " mesh matches " + definition.Width + "x" + definition.Height);
                view.Body.GetComponent<Renderer>().GetPropertyBlock(tint);
                var expected = scene.Art.Yarn[definition.Color].GetColor("_BaseColor");
                var actual = tint.GetColor("_BaseColor");
                Expect(Near(actual, expected) || Near(actual, expected.linear), "Block " + view.Index + " tint");
            }
            for (var i = 0; i < views.Length; i++)
                for (var j = i + 1; j < views.Length; j++)
                {
                    if (scene.State.Blocks[views[i].Index].Phase != 0 || scene.State.Blocks[views[j].Index].Phase != 0) continue;
                    var a = views[i].Body.GetComponent<Renderer>().bounds;
                    var gap = WoolBlockMesh.Inset * 2 * WoolRouting.CellSize - 0.002f;
                    a.Expand(new Vector3(gap * 2, 0, gap * 2));
                    Expect(!a.Intersects(views[j].Body.GetComponent<Renderer>().bounds), "Blocks " + views[i].Index + " and " + views[j].Index + " keep a gap of at least " + gap.ToString("F3"));
                }
            var blocked = false;
            foreach (var view in views)
            {
                if (scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                Expect(view.Shake > 0 && scene.State.Blocks[view.Index].Phase == 0, "Blocked block " + view.Index + " only shakes");
                blocked = true;
                break;
            }
            Expect(blocked, "A blocked block was clicked through EventSystem");
            var moved = false;
            foreach (var view in views)
            {
                if (!scene.State.CanSelect(view.Index)) continue;
                Click(view, out var hit);
                if (!hit) continue;
                Expect(scene.State.Blocks[view.Index].Phase == 1, "Clear block " + view.Index + " starts its flight");
                moved = true;
                break;
            }
            Expect(moved, "A clear block was clicked through EventSystem");
            return "PLAY BLOCKS PASS / " + views.Length + " blocks";
        }

        static void Click(WoolBlockView view, out bool hit)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(view.transform.position + Vector3.up * 0.3f), button = PointerEventData.InputButton.Left };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            hit = results.Count > 0 && results[0].gameObject == view.gameObject;
            if (hit) ExecuteEvents.Execute(view.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
