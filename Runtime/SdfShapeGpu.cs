using System.Runtime.InteropServices;
using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>SDF operation types. Values match shader SDF_OP_* constants.</summary>
    public enum ESdfOp { Union = 0, SmoothUnion = 1, Subtraction = 2, SmoothSubtraction = 3 }

    /// <summary>
    /// GPU-layout struct for SDF primitive shape. Matches HLSL SdfShape exactly.
    /// All transforms are world-space — the volume matrix handles voxel mapping.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SdfShapeGpu {
        public Vector3 Position;           // 12
        public float CornerRadius;         // 4
        public Vector4 RotationQuat;       // 16
        public Vector3 HalfExtents;        // 12
        public float Smoothing;            // 4
        public int OpType;                 // 4
        private float _pad0, _pad1, _pad2; // 12

        public const int STRIDE = 64;
    }

    /// <summary>
    /// GPU-layout struct for mesh SDF triangle. Vertices in world space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SdfTriangleGpu {
        public Vector3 V0;                 // 12
        public Vector3 V1;                 // 12
        public Vector3 V2;                 // 12
        public float Smoothing;            // 4
        public int OpType;                 // 4
        private float _pad0, _pad1, _pad2; // 12

        public const int STRIDE = 56;
    }

}
