# VolumeSculpt

GPU-accelerated volume sculpting toolkit for Unity. Composes signed distance fields from primitives and mesh triangles, extracts meshes via marching cubes, and paints 3D color volumes — all through compute shaders.

## Why I Built This

I was building a procedural world generator where terrain, clouds, and other features were all defined as SDF shapes composed together. I needed a pipeline that could take a set of primitives (boxes with rounded corners, positioned and rotated arbitrarily in world space), bake them into a 3D distance field on the GPU, and then extract a mesh from that field — all at interactive speeds in the editor.

The marching cubes extraction needed to support LOD so I could get a quick preview at low resolution during authoring, then bake at full resolution for the final mesh. And because the meshes were being generated from volume data, I needed vertex welding to produce clean topology with proper shared normals instead of the faceted triangle soup that marching cubes gives you out of the box.

The paint system came from needing to assign material properties (color, roughness, blend weights) to the same volumetric space. Paint zones use the same rounded-box SDF shapes as the geometry, with falloff-based blending and per-channel write masks. The output is a 3D RGBA texture that shaders sample alongside the mesh.

## What It Does

### SDF Baking

Define shapes as `SdfShapeGpu` structs (position, rotation quaternion, half-extents, corner radius) and bake them into a 3D RenderTexture. Each shape carries a CSG operation — union, subtraction, or their smooth variants with configurable blend radius. You can also bake from mesh triangles (unsigned distance with ray-cast winding for inside/outside), or composite primitives on top of a mesh pass.

The output is raw signed distance in meters, stored as RHalf. Negative values are inside the surface.

### Marching Cubes Extraction

Extracts a triangle mesh from the SDF volume on the GPU, then welds vertices on the CPU. The GPU kernel outputs triangle soup with interpolated positions and normals. The CPU pass uses a spatial hash grid to merge vertices within a distance threshold, accumulating and renormalizing shared normals.

LOD is handled by a stride parameter — stride 1 evaluates every voxel, stride 2 skips every other one, stride 4 gives you quarter resolution. You can extract a full LOD chain in one call. There's also an optional clip extents parameter for when the bake volume is deliberately larger than the desired mesh (overlap regions for seamless terrain tiling).

### 3D Paint Baking

Bakes paint zones into an RGBA 3D texture. Each zone is a rounded box with falloff, and carries a color value, per-channel write mask, and blend mode (override or additive). The compute shader evaluates all zones per voxel and composites them in order.

### Volume Description

All dispatch functions take a `VolumeDesc` struct that describes the bake space — center, size, rotation, and resolution. The volume supports arbitrary rotation via full matrix transforms (not limited to axis-aligned boxes). The HLSL side receives world-to-volume and volume-to-world matrices and handles all coordinate conversions.

## Structure

```
Runtime/
  VolumeDesc.cs                       — Bake volume descriptor (center, size, rotation, resolution, matrices).
  SdfDispatch.cs                      — Public API for SDF baking.
  MarchingCubesDispatch.cs            — Public API for mesh extraction.
  PaintDispatch.cs                    — Public API for 3D paint baking.
  SdfShapeGpu.cs                      — GPU structs for SDF primitives and mesh triangles.
  PaintData.cs                        — GPU struct for paint zones.
  ComputeHelper.cs                    — RT3D creation, volume uniform setup, shader loading.
  EResolution.cs                      — Power-of-two resolution enums.
  Internal/
    SdfDispatchInternal.cs            — SDF compute dispatch implementation.
    MarchingCubesDispatchInternal.cs  — Marching cubes GPU extraction + CPU vertex welding.
    PaintDispatchInternal.cs          — Paint compute dispatch implementation.

Shaders/Resources/VolumetricShaders/
  Includes/
    VolumetricsCommon.hlsl            — Volume transforms, SDF primitives, CSG ops, quaternion math.
  SdfBake.compute                     — SDF baking kernels (primitives, mesh triangles, composite).
  MarchingCubes.compute               — Marching cubes kernel with LOD stride support.
  Paint3DBake.compute                 — Paint zone baking kernel.
```

## Dependencies

Unity only — no third-party packages.
