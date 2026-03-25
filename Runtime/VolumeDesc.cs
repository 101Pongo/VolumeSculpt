using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>
    /// Describes a bake volume for compute dispatch.
    /// Supports arbitrary rotation — not limited to AABB.
    /// </summary>
    public struct VolumeDesc {

        /// <summary>World-space center.</summary>
        public Vector3 Center;

        /// <summary>Full size (not half).</summary>
        public Vector3 Size;

        /// <summary>Rotation of the volume in world space.</summary>
        public Quaternion Rotation;

        /// <summary>Voxel resolution.</summary>
        public Vector3Int Resolution;

        /// <summary>World-to-volume-local matrix. Maps world pos → [-0.5, 0.5] cube.</summary>
        public Matrix4x4 WorldToVolume =>
            Matrix4x4.Scale(new Vector3(1f / Size.x, 1f / Size.y, 1f / Size.z)) *
            Matrix4x4.Rotate(Quaternion.Inverse(Rotation)) *
            Matrix4x4.Translate(-Center);

        /// <summary>Volume-local-to-world matrix. Maps [-0.5, 0.5] cube → world pos.</summary>
        public Matrix4x4 VolumeToWorld =>
            Matrix4x4.Translate(Center) *
            Matrix4x4.Rotate(Rotation) *
            Matrix4x4.Scale(Size);

        /// <summary>Total voxel count.</summary>
        public int VoxelCount => Resolution.x * Resolution.y * Resolution.z;

        /// <summary>Thread group counts for a given group size.</summary>
        public (int x, int y, int z) ThreadGroups(int groupSize = 4) => (
            Mathf.CeilToInt(Resolution.x / (float)groupSize),
            Mathf.CeilToInt(Resolution.y / (float)groupSize),
            Mathf.CeilToInt(Resolution.z / (float)groupSize));
    }

}
