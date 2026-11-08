using System;
using UnityEngine;

namespace JamJam.Editor
{
    public static partial class WoolArtChecks
    {
        public static string Chevron()
        {
            LoadArt(out var art);
            var filter = art.Segment.GetComponentInChildren<MeshFilter>();
            Expect(filter != null && filter.name.StartsWith("Yarn", StringComparison.Ordinal), "Segment renderer keeps the Yarn prefix");
            var mesh = filter.sharedMesh;
            Expect(mesh != null && mesh.name == "Chevron", "Segment uses the Chevron mesh");
            Expect(Mathf.Abs(mesh.bounds.size.x * 1.15f - 1f) < 0.05f, "Chevron world width ~1.0 but was " + mesh.bounds.size.x * 1.15f);
            Expect(mesh.tangents.Length == mesh.vertexCount, "Chevron tangents for the knit normal map");
            Expect(mesh.bounds.max.y < 0 && mesh.bounds.min.y < -0.25f, "Chevron sits on the road under the 0.31 lift");
            ExpectOutward(mesh, "Chevron");
            return "CHEVRON PASS / " + mesh.vertexCount + " vertices";
        }
    }
}
