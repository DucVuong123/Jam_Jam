using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        static WoolLevel[] diagonalLevels;
        static WoolLevel diagonalFixture;

        public static string PlayDiagonalArm()
        {
            Expect(EditorApplication.isPlaying, "Enter Play Mode first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            Expect(scene != null && scene.State != null, "WoolScene is running");
            if (diagonalLevels == null) diagonalLevels = scene.Levels;
            if (diagonalFixture != null) Object.Destroy(diagonalFixture);
            diagonalFixture = ScriptableObject.CreateInstance<WoolLevel>();
            diagonalFixture.Title = "Diagonal Fixture";
            diagonalFixture.Path = diagonalLevels[0].Path;
            diagonalFixture.Data = new LevelDefinition
            {
                Columns = 8, Rows = 6, Slots = 4, PathLength = diagonalLevels[0].Data.PathLength, InitialHead = 2, Speed = 0.01f, Spacing = 0.3f,
                Blocks = new[]
                {
                    new BlockDefinition { X = 2, Y = 1.5f, Width = 2, Height = 1, Direction = ExitDirection.UpRight, Color = 0, Capacity = 1 },
                    new BlockDefinition { X = 5, Y = 4, Width = 2, Height = 1, Direction = ExitDirection.Up, Color = 0, Capacity = 1 },
                    new BlockDefinition { X = 5, Y = 1.5f, Width = 2, Height = 1, Direction = ExitDirection.UpLeft, Color = 0, Capacity = 1 }
                },
                Yarn = new[] { 0, 0, 0 }
            };
            scene.Levels = new[] { diagonalFixture };
            scene.LoadLevel(0);
            Physics.SyncTransforms();
            DiagonalViews(out var views);
            for (var i = 0; i < 3; i++)
            {
                var view = views[i];
                var direction = scene.State.Blocks[i].Definition.Direction;
                WoolShape.Step(direction, out var stepX, out var stepZ);
                Expect(Mathf.Abs(Mathf.DeltaAngle(view.transform.eulerAngles.y, WoolShape.RestYaw(direction))) < 0.5f, "Block " + i + " rests at yaw " + WoolShape.RestYaw(direction) + " but was " + view.transform.eulerAngles.y.ToString("F2"));
                Expect(Vector3.Dot(view.Arrow.up, new Vector3(stepX, 0, stepZ)) > 0.99f, "Block " + i + " arrow points " + direction);
                var bounds = view.GetComponent<BoxCollider>().bounds;
                var half = (2 - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize * 0.5f;
                var depth = (1 - WoolBlockMesh.Inset * 2) * WoolRouting.CellSize * 0.5f;
                if (WoolShape.IsDiagonal(direction))
                    Expect(Mathf.Abs(bounds.extents.x - (half + depth) * WoolShape.Diagonal) < 0.002f && Mathf.Abs(bounds.extents.z - (half + depth) * WoolShape.Diagonal) < 0.002f, "Block " + i + " collider turns with the block");
                else Expect(Mathf.Abs(bounds.extents.x - half) < 0.002f && Mathf.Abs(bounds.extents.z - depth) < 0.002f, "Cardinal collider stays axis aligned");
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay(camera.WorldToScreenPoint(view.transform.position + Vector3.up * ((WoolBlockMesh.Thickness + 0.2f) * 0.5f)));
                Expect(Physics.Raycast(ray, out var hit, 200f) && hit.collider.gameObject == view.gameObject, "Pointer ray at block " + i + " hits its collider");
            }
            Expect(!scene.State.CanSelect(0), "Diagonal block 0 is blocked by block 1");
            Expect(scene.State.CanSelect(1) && scene.State.CanSelect(2), "Blocks 1 and 2 are clear");
            scene.Select(0);
            Expect(scene.State.Blocks[0].Phase == 0 && scene.State.Blocks[0].Slot == -1 && scene.State.Spools[0].Block < 0, "Blocked diagonal does not move or reserve a slot");
            scene.Select(2);
            Expect(scene.State.Blocks[2].Phase == 1, "Clear diagonal block departs");
            Time.timeScale = 0.2f;
            return "DIAGONAL ARMED / 3 blocks";
        }

        public static string PlayDiagonalMid()
        {
            Expect(EditorApplication.isPlaying && diagonalFixture != null, "Run PlayDiagonalArm first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var block = scene.State.Blocks[2];
            var distance = block.Travel * WoolRouting.Speed;
            Expect(block.Phase == 1 && distance > 0 && distance <= block.ExitDistance, "Block 2 is on its diagonal first segment (" + distance.ToString("F3") + " of " + block.ExitDistance.ToString("F3") + ")");
            DiagonalViews(out var views);
            var view = views[2];
            WoolShape.Step(block.Definition.Direction, out var stepX, out var stepZ);
            var forward = new Vector3(stepX, 0, stepZ);
            var expected = WoolScene.BlockPosition(block.Definition, diagonalFixture.Data) + forward * distance;
            Expect(Vector3.Distance(view.transform.position, expected) < 0.002f, "Block 2 moves along its diagonal arrow");
            Expect(Mathf.Abs(Mathf.DeltaAngle(view.transform.eulerAngles.y, WoolShape.RestYaw(block.Definition.Direction))) < 0.5f && Vector3.Dot(view.Arrow.up, forward) > 0.99f, "Block 2 keeps its heading on the diagonal");
            Expect(scene.State.Blocks[0].Phase == 0 && !scene.State.CanSelect(0), "Block 0 is still blocked");
            return "DIAGONAL MID PASS / travel " + block.Travel.ToString("F2") + " s";
        }

        public static string PlayDiagonalEnd()
        {
            Expect(EditorApplication.isPlaying && diagonalFixture != null, "Run PlayDiagonalArm first");
            var scene = Object.FindFirstObjectByType<WoolScene>();
            var game = scene.State;
            Time.timeScale = 1;
            var block = game.Blocks[2];
            for (var i = 0; i < 1000 && block.Phase != 2; i++) game.Step(0.02f);
            var slot = WoolRouting.SlotCenter(block.Slot);
            var end = block.Route[block.Route.Count - 1];
            Expect(block.Phase == 2 && end.X == slot.X && end.Z == slot.Z, "Diagonal block reaches slot " + block.Slot);
            Expect(!game.CanSelect(0), "Block 0 stays blocked after block 2 leaves");
            scene.Select(1);
            Expect(game.Blocks[1].Phase == 1, "Blocker departs");
            for (var i = 0; i < 1000 && !game.CanSelect(0); i++) game.Step(0.02f);
            Expect(game.Blocks[1].Phase == 1 && game.CanSelect(0), "Diagonal lane opens while the blocker is still travelling");
            scene.Select(0);
            var route = game.Blocks[0].Route;
            WoolShape.Step(game.Blocks[0].Definition.Direction, out var stepX, out var stepZ);
            var dx = route[1].X - route[0].X;
            var dz = route[1].Z - route[0].Z;
            var length = Mathf.Sqrt(dx * dx + dz * dz);
            Expect(game.Blocks[0].Phase == 1 && Mathf.Abs(dx / length - stepX) < 1e-4f && Mathf.Abs(dz / length - stepZ) < 1e-4f, "Opened diagonal leaves along its arrow");
            scene.Levels = diagonalLevels;
            diagonalLevels = null;
            scene.LoadLevel(0);
            Object.Destroy(diagonalFixture);
            diagonalFixture = null;
            return "PLAY DIAGONAL PASS";
        }

        static void DiagonalViews(out WoolBlockView[] views)
        {
            views = new WoolBlockView[3];
            foreach (var view in Object.FindObjectsByType<WoolBlockView>(FindObjectsSortMode.None))
                if (view.Index >= 0 && view.Index < 3 && view.gameObject.activeInHierarchy) views[view.Index] = view;
            for (var i = 0; i < 3; i++) Expect(views[i] != null, "Block view " + i + " exists");
        }
    }
}
