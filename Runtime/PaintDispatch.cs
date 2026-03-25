using Atlas.VolumeSculpt.Runtime.Internal;
using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>
    /// GPU-accelerated 3D paint baking.
    /// Bakes paint zones into a 3D RGBA texture for volumetric coloring.
    /// </summary>
    /// <example><code>
    /// var rt = PaintDispatch.CreatePaintTexture(volume.Resolution);
    /// PaintDispatch.Bake(rt, volume, zones, defaultValue: Color.black);
    /// </code></example>
    public static class PaintDispatch {

        /// <summary>Create a properly configured RenderTexture for paint output.</summary>
        public static RenderTexture CreatePaintTexture(Vector3Int resolution)
            => PaintDispatchInternal.CreatePaintTexture(resolution);

        /// <summary>Bake paint zones into target texture.</summary>
        public static void Bake(
            RenderTexture target, VolumeDesc volume,
            PaintZoneGpu[] zones, Color defaultValue = default)
            => PaintDispatchInternal.Bake(target, volume, zones, defaultValue);
    }

}
