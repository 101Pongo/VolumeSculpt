using System.Runtime.InteropServices;
using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>Paint blending mode for zone compositing.</summary>
    public enum EPaintBlendMode { Override = 0, Additive = 1 }

    /// <summary>
    /// GPU-layout struct for paint zone. Matches HLSL PaintZone exactly.
    /// All transforms are world-space — the volume matrix handles voxel mapping.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PaintZoneGpu {
        public Vector3 Position;     // 12
        public float CornerRadius;   // 4
        public Vector4 RotationQuat; // 16
        public Vector3 HalfExtents;  // 12
        public float Falloff;        // 4
        public Vector4 Value;        // 16
        public int WriteMask;        // 4
        public int BlendMode;        // 4
        private float _pad0, _pad1;  // 8

        public const int STRIDE = 80;
    }

}
