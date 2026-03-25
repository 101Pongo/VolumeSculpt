// ============================================================================
// VolumetricsCommon.hlsl
// Shared volumetric compute utilities
// ============================================================================

#ifndef ATLAS_VOLUME_SCULPT_COMMON_HLSL
#define ATLAS_VOLUME_SCULPT_COMMON_HLSL

#define PI 3.14159265359
#define TAU 6.28318530718
#define MAX_DISTANCE 10000.0

// ============================================================================
// VOLUME TRANSFORMS
// Volumes support arbitrary rotation via full matrix transform.
// Volume-local space: [-0.5, 0.5] cube maps to the full volume.
// ============================================================================

float4x4 _VolumeToWorld;  // volume local [-0.5, 0.5] → world
float4x4 _WorldToVolume;  // world → volume local [-0.5, 0.5]
int3 _Resolution;

/// Voxel index → volume-local UVW [0, 1].
float3 VoxelToUvw(uint3 voxel) {
    return (float3(voxel) + 0.5) / float3(_Resolution);
}

/// Voxel index → world position.
float3 VoxelToWorld(uint3 voxel) {
    float3 local = VoxelToUvw(voxel) - 0.5;
    return mul(_VolumeToWorld, float4(local, 1.0)).xyz;
}

/// World position → volume-local UVW [0, 1].
float3 WorldToUvw(float3 worldPos) {
    float3 local = mul(_WorldToVolume, float4(worldPos, 1.0)).xyz;
    return local + 0.5;
}

// ============================================================================
// INDEX CONVERSION
// ============================================================================

uint ToLinearIndex(uint3 id) {
    return id.x + id.y * _Resolution.x + id.z * _Resolution.x * _Resolution.y;
}

uint3 FromLinearIndex(uint index) {
    uint z = index / (_Resolution.x * _Resolution.y);
    uint rem = index % (_Resolution.x * _Resolution.y);
    uint y = rem / _Resolution.x;
    uint x = rem % _Resolution.x;
    return uint3(x, y, z);
}

// ============================================================================
// QUATERNION OPERATIONS
// ============================================================================

float3 RotateByQuat(float3 v, float4 q) {
    float3 u = q.xyz;
    float s = q.w;
    return 2.0 * dot(u, v) * u + (s * s - dot(u, u)) * v + 2.0 * s * cross(u, v);
}

float3 InverseRotateByQuat(float3 v, float4 q) {
    return RotateByQuat(v, float4(-q.xyz, q.w));
}

// ============================================================================
// SDF PRIMITIVES
// ============================================================================

float sdBox(float3 p, float3 halfExtents) {
    float3 q = abs(p) - halfExtents;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdRoundedBox(float3 p, float3 halfExtents, float cornerRadius) {
    float3 q = abs(p) - (halfExtents - cornerRadius);
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - cornerRadius;
}

// ============================================================================
// INFLUENCE / FALLOFF
// ============================================================================

float GetInfluence(float signedDistance, float falloff) {
    if (signedDistance <= 0.0) return 1.0;
    if (falloff <= 0.0 || signedDistance >= falloff) return 0.0;
    float t = 1.0 - (signedDistance / falloff);
    return t * t * (3.0 - 2.0 * t);
}

// ============================================================================
// SDF OPERATIONS
// ============================================================================

#define SDF_OP_UNION 0
#define SDF_OP_SMOOTH_UNION 1
#define SDF_OP_SUBTRACTION 2
#define SDF_OP_SMOOTH_SUBTRACTION 3

float opSmoothUnion(float d1, float d2, float k) {
    if (k <= 0.0) return min(d1, d2);
    float h = saturate(0.5 + 0.5 * (d2 - d1) / k);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
}

float opSmoothSubtraction(float d1, float d2, float k) {
    if (k <= 0.0) return max(d1, -d2);
    float h = saturate(0.5 - 0.5 * (d2 + d1) / k);
    return lerp(d1, -d2, h) + k * h * (1.0 - h);
}

float CombineSdf(float sceneDist, float sdfDist, uint opType, float smoothing) {
    if (opType == SDF_OP_UNION) return min(sceneDist, sdfDist);
    if (opType == SDF_OP_SMOOTH_UNION) return opSmoothUnion(sceneDist, sdfDist, smoothing);
    if (opType == SDF_OP_SUBTRACTION) return max(sceneDist, -sdfDist);
    if (opType == SDF_OP_SMOOTH_SUBTRACTION) return opSmoothSubtraction(sceneDist, sdfDist, smoothing);
    return min(sceneDist, sdfDist);
}

#endif
