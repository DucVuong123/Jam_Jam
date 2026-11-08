using System.Collections.Generic;
using UnityEngine;

namespace JamJam
{
    public static class WoolBlockMesh
    {
        public const float Thickness = 0.48f;
        public const float Inset = 0.09f;
        public const float Corner = 0.18f;
        public const float Bevel = 0.06f;
        public const float Border = 0.1f;
        const int CornerSteps = 5;
        const int BevelSteps = 3;
        const int Count = 4 * (CornerSteps + 1);
        static readonly Dictionary<(int, int), Mesh> cache = new Dictionary<(int, int), Mesh>();

        public static void Get(int width, int height, out Mesh mesh)
        {
            if (cache.TryGetValue((width, height), out mesh) && mesh != null) return;
            Build(width, height, out mesh);
            cache[(width, height)] = mesh;
        }

        public static void Build(int width, int height, out Mesh mesh)
        {
            var halfX = (width * 0.5f - Inset) * WoolRouting.CellSize;
            var halfZ = (height * 0.5f - Inset) * WoolRouting.CellSize;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var edge = new List<Vector2>();
            var triangles = new List<int>();
            var bottom = Ring(halfX, halfZ, 0, 0, 0, true, 1, vertices, normals, uv, edge);
            var shoulder = Ring(halfX, halfZ, 0, Thickness - Bevel, 0, true, 1, vertices, normals, uv, edge);
            Connect(bottom, shoulder, triangles);
            var lower = Ring(halfX, halfZ, 0, Thickness - Bevel, 0, false, 0, vertices, normals, uv, edge);
            for (var step = 1; step <= BevelSteps; step++)
            {
                var tilt = step / (float)BevelSteps * Mathf.PI * 0.5f;
                var upper = Ring(halfX, halfZ, Bevel * (1 - Mathf.Cos(tilt)), Thickness - Bevel + Bevel * Mathf.Sin(tilt), tilt, false, 0, vertices, normals, uv, edge);
                Connect(lower, upper, triangles);
                lower = upper;
            }
            var inner = Ring(halfX, halfZ, Bevel + Border, Thickness, Mathf.PI * 0.5f, false, 1, vertices, normals, uv, edge);
            Connect(lower, inner, triangles);
            var center = vertices.Count;
            vertices.Add(new Vector3(0, Thickness, 0));
            normals.Add(Vector3.up);
            uv.Add(Vector2.zero);
            edge.Add(new Vector2(1, 0));
            for (var k = 0; k < Count; k++) triangles.AddRange(new[] { center, inner + k + 1, inner + k });
            mesh = new Mesh { name = "KnitBlock_" + width + "x" + height };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetUVs(1, edge);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        static int Ring(float halfX, float halfZ, float inset, float y, float tilt, bool side, float edgeX, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<Vector2> edge)
        {
            var start = vertices.Count;
            var radius = Mathf.Max(0.01f, Corner - inset);
            var perimeter = 0f;
            var previous = Vector3.zero;
            for (var k = 0; k <= Count; k++)
            {
                var index = k % Count;
                var quadrant = index / (CornerSteps + 1);
                var angle = (quadrant + index % (CornerSteps + 1) / (float)CornerSteps) * Mathf.PI * 0.5f;
                var direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var corner = new Vector3((quadrant == 0 || quadrant == 3 ? 1 : -1) * (halfX - Corner), y, (quadrant < 2 ? 1 : -1) * (halfZ - Corner));
                var outline = corner + direction * Corner;
                if (k > 0) perimeter += Vector3.Distance(previous, outline);
                previous = outline;
                var point = corner + direction * radius;
                vertices.Add(point);
                normals.Add((direction * Mathf.Cos(tilt) + Vector3.up * Mathf.Sin(tilt)).normalized);
                uv.Add(side ? new Vector2(perimeter, y) : new Vector2(point.x, point.z));
                edge.Add(new Vector2(edgeX, perimeter));
            }
            return start;
        }

        static void Connect(int lower, int upper, List<int> triangles)
        {
            for (var k = 0; k < Count; k++)
                triangles.AddRange(new[] { lower + k, upper + k, lower + k + 1, lower + k + 1, upper + k, upper + k + 1 });
        }
    }
}
