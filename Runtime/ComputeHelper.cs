using UnityEngine;
using UnityEngine.Rendering;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>
    /// Shared utilities for compute dispatch: RT creation, volume uniform setup, shader loading.
    /// </summary>
    public static class ComputeHelper {
        private const string _SHADER_RESOURCE_PATH = "VolumetricShaders/";

        /// <summary>
        /// Load a VolumeSculpt compute shader from Resources/VolumetricShaders/.
        /// </summary>
        internal static ComputeShader LoadShader(string name) {
            return LoadShaderAtPath(_SHADER_RESOURCE_PATH + name);
        }

        /// <summary>
        /// Load a compute shader from any Resources path.
        /// For use by dependent modules with their own shader folders.
        /// </summary>
        public static ComputeShader LoadShaderAtPath(string resourcePath) {
            var shader = Resources.Load<ComputeShader>(resourcePath);
            if (shader == null)
                Debug.LogError($"[VolumeSculpt] Compute shader not found: Resources/{resourcePath}");
            return shader;
        }

        /// <summary>
        /// Set standard volume uniforms on a compute shader.
        /// Uses full matrix transforms for rotated volume support.
        /// </summary>
        public static void SetVolumeUniforms(ComputeShader cs, VolumeDesc vol) {
            cs.SetInts("_Resolution", vol.Resolution.x, vol.Resolution.y, vol.Resolution.z);
            cs.SetMatrix("_VolumeToWorld", vol.VolumeToWorld);
            cs.SetMatrix("_WorldToVolume", vol.WorldToVolume);
        }

        /// <summary>
        /// Create a 3D RenderTexture configured for compute read/write.
        /// Caller owns the lifetime. Returns null during editor domain reload
        /// when the GPU is not ready for resource allocation.
        /// </summary>
        public static RenderTexture CreateRT3D(
            Vector3Int resolution,
            RenderTextureFormat format = RenderTextureFormat.RFloat,
            FilterMode filter = FilterMode.Trilinear) {

            // Clamp to minimum 1 to prevent D3D12 E_INVALIDARG on zero-size allocation.
            resolution.x = Mathf.Max(1, resolution.x);
            resolution.y = Mathf.Max(1, resolution.y);
            resolution.z = Mathf.Max(1, resolution.z);

            // Use descriptor so all 3D properties are set before any GPU allocation.
            // The RenderTexture(w,h,0,fmt) constructor can implicitly Create() on some
            // backends, producing a 2D allocation before dimension/volumeDepth are set.
            var desc = new RenderTextureDescriptor(resolution.x, resolution.y, format, 0) {
                dimension = TextureDimension.Tex3D,
                volumeDepth = resolution.z,
                enableRandomWrite = true,
                msaaSamples = 1
            };

            var rt = new RenderTexture(desc) {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            if (!rt.Create()) {
                rt.Release();
                return null;
            }
            return rt;
        }

        /// <summary>
        /// Dispatch a 3D compute kernel with thread groups from volume resolution.
        /// </summary>
        public static void Dispatch3D(ComputeShader cs, int kernel, VolumeDesc vol, int groupSize = 4) {
            var (gx, gy, gz) = vol.ThreadGroups(groupSize);
            cs.Dispatch(kernel, gx, gy, gz);
        }
    }

}
