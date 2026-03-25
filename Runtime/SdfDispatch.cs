using Atlas.VolumeSculpt.Runtime.Internal;
using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>
    /// GPU-accelerated signed distance field generation.
    /// Bakes SDF volumes from primitive shapes and/or mesh triangles.
    /// Output is raw world-space distance in meters (RHalf format).
    /// </summary>
    /// <example><code>
    /// var rt = SdfDispatch.CreateSdfTexture(volume.Resolution);
    /// SdfDispatch.Bake(rt, volume, shapes);
    /// </code></example>
    public static class SdfDispatch {

        /// <summary>Create a properly configured RenderTexture for SDF output (RHalf).</summary>
        public static RenderTexture CreateSdfTexture(Vector3Int resolution)
            => SdfDispatchInternal.CreateSdfTexture(resolution);

        /// <summary>
        /// Full SDF bake. Mesh triangles first (if any), then primitives composited on top.
        /// </summary>
        public static void Bake(
            RenderTexture target, VolumeDesc volume,
            SdfShapeGpu[] shapes, SdfTriangleGpu[] triangles = null)
            => SdfDispatchInternal.Bake(target, volume, shapes, triangles);

        /// <summary>Bake primitives only. Initializes volume to MAX_DIST, then evaluates shapes.</summary>
        public static void BakePrimitives(RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes)
            => SdfDispatchInternal.BakePrimitives(target, volume, shapes);

        /// <summary>Composite primitives onto an existing SDF (e.g. after a mesh pass).</summary>
        public static void CompositePrimitives(RenderTexture target, VolumeDesc volume, SdfShapeGpu[] shapes)
            => SdfDispatchInternal.CompositePrimitives(target, volume, shapes);

        /// <summary>Bake mesh triangles only.</summary>
        public static void BakeMeshTriangles(RenderTexture target, VolumeDesc volume, SdfTriangleGpu[] triangles)
            => SdfDispatchInternal.BakeMeshTriangles(target, volume, triangles);
    }

}
