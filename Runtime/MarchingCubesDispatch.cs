using Atlas.VolumeSculpt.Runtime.Internal;
using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>
    /// GPU marching cubes mesh extraction from SDF volumes.
    /// Extracts triangle soup on GPU, welds vertices into a Unity Mesh on CPU.
    /// Supports LOD via stride parameter.
    ///
    /// Optional <c>clipExtents</c> (half-size) culls triangles whose centroid
    /// falls outside the box centered at the volume origin. Use this when the
    /// bake volume is larger than the desired mesh (e.g. terrain cell overlap).
    /// </summary>
    public static class MarchingCubesDispatch {

        /// <summary>Extract mesh from SDF texture at full resolution.</summary>
        public static Mesh Extract(
            RenderTexture sdf, VolumeDesc volume,
            float isoLevel = 0f, float weldThreshold = 0.001f)
            => MarchingCubesDispatchInternal.Extract(sdf, volume, isoLevel, weldThreshold);

        /// <summary>Extract mesh at a given LOD stride. Stride 2 = half res, 4 = quarter.</summary>
        public static Mesh ExtractAtStride(
            RenderTexture sdf, VolumeDesc volume, int stride,
            float isoLevel = 0f, float weldThreshold = 0.001f,
            Vector3 clipExtents = default)
            => MarchingCubesDispatchInternal.ExtractAtStride(sdf, volume, stride, isoLevel, weldThreshold, clipExtents);

        /// <summary>Extract multiple LOD meshes. Index 0 = highest detail.</summary>
        public static Mesh[] ExtractLods(
            RenderTexture sdf, VolumeDesc volume, int lodCount,
            float isoLevel = 0f, float weldThreshold = 0.001f,
            Vector3 clipExtents = default)
            => MarchingCubesDispatchInternal.ExtractLods(sdf, volume, lodCount, isoLevel, weldThreshold, clipExtents);
    }

}
