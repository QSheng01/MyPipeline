# URP Cluster Usage

This migration provides geometry clusters with GPU frustum culling and indirect
procedural drawing. It is independent of the source project's custom render
pipeline events and can be used from a standard URP renderer.

## Bake

1. Create an empty GameObject and add `MPipeline.MeshCombiner`.
2. Make the meshes to combine children of that GameObject.
3. Set `Voxel Count`. Higher values improve spatial ordering but increase bake
   time. Start with `32`.
4. Use the component context menu `Bake Cluster Asset`.
5. The generated asset is placed under `Assets/Mipipline/ClusterData`.

The bake stores positions relative to the `MeshCombiner` transform, so the
object can be moved, rotated, or scaled at runtime. It also stores source
material references and per-triangle material indices.

## Runtime

1. Add `MPipeline.ClusterHiZRendererFeature` and then
   `MPipeline.ClusterRendererFeature` to the URP Renderer Data used by the
   camera. The Hi-Z feature builds and publishes the depth pyramid; the
   cluster feature only consumes it.
2. Leave `Culling Shader` and `Cluster Material` empty to use the bundled
   `Resources/ClusterCulling.compute` and `Resources/ClusterProcedural.mat`.
3. The bake command adds `MPipeline.ClusterRuntimeRenderer` to the same root
   object and assigns the generated asset.
4. Enable `Use GPU Frustum Culling` on the runtime component.
5. Enable `Use Hi-Z Occlusion Culling` to reject clusters hidden behind opaque
   geometry already present in the camera depth texture.

`ClusterRendererFeature` dispatches the compute culling pass for each active
runtime renderer and then calls `DrawProceduralIndirect`. The vertex and
cluster buffers are uploaded once per geometry asset and released when the
component is disabled or destroyed.

When Hi-Z occlusion is enabled, `ClusterHiZRendererFeature` resolves the
current URP camera depth attachment with URP's `CopyDepth` shader, builds an
HDRP-style packed depth atlas, and publishes a camera-scoped current-frame
resource. This path works with both single-sample and MSAA depth sources. The
atlas has no hardware mip chain: all mip levels are stored as rectangular
regions in one `R32_SFloat` texture, and their offsets and sizes are passed to
the culling compute shader. Cluster AABBs select a mip based on their projected
screen footprint and conservatively inspect every covered atlas texel.
`ClusterRendererFeature` falls back to frustum-only culling when the
independent Hi-Z feature is absent or has not published a pyramid for the
current camera and frame.

The depth pyramid contains opaque geometry rendered before the cluster pass.
Clusters drawn by this pass do not occlude one another until a later frame.
When Scene view rendering is configured to use the Game camera frustum, Hi-Z is
disabled for that Scene view because its depth buffer belongs to a different
camera.

## Current Scope

The migrated path currently uses one URP material for the complete baked asset.
The source material references and triangle-material array are preserved for a
later material-table integration, but the current URP shader does not yet
sample the source project's virtual material pool. Streaming I/O, LOD
selection, and clustered light lists are outside this first migration.
