using UnityEditor;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Blocks()
        {
            WoolBlockMesh.Build(2, 1, out var wide);
            WoolBlockMesh.Build(1, 2, out var tall);
            var inset = WoolBlockMesh.Inset * 2;
            var cell = WoolRouting.CellSize;
            ExpectBounds(wide, new Vector3((2 - inset) * cell, WoolBlockMesh.Thickness, (1 - inset) * cell), "Block 2x1");
            ExpectBounds(tall, new Vector3((1 - inset) * cell, WoolBlockMesh.Thickness, (2 - inset) * cell), "Block 1x2");
            Expect(Mathf.Abs(wide.bounds.min.y) < 0.001f, "Block pivot at base");
            Expect(wide.uv2.Length == wide.vertexCount && wide.tangents.Length == wide.vertexCount, "Block edge UV and tangents");
            ExpectOutward(wide, "Block 2x1");
            ExpectOutward(tall, "Block 1x2");
            UnityEngine.Object.DestroyImmediate(wide);
            UnityEngine.Object.DestroyImmediate(tall);
            WoolBlockMesh.Get(2, 1, out var first);
            WoolBlockMesh.Get(2, 1, out var second);
            WoolBlockMesh.Get(1, 2, out var other);
            Expect(first == second && first != other, "Block mesh cache keyed by size");
            LoadArt(out var art);
            var view = art.Block.GetComponent<WoolBlockView>();
            var filter = view.Body.GetComponent<MeshFilter>();
            var body = view.Body.GetComponent<MeshRenderer>();
            Expect(filter != null && filter.sharedMesh != null && body != null, "ArrowBlock body is a mesh");
            Expect(body.sharedMaterial.shader.name == "JamJam/WoolKnit" && body.sharedMaterial.enableInstancing, "Block material is instanced WoolKnit");
            Expect(!ShaderUtil.ShaderHasError(body.sharedMaterial.shader), "WoolKnit compiles");
            var arrow = view.Arrow.GetComponent<MeshRenderer>();
            Expect(arrow != null && arrow.sharedMaterials.Length == 2, "Arrow plate has rim and face");
            ExpectColor(arrow.sharedMaterials[1], "#FFF8EC", "Arrow face");
            Expect(Vector3.Dot(view.Arrow.up, Vector3.forward) > 0.99f, "Arrow points along +Z by default");
            Expect(Mathf.Abs(view.Arrow.localScale.x - 0.56f * WoolRouting.CellSize) < 0.001f && Mathf.Abs(view.Arrow.localScale.y - 1.56f * WoolRouting.CellSize) < 0.001f, "Arrow plate scaled to the cell");
            Expect(art.Block.GetComponent<BoxCollider>() != null, "Block collider kept");
            return "BLOCKS PASS / " + first.vertexCount + " vertices per block";
        }
    }
}
