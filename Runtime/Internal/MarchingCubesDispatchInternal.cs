using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Atlas.VolumeSculpt.Runtime.Internal {

    internal static class MarchingCubesDispatchInternal {

        [StructLayout(LayoutKind.Sequential)]
        private struct GpuTriangle {
            public Vector3 VertA, NormA;
            public Vector3 VertB, NormB;
            public Vector3 VertC, NormC;
        }

        private static ComputeShader _shader;
        private static ComputeShader Shader => _shader ??= ComputeHelper.LoadShader("MarchingCubes");

        internal static Mesh Extract(
            RenderTexture sdf, VolumeDesc volume,
            float isoLevel = 0f, float weldThreshold = 0.001f) {
            return ExtractAtStride(sdf, volume, 1, isoLevel, weldThreshold);
        }

        internal static Mesh ExtractAtStride(
            RenderTexture sdf, VolumeDesc volume, int stride,
            float isoLevel = 0f, float weldThreshold = 0.001f,
            Vector3 clipExtents = default) {

            var cs = Shader;
            if (cs == null) return null;

            int kernel = cs.FindKernel("MarchingCubes");
            var res = volume.Resolution;

            var effectiveDims = new Vector3Int(
                (res.x - 1) / stride + 1,
                (res.y - 1) / stride + 1,
                (res.z - 1) / stride + 1);

            int maxTris = effectiveDims.x * effectiveDims.y * effectiveDims.z;

            using var triangleBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, maxTris, Marshal.SizeOf<GpuTriangle>());
            using var countBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, 1, sizeof(uint));
            countBuffer.SetData(new uint[] { 0 });

            ComputeHelper.SetVolumeUniforms(cs, volume);
            cs.SetTexture(kernel, "_SDF", sdf);
            cs.SetInt("_Stride", stride);
            cs.SetInt("_MaxTriangles", maxTris);
            cs.SetFloat("_IsoLevel", isoLevel);
            cs.SetBuffer(kernel, "_Triangles", triangleBuffer);
            cs.SetBuffer(kernel, "_TriangleCount", countBuffer);

            int gx = Mathf.CeilToInt(effectiveDims.x / 4f);
            int gy = Mathf.CeilToInt(effectiveDims.y / 4f);
            int gz = Mathf.CeilToInt(effectiveDims.z / 4f);
            cs.Dispatch(kernel, gx, gy, gz);

            var countData = new uint[1];
            countBuffer.GetData(countData);
            int triCount = (int)countData[0];

            if (triCount == 0) return null;

            var triangles = new GpuTriangle[triCount];
            triangleBuffer.GetData(triangles, 0, 0, triCount);

            var size = volume.Size;
            bool doClip = clipExtents.x > 0f && clipExtents.y > 0f && clipExtents.z > 0f;

            int vertCount = 0;
            // First pass: count triangles that survive clipping.
            if (!doClip) {
                vertCount = triCount * 3;
            } else {
                for (int t = 0; t < triCount; t++) {
                    var a = Vector3.Scale(triangles[t].VertA, size);
                    var b = Vector3.Scale(triangles[t].VertB, size);
                    var c = Vector3.Scale(triangles[t].VertC, size);
                    if (AnyVertInBounds(a, b, c, clipExtents))
                        vertCount += 3;
                }
            }

            if (vertCount == 0) return null;

            var positions = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            int vi = 0;

            for (int t = 0; t < triCount; t++) {
                var a = Vector3.Scale(triangles[t].VertA, size);
                var b = Vector3.Scale(triangles[t].VertB, size);
                var c = Vector3.Scale(triangles[t].VertC, size);

                if (doClip && !AnyVertInBounds(a, b, c, clipExtents))
                    continue;

                positions[vi]     = a;
                positions[vi + 1] = b;
                positions[vi + 2] = c;
                normals[vi]       = triangles[t].NormA;
                normals[vi + 1]   = triangles[t].NormB;
                normals[vi + 2]   = triangles[t].NormC;
                vi += 3;
            }

            float voxelSize = Mathf.Min(size.x / volume.Resolution.x,
                                        size.y / volume.Resolution.y,
                                        size.z / volume.Resolution.z);
            float scaledThreshold = Mathf.Max(weldThreshold, voxelSize * stride * 0.1f);
            return BuildWeldedMesh(positions, normals, scaledThreshold);
        }

        internal static Mesh[] ExtractLods(
            RenderTexture sdf, VolumeDesc volume, int lodCount,
            float isoLevel = 0f, float weldThreshold = 0.001f,
            Vector3 clipExtents = default) {

            var meshes = new List<Mesh>();
            for (int lod = 0; lod < lodCount; lod++) {
                int stride = 1 << lod;
                var mesh = ExtractAtStride(sdf, volume, stride, isoLevel, weldThreshold, clipExtents);
                if (mesh == null) {
                    if (lod == 0) meshes.Add(new Mesh());
                    break;
                }
                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }

        private static bool VertInBounds(Vector3 v, Vector3 ext) =>
            Mathf.Abs(v.x) <= ext.x && Mathf.Abs(v.y) <= ext.y && Mathf.Abs(v.z) <= ext.z;

        private static bool AnyVertInBounds(Vector3 a, Vector3 b, Vector3 c, Vector3 ext) =>
            VertInBounds(a, ext) || VertInBounds(b, ext) || VertInBounds(c, ext);

        private static Mesh BuildWeldedMesh(Vector3[] srcPos, Vector3[] srcNorm, float threshold) {
            int srcCount = srcPos.Length;
            float thresholdSq = threshold * threshold;
            float cellSize = threshold * 2f;
            float invCellSize = cellSize > 0f ? 1f / cellSize : 1f;

            // Use grid coordinates directly as key to avoid XOR hash collisions.
            // Each cell stores a list of vertex indices to handle multiple verts per cell.
            var cellMap = new Dictionary<(int, int, int), List<int>>();
            var weldedPos = new List<Vector3>();
            var weldedNorm = new List<Vector3>();
            var indices = new int[srcCount];

            for (int i = 0; i < srcCount; i++) {
                var pos = srcPos[i];
                var norm = srcNorm[i];

                if (float.IsNaN(pos.x) || float.IsInfinity(pos.x)) pos = Vector3.zero;
                if (float.IsNaN(norm.x) || float.IsInfinity(norm.x)) norm = Vector3.up;

                int ix = Mathf.FloorToInt(pos.x * invCellSize);
                int iy = Mathf.FloorToInt(pos.y * invCellSize);
                int iz = Mathf.FloorToInt(pos.z * invCellSize);
                var cellKey = (ix, iy, iz);

                int foundIdx = -1;

                // Check this cell and all 26 neighbors for a weldable vertex.
                for (int dx = -1; dx <= 1 && foundIdx < 0; dx++) {
                    for (int dy = -1; dy <= 1 && foundIdx < 0; dy++) {
                        for (int dz = -1; dz <= 1 && foundIdx < 0; dz++) {
                            var neighborKey = (ix + dx, iy + dy, iz + dz);
                            if (!cellMap.TryGetValue(neighborKey, out var bucket)) continue;
                            for (int b = 0; b < bucket.Count; b++) {
                                if ((weldedPos[bucket[b]] - pos).sqrMagnitude < thresholdSq) {
                                    foundIdx = bucket[b];
                                    break;
                                }
                            }
                        }
                    }
                }

                if (foundIdx >= 0) {
                    indices[i] = foundIdx;
                    weldedNorm[foundIdx] += norm;
                } else {
                    int newIdx = weldedPos.Count;
                    weldedPos.Add(pos);
                    weldedNorm.Add(norm);

                    if (!cellMap.TryGetValue(cellKey, out var bucket)) {
                        bucket = new List<int>(4);
                        cellMap[cellKey] = bucket;
                    }
                    bucket.Add(newIdx);
                    indices[i] = newIdx;
                }
            }

            for (int i = 0; i < weldedNorm.Count; i++)
                weldedNorm[i] = weldedNorm[i].normalized;

            var mesh = new Mesh {
                indexFormat = weldedPos.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(weldedPos);
            mesh.SetNormals(weldedNorm);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }

}
