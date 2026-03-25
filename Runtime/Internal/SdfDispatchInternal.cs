using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime.Internal {

    internal static class SdfDispatchInternal {
        private static ComputeShader _shader;
        private static ComputeShader Shader => _shader ??= ComputeHelper.LoadShader("SdfBake");

        internal static RenderTexture CreateSdfTexture(Vector3Int resolution) {
            return ComputeHelper.CreateRT3D(resolution, RenderTextureFormat.RHalf);
        }

        internal static void Bake(
            RenderTexture target, VolumeDesc volume,
            SdfShapeGpu[] shapes, SdfTriangleGpu[] triangles = null) {

            var cs = Shader;
            if (cs == null) return;

            bool hasTris = triangles != null && triangles.Length > 0;
            bool hasShapes = shapes != null && shapes.Length > 0;

            if (hasTris) {
                DispatchMeshPass(cs, target, volume, triangles);
                if (hasShapes) DispatchCompositePass(cs, target, volume, shapes);
            } else {
                DispatchPrimitivePass(cs, target, volume, shapes);
            }
        }

        internal static void BakePrimitives(RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes) {
            var cs = Shader;
            if (cs == null) return;
            DispatchPrimitivePass(cs, target, volume, shapes);
        }

        internal static void CompositePrimitives(RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes) {
            var cs = Shader;
            if (cs == null) return;
            DispatchCompositePass(cs, target, volume, shapes);
        }

        internal static void BakeMeshTriangles(RenderTexture target, VolumeDesc volume, SdfTriangleGpu[] triangles) {
            var cs = Shader;
            if (cs == null) return;
            DispatchMeshPass(cs, target, volume, triangles);
        }

        private static void DispatchPrimitivePass(
            ComputeShader cs, RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes) {
            int kernel = cs.FindKernel("CSBakeSdf");
            int count = shapes != null ? shapes.Length : 0;
            using var buffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, Mathf.Max(1, count), SdfShapeGpu.STRIDE);
            if (count > 0) buffer.SetData(shapes);
            ComputeHelper.SetVolumeUniforms(cs, volume);
            cs.SetBuffer(kernel, "_ZoneBuffer", buffer);
            cs.SetInt("_ZoneCount", count);
            cs.SetTexture(kernel, "_OutputSdf", target);
            ComputeHelper.Dispatch3D(cs, kernel, volume);
        }

        private static void DispatchCompositePass(
            ComputeShader cs, RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes) {
            int kernel = cs.FindKernel("CSCompositePrimitives");
            int count = shapes != null ? shapes.Length : 0;
            using var buffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, Mathf.Max(1, count), SdfShapeGpu.STRIDE);
            if (count > 0) buffer.SetData(shapes);
            ComputeHelper.SetVolumeUniforms(cs, volume);
            cs.SetBuffer(kernel, "_ZoneBuffer", buffer);
            cs.SetInt("_ZoneCount", count);
            cs.SetTexture(kernel, "_OutputSdf", target);
            ComputeHelper.Dispatch3D(cs, kernel, volume);
        }

        private static void DispatchMeshPass(
            ComputeShader cs, RenderTexture target, VolumeDesc volume, SdfTriangleGpu[] triangles) {
            int kernel = cs.FindKernel("CSBakeMeshSdf");
            int count = triangles != null ? triangles.Length : 0;
            using var buffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, Mathf.Max(1, count), SdfTriangleGpu.STRIDE);
            if (count > 0) buffer.SetData(triangles);
            ComputeHelper.SetVolumeUniforms(cs, volume);
            cs.SetBuffer(kernel, "_TriangleBuffer", buffer);
            cs.SetInt("_TriangleCount", count);
            cs.SetTexture(kernel, "_OutputSdf", target);
            ComputeHelper.Dispatch3D(cs, kernel, volume);
        }
    }

}
