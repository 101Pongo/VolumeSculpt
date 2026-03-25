using UnityEngine;

namespace Atlas.VolumeSculpt.Runtime {

    /// <summary>Power-of-two resolution for 3D textures (8–512).</summary>
    public enum EResolution3D {
        _8 = 8, _16 = 16, _32 = 32, _64 = 64,
        _128 = 128, _256 = 256, _512 = 512
    }

    /// <summary>Power-of-two resolution for 2D textures (16–4096).</summary>
    public enum EResolution2D {
        _16 = 16, _32 = 32, _64 = 64, _128 = 128,
        _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096
    }

    public static class ResolutionExtensions {
        public static Vector3Int ToVector3Int(this EResolution3D res) => new((int)res, (int)res, (int)res);
        public static Vector3Int ToVector3Int(this EResolution3D x, EResolution3D y, EResolution3D z) => new((int)x, (int)y, (int)z);
        public static Vector2Int ToVector2Int(this EResolution2D res) => new((int)res, (int)res);
        public static Vector2Int ToVector2Int(this EResolution2D w, EResolution2D h) => new((int)w, (int)h);
    }

}
