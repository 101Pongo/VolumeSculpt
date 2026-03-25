using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime.Internal {

    internal static class PaintDispatchInternal {
        private static ComputeShader _shader;
        private static ComputeShader Shader => _shader ??= ComputeHelper.LoadShader("Paint3DBake");

        internal static RenderTexture CreatePaintTexture(Vector3Int resolution) {
            return ComputeHelper.CreateRT3D(resolution, RenderTextureFormat.ARGB32);
        }

        internal static void Bake(
            RenderTexture target, VolumeDesc volume,
            PaintZoneGpu[] zones, Color defaultValue = default) {

            var cs = Shader;
            if (cs == null) return;

            int kernel = cs.FindKernel("CSBakePaint");
            int count = zones != null ? zones.Length : 0;

            using var buffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured, Mathf.Max(1, count), PaintZoneGpu.STRIDE);
            if (count > 0) buffer.SetData(zones);

            ComputeHelper.SetVolumeUniforms(cs, volume);
            cs.SetBuffer(kernel, "_ZoneBuffer", buffer);
            cs.SetInt("_ZoneCount", count);
            cs.SetVector("_DefaultValue", (Vector4)defaultValue);
            cs.SetTexture(kernel, "_OutputPaint", target);

            ComputeHelper.Dispatch3D(cs, kernel, volume);
        }
    }

}
