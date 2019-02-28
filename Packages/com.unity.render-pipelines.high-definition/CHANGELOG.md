# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [4.6.0-preview] - 2018-12-07

### Added

### Fixed
- Fixed forward clustered lighting for VR (double-wide).
- Fixed HDRenderPipelineAsset inspector broken when displaying its FrameSettings from project windows.
- Fixed Decals and SSR diable flags for all shader graph master node (Lit, Fabric, StackLit, PBR)
- Fixed Distortion blend mode for shader graph master node (Lit, StackLit)
- Fixed bent Normal for Fabric master node in shader graph
- Fixed PBR master node lightlayers
- Fixed stacklit transmission and sun highlight
- Fixed logic to disable FPTL with stereo rendering
- Fixed an issue with flipped depth buffer during postprocessing
- Fixed decals with stereo rendering
- Fixed flip logic for postprocessing + VR
- Fixed copyStencilBuffer pass for Switch
- Fixed point light shadow map culling that wasn't taking into account far plane
- Fixed usage of SSR with transparent on all master node
- Fixed SSR and microshadowing on fabric material
- Fixed shader stripping for built-in lit shaders.

### Changed
- Removing the simple lightloop used by the simple lit shader
- Added a StackLit master node replacing the InspectorUI version. IMPORTANT: All previously authored StackLit Materials will be lost. You need to recreate them with the master node.

## [4.3.0-preview] - 2018-11-23

### Added
- Added option to run Contact Shadows and Volumetrics Voxelization stage in Async Compute
- Added camera freeze debug mode - Allow to visually see culling result for a camera
- Added support of LuxAtDistance for punctual lights
- Added option to remove more shader variants and display information when building a player

### Fixed
- Fixed issue in pyramid shaped spotlight handles manipulation
- Fixed Debug.DrawLine and Debug.Ray call to work in game view
- Fixed DebugMenu's enum resetted on change
- Fixed divide by 0 in refraction causing NaN
- Fixed disable rough refraction support
- Fixed refraction, SSS and atmospheric scattering for VR
- Fixed Light's UX to not allow negative intensity

### Changed
- Rename "Regular" in Diffusion profile UI "Thick Object"
- Changed VBuffer depth parametrization for volumetric from distanceRange to depthExtent - Require update of volumetric settings - Fog start at near plan
- SpotLight with box shape use Lux unit only

## [4.2.0-preview] - 2018-11-16

### Added
- Added a y-axis offset for the PlanarReflectionProbe and offset tool.
- Exposed the option to run SSR and SSAO on async compute.
- Added support for the _GlossMapScale parameter in the Legacy to HDRP Material converter.
- Added wave intrinsic instructions for use in Shaders (for AMD GCN).


### Fixed
- Fixed cubemap assignation on custom ReflectionProbe
- Fixed Reflection Probes’ capture settings' shadow distance.
- Fixed an issue with the SRP batcher and Shader variables declaration.
- Fixed thickness and subsurface slots for fabric Shader master node that wasn't appearing with the right combination of flags.
- Fixed d3d debug layer warning.
- Fixed PCSS sampling quality.
- Fixed the Subsurface and transmission Material feature enabling for fabric Shader.
- Fixed the Shader Graph UV node’s dimensions when using it in a vertex Shader.
- Fixed the planar reflection mirror gizmo's rotation.
- Fixed HDRenderPipelineAsset's FrameSettings not showing the selected enum in the Inspector drop-down.
- Fixed an error with async compute.
- MSAA now supports transparency.
- The HDRP Material upgrader tool now converts metallic values correctly.
- Volumetrics now render in Reflection Probes.
- Fixed a crash that occurred whenever you set a viewport size to 0.
- Fixed the Camera physic parameter that the UI previously did not display.


### Changed
- Updated default FrameSettings used for realtime Reflection Probes when you create a new HDRenderPipelineAsset.
- Remove multi-camera support. LWRP and HDRP will not support multi-camera layered rendering.
- Updated Shader Graph subshaders to use the new instancing define.
- Changed fog distance calculation from distance to plane to distance to sphere.
- Optimized forward rendering using AMD GCN by scalarizing the light loop.
- Changed the UI of the Light Editor.
- Change ordering of includes in HDRP Materials in order to reduce iteration time for faster compilation.

## [4.1.0-preview] - 2018-10-18

### Added
- Added occlusion mesh to depth prepass for VR (VR still disabled for now)
- Added a debug mode to display only one shadow at once
- Added controls for the highlight created by directional lights
- Added a light radius setting to punctual lights to soften light attenuation and simulate fill lighting
- Added a 'minRoughness' parameter to all non-area lights (was previously only available for certain light types)
- Added separate volumetric light/shadow dimmers
- Added per-pixel jitter to volumetrics to reduce aliasing artifacts
- Added a SurfaceShading.hlsl file, which implements material-agnostic shading functionality in an efficient manner
- Added support for shadow bias for thin object transmission
- Added FrameSettings to control realtime planar reflection
- Added control for SRPBatcher on HDRP Asset
- Added an option to clear the shadow atlases in the debug menu
- Added a color visualization of the shadow atlas rescale in debug mode
- Added support for disabling SSR on materials
- Added intrinsic for XBone
- Added new light volume debugging tool
- Added a new SSR debug view mode
- Added translaction's scale invariance on DensityVolume
- Added multiple supported LitShadermode and per renderer choice in case of both Forward and Deferred supported
- Added custom specular occlusion mode to Lit Shader Graph Master node
- Added separate editor resources file for those resources to not be taken in player builds.
- Added support for disabling SSR on materials in shader graph
- Added support of MSAA when Both ListShaderMode is enabled (previously only Forward mode was supported)
- Added support of emissive color override in debug mode
- Exposed max light for lightloop settings in hdrp asset UI
- Disable NormalDBuffer pass update if no there is no decal
- Added distant (fallback) volumetric fog + improved fog evaluation precision
- Add an option to reflect sky in SSR

### Fixed
- Fixed a normal bias issue with Stacklit (Was causing light leaking)
- Fixed camera preview outputing an error when both scene and game view where display and play and exit was call
- Fixed override debug mode not apply correctly on static GI
- Fixed issue where XRGraphicsConfig values set in the asset inspector GUI weren't propagating correctly (VR still disabled for now)
- Fixed issue with tangent that was using SurfaceGradient instead of regular normal decoding
- Fixed wrong error message display when switching to unsupported target like IOS
- Fixed an issue with ambient occlusion texture sometimes not being created properly causing broken rendering
- Shadow near plane is no longer limited at 0.1
- Fixed decal draw order on transparent material
- Fixed an issue where sometime the lookup texture used for GGX convolution was broken, causing broken rendering
- Fixed an issue where you wouldn't see any fog for certain pipeline/scene configurations
- Fixed an issue with volumetric lighting where the anisotropy value of 0 would not result in perfectly isotropic lighting
- Fixed shadow bias when the atlas is rescaled
- Fixed shadow cascade sampling outside of the atlas when cascade count is inferior to 4
- Fixed shadow filter width in deferred rendering not matching shader config
- Fixed stereo sampling of depth texture in MSAA DepthValues.shader
- Fixed box light UI which allowed negative and zero sizes, thus causing NaNs
- Fixed stereo rendering in HDRISky.shader (VR)
- Fixed normal blend and blend sphere influence for reflection probe
- Fixed distortion filtering (was point filtering, now trilinear)
- Fixed contact shadow for large distance
- Fixed depth pyramid debug view mode
- Fixed sphere shaped influence handles clamping in reflection probes
- Fixed reflection probes data migration for project created before using hdrp
- Fixed sphere shaped influence handles clamping in reflection probes
- Fixed reflection probes data migration for project created before using hdrp
- Fixed UI of layered material where scrollbar was rendered above copy button.
- Fixed material tesselation's parameters "start fade distance" and "end fade distance" that were clamped while being modified.
- Fixed various distortion and refraction issues - handle a better fallback
- Fixed SSR for multiple views
- Fixed SSR issues related to self-intersections
- Fixed shape density volume handle speed
- Fixed density volume shape handle moving too fast
- Fixed camera velocity pass that was remove by mistake
- Fixed some null pointer exceptions when disabling motion vectors support
- Fixed viewports for both SSS combine pass and transparent depth prepass
- Fixed blend mode pop up in UI not appearing when pre refraction is on
- Fixed some null pointer exceptions when disabling motion vectors support
- Fixed layered lit UI issue with scrollbar
- Fixed ambient occlusion for Lit Master Node when slot is connected

### Changed
- Use samplerunity_ShadowMask instead of samplerunity_samplerLightmap for shadow mask
- Allow to resize reflection probe gizmo's size
- Improve quality of screen space shadow
- Remove support of projection model for ScreenSpaceLighting (SSR always use HiZ and refraction always Proxy)
- Remove all the debug mode from SSR that are obsolete now
- Expose frameSettings and Capture settings for reflection and planar probe
- Update UI for reflection probe, planar probe, camera and HDRP Asset
- Implement proper linear blending for volumetric lighting via deep compositing as described in the paper "Deep Compositing Using Lie Algebras"
- Changed  planar mapping to match terrain convention (XZ instead of ZX)
- XRGraphicsConfig is no longer Read/Write. Instead, it's read-only. This improves consistency of XR behavior between the legacy render pipeline and SRP
- Change reflection probe data migration code (to update old reflection probe to new one)
- Updated gizmo for ReflectionProbes
- Updated UI and Gizmo of DensityVolume
- Renamed "Line" shaped lights to "Tube" light
- Use the "mean height" fog parametrization
- Shadow quality settings are setup to "All" when using HDRP (Not visile in UI when using SRP). Avoid to have disabled shadow.
- Internally use premultiplied alpha for all fog

## [4.0.0-preview] - 2018-09-28

### Added
- Added a new TerrainLit shader that supports rendering of Unity terrains.
- Added controls for linear fade at the boundary of density volumes
- Added new API to control decals without monobehaviour object
- Improve Decal Gizmo
- Implement Screen Space Reflections (SSR) (alpha version, highly experimental)
- Add an option to invert the fade parameter on a Density Volume
- Added a Fabric shader (experimental) handling cotton and silk
- Added support for MSAA in forward only for opaque only
- Implement smoothness fade for SSR
- Added support for AxF shader (X-rite format - require special AxF importer from Unity not part of HDRP)
- Added control for sundisc on directional light (hack)
- Added a new HD Lit Master node that implements Lit shader support for Shader Graph
- Added Micro shadowing support (hack)
- Added an event on HDAdditionalCameraData for custom rendering
- HDRP Shader Graph shaders now support 4-channel UVs.

### Fixed
- Fixed an issue where sometimes the deferred shadow texture would not be valid, causing wrong rendering.
- Stencil test during decals normal buffer update is now properly applied
- Decals corectly update normal buffer in forward
- Fixed a normalization problem in reflection probe face fading causing artefacts in some cases
- Fix multi-selection behavior of Density Volumes overwriting the albedo value
- Fixed support of depth texture for RenderTexture. HDRP now correctly output depth to user depth buffer if RenderTexture request it.
- Fixed multi-selection behavior of Density Volumes overwriting the albedo value
- Fixed support of depth for RenderTexture. HDRP now correctly output depth to user depth buffer if RenderTexture request it.
- Fixed support of Gizmo in game view in the editor
- Fixed gizmo for spot light type
- Fixed issue with TileViewDebug mode being inversed in gameview
- Fixed an issue with SAMPLE_TEXTURECUBE_SHADOW macro
- Fixed issue with color picker not display correctly when game and scene view are visible at the same time
- Fixed an issue with reflection probe face fading
- Fixed camera motion vectors shader and associated matrices to update correctly for single-pass double-wide stereo rendering
- Fixed light attenuation functions when range attenuation is disabled
- Fixed shadow component algorithm fixup not dirtying the scene, so changes can be saved to disk.
- Fixed some GC leaks for HDRP
- Fixed contact shadow not affected by shadow dimmer
- Fixed GGX that works correctly for the roughness value of 0 (mean specular highlgiht will disappeard for perfect mirror, we rely on maxSmoothness instead to always have a highlight even on mirror surface)
- Add stereo support to ShaderPassForward.hlsl. Forward rendering now seems passable in limited test scenes with camera-relative rendering disabled.
- Add stereo support to ProceduralSky.shader and OpaqueAtmosphericScattering.shader.
- Added CullingGroupManager to fix more GC.Alloc's in HDRP
- Fixed rendering when multiple cameras render into the same render texture

### Changed
- Changed the way depth & color pyramids are built to be faster and better quality, thus improving the look of distortion and refraction.
- Stabilize the dithered LOD transition mask with respect to the camera rotation.
- Avoid multiple depth buffer copies when decals are present
- Refactor code related to the RT handle system (No more normal buffer manager)
- Remove deferred directional shadow and move evaluation before lightloop
- Add a function GetNormalForShadowBias() that material need to implement to return the normal used for normal shadow biasing
- Remove Jimenez Subsurface scattering code (This code was disabled by default, now remove to ease maintenance)
- Change Decal API, decal contribution is now done in Material. Require update of material using decal
- Move a lot of files from CoreRP to HDRP/CoreRP. All moved files weren't used by Ligthweight pipeline. Long term they could move back to CoreRP after CoreRP become out of preview
- Updated camera inspector UI
- Updated decal gizmo
- Optimization: The objects that are rendered in the Motion Vector Pass are not rendered in the prepass anymore
- Removed setting shader inclue path via old API, use package shader include paths
- The default value of 'maxSmoothness' for punctual lights has been changed to 0.99
- Modified deferred compute and vert/frag shaders for first steps towards stereo support
- Moved material specific Shader Graph files into corresponding material folders.
- Hide environment lighting settings when enabling HDRP (Settings are control from sceneSettings)
- Update all shader includes to use absolute path (allow users to create material in their Asset folder)
- Done a reorganization of the files (Move ShaderPass to RenderPipeline folder, Move all shadow related files to Lighting/Shadow and others)
- Improved performance and quality of Screen Space Shadows

## [3.3.0-preview]

### Added
- Added an error message to say to use Metal or Vulkan when trying to use OpenGL API
- Added a new Fabric shader model that supports Silk and Cotton/Wool
- Added a new HDRP Lighting Debug mode to visualize Light Volumes for Point, Spot, Line, Rectangular and Reflection Probes
- Add support for reflection probe light layers
- Improve quality of anisotropic on IBL

### Fixed
- Fix an issue where the screen where darken when rendering camera preview
- Fix display correct target platform when showing message to inform user that a platform is not supported
- Remove workaround for metal and vulkan in normal buffer encoding/decoding
- Fixed an issue with color picker not working in forward
- Fixed an issue where reseting HDLight do not reset all of its parameters
- Fixed shader compile warning in DebugLightVolumes.shader

### Changed
- Changed default reflection probe to be 256x256x6 and array size to be 64
- Removed dependence on the NdotL for thickness evaluation for translucency (based on artist's input)
- Increased the precision when comparing Planar or HD reflection probe volumes
- Remove various GC alloc in C#. Slightly better performance

## [3.2.0-preview]

### Added
- Added a luminance meter in the debug menu
- Added support of Light, reflection probe, emissive material, volume settings related to lighting to Lighting explorer
- Added support for 16bit shadows

### Fixed
- Fix issue with package upgrading (HDRP resources asset is now versionned to worarkound package manager limitation)
- Fix HDReflectionProbe offset displayed in gizmo different than what is affected.
- Fix decals getting into a state where they could not be removed or disabled.
- Fix lux meter mode - The lux meter isn't affected by the sky anymore
- Fix area light size reset when multi-selected
- Fix filter pass number in HDUtils.BlitQuad
- Fix Lux meter mode that was applying SSS
- Fix planar reflections that were not working with tile/cluster (olbique matrix)
- Fix debug menu at runtime not working after nested prefab PR come to trunk
- Fix scrolling issue in density volume

### Changed
- Shader code refactor: Split MaterialUtilities file in two parts BuiltinUtilities (independent of FragInputs) and MaterialUtilities (Dependent of FragInputs)
- Change screen space shadow rendertarget format from ARGB32 to RG16

## [3.1.0-preview]

### Added
- Decal now support per channel selection mask. There is now two mode. One with BaseColor, Normal and Smoothness and another one more expensive with BaseColor, Normal, Smoothness, Metal and AO. Control is on HDRP Asset. This may require to launch an update script for old scene: 'Edit/Render Pipeline/Single step upgrade script/Upgrade all DecalMaterial MaskBlendMode'.
- Decal now supports depth bias for decal mesh, to prevent z-fighting
- Decal material now supports draw order for decal projectors 
- Added LightLayers support (Base on mask from renderers name RenderingLayers and mask from light name LightLayers - if they match, the light apply) - cost an extra GBuffer in deferred (more bandwidth)
- When LightLayers is enabled, the AmbientOclusion is store in the GBuffer in deferred path allowing to avoid double occlusion with SSAO. In forward the double occlusion is now always avoided.
- Added the possibility to add an override transform on the camera for volume interpolation
- Added desired lux intensity and auto multiplier for HDRI sky
- Added an option to disable light by type in the debug menu
- Added gradient sky
- Split EmissiveColor and bakeDiffuseLighting in forward avoiding the emissiveColor to be affect by SSAO
- Added a volume to control indirect light intensity
- Added EV 100 intensity unit for area lights
- Added support for RendererPriority on Renderer. This allow to control order of transparent rendering manually. HDRP have now two stage of sorting for transparent in addition to bact to front. Material have a priority then Renderer have a priority.
- Add Coupling of (HD)Camera and HDAdditionalCameraData for reset and remove in inspector contextual menu of Camera
- Add Coupling of (HD)ReflectionProbe and HDAdditionalReflectionData for reset and remove in inspector contextual menu of ReflectoinProbe
- Add macro to forbid unity_ObjectToWorld/unity_WorldToObject to be use as it doesn't handle camera relative rendering
- Add opacity control on contact shadow

### Fixed
- Fixed an issue with PreIntegratedFGD texture being sometimes destroyed and not regenerated causing rendering to break
- PostProcess input buffers are not copied anymore on PC if the viewport size matches the final render target size
- Fixed an issue when manipulating a lot of decals, it was displaying a lot of errors in the inspector
- Fixed capture material with reflection probe
- Refactored Constant Buffers to avoid hitting the maximum number of bound CBs in some cases.
- Fixed the light range affecting the transform scale when changed.
- Snap to grid now works for Decal projector resizing.
- Added a warning for 128x128 cookie texture without mipmaps
- Replace the sampler used for density volumes for correct wrap mode handling

### Changed
- Move Render Pipeline Debug "Windows from Windows->General-> Render Pipeline debug windows" to "Windows from Windows->Analysis-> Render Pipeline debug windows"
- Update detail map formula for smoothness and albedo, goal it to bright and dark perceptually and scale factor is use to control gradient speed
- Refactor the Upgrade material system. Now a material can be update from older version at any time. Call Edit/Render Pipeline/Upgrade all Materials to newer version
- Change name EnableDBuffer to EnableDecals at several place (shader, hdrp asset...), this require a call to Edit/Render Pipeline/Upgrade all Materials to newer version to have up to date material.
- Refactor shader code: BakeLightingData structure have been replace by BuiltinData. Lot of shader code have been remove/change.
- Refactor shader code: All GBuffer are now handled by the deferred material. Mean ShadowMask and LightLayers are control by lit material in lit.hlsl and not outside anymore. Lot of shader code have been remove/change.
- Refactor shader code: Rename GetBakedDiffuseLighting to ModifyBakedDiffuseLighting. This function now handle lighting model for transmission too. Lux meter debug mode is factor outisde.
- Refactor shader code: GetBakedDiffuseLighting is not call anymore in GBuffer or forward pass, including the ConvertSurfaceDataToBSDFData and GetPreLightData, this is done in ModifyBakedDiffuseLighting now
- Refactor shader code: Added a backBakeDiffuseLighting to BuiltinData to handle lighting for transmission
- Refactor shader code: Material must now call InitBuiltinData (Init all to zero + init bakeDiffuseLighting and backBakeDiffuseLighting ) and PostInitBuiltinData

## [3.0.0-preview]

### Fixed
- Fixed an issue with distortion that was using previous frame instead of current frame
- Fixed an issue where disabled light where not upgrade correctly to the new physical light unit system introduce in 2.0.5-preview

### Changed
- Update assembly definitions to output assemblies that match Unity naming convention (Unity.*).

## [2.0.5-preview]

### Added
- Add option supportDitheringCrossFade on HDRP Asset to allow to remove shader variant during player build if needed
- Add contact shadows for punctual lights (in additional shadow settings), only one light is allowed to cast contact shadows at the same time and so at each frame a dominant light is choosed among all light with contact shadows enabled.
- Add PCSS shadow filter support (from SRP Core)
- Exposed shadow budget parameters in HDRP asset
- Add an option to generate an emissive mesh for area lights (currently rectangle light only). The mesh fits the size, intensity and color of the light.
- Add an option to the HDRP asset to increase the resolution of volumetric lighting.
- Add additional ligth unit support for punctual light (Lumens, Candela) and area lights (Lumens, Luminance)
- Add dedicated Gizmo for the box Influence volume of HDReflectionProbe / PlanarReflectionProbe

### Changed
- Re-enable shadow mask mode in debug view
- SSS and Transmission code have been refactored to be able to share it between various material. Guidelines are in SubsurfaceScattering.hlsl
- Change code in area light with LTC for Lit shader. Magnitude is now take from FGD texture instead of a separate texture
- Improve camera relative rendering: We now apply camera translation on the model matrix, so before the TransformObjectToWorld(). Note: unity_WorldToObject and unity_ObjectToWorld must never be used directly.
- Rename positionWS to positionRWS (Camera relative world position) at a lot of places (mainly in interpolator and FragInputs). In case of custom shader user will be required to update their code.
- Rename positionWS, capturePositionWS, proxyPositionWS, influencePositionWS to positionRWS, capturePositionRWS, proxyPositionRWS, influencePositionRWS (Camera relative world position) in LightDefinition struct.
- Improve the quality of trilinear filtering of density volume textures.
- Improve UI for HDReflectionProbe / PlanarReflectionProbe

### Fixed
- Fixed a shader preprocessor issue when compiling DebugViewMaterialGBuffer.shader against Metal target
- Added a temporary workaround to Lit.hlsl to avoid broken lighting code with Metal/AMD
- Fixed issue when using more than one volume texture mask with density volumes.
- Fixed an error which prevented volumetric lighting from working if no density volumes with 3D textures were present.
- Fix contact shadows applied on transmission
- Fix issue with forward opaque lit shader variant being removed by the shader preprocessor
- Fixed compilation errors on Nintendo Switch (limited XRSetting support).
- Fixed apply range attenuation option on punctual light
- Fixed issue with color temperature not take correctly into account with static lighting
- Don't display fog when diffuse lighting, specular lighting, or lux meter debug mode are enabled.

## [2.0.4-preview]

### Fixed
- Fix issue when disabling rough refraction and building a player. Was causing a crash.

## [2.0.3-preview]

### Added
- Increased debug color picker limit up to 260k lux

## [2.0.2-preview]

### Added
- Add Light -> Planar Reflection Probe command
- Added a false color mode in rendering debug
- Add support for mesh decals
- Add flag to disable projector decals on transparent geometry to save performance and decal texture atlas space
- Add ability to use decal diffuse map as mask only
- Add visualize all shadow masks in lighting debug
- Add export of normal and roughness buffer for forwardOnly and when in supportOnlyForward mode for forward
- Provide a define in lit.hlsl (FORWARD_MATERIAL_READ_FROM_WRITTEN_NORMAL_BUFFER) when output buffer normal is used to read the normal and roughness instead of caclulating it (can save performance, but lower quality due to compression)
- Add color swatch to decal material

### Changed
- Change Render -> Planar Reflection creation to 3D Object -> Mirror
- Change "Enable Reflector" name on SpotLight to "Angle Affect Intensity"
- Change prototype of BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData) to BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)

### Fixed
- Fix issue with StackLit in deferred mode with deferredDirectionalShadow due to GBuffer not being cleared. Gbuffer is still not clear and issue was fix with the new Output of normal buffer.
- Fixed an issue where interpolation volumes were not updated correctly for reflection captures.
- Fixed an exception in Light Loop settings UI

## [2.0.1-preview]

### Added
- Add stripper of shader variant when building a player. Save shader compile time.
- Disable per-object culling that was executed in C++ in HD whereas it was not used (Optimization)
- Enable texture streaming debugging (was not working before 2018.2)
- Added Screen Space Reflection with Proxy Projection Model
- Support correctly scene selection for alpha tested object
- Add per light shadow mask mode control (i.e shadow mask distance and shadow mask). It use the option NonLightmappedOnly
- Add geometric filtering to Lit shader (allow to reduce specular aliasing)
- Add shortcut to create DensityVolume and PlanarReflection in hierarchy
- Add a DefaultHDMirrorMaterial material for PlanarReflection
- Added a script to be able to upgrade material to newer version of HDRP
- Removed useless duplication of ForwardError passes.
- Add option to not compile any DEBUG_DISPLAY shader in the player (Faster build) call Support Runtime Debug display

### Changed
- Changed SupportForwardOnly to SupportOnlyForward in render pipeline settings
- Changed versioning variable name in HDAdditionalXXXData from m_version to version
- Create unique name when creating a game object in the rendering menu (i.e Density Volume(2))
- Re-organize various files and folder location to clean the repository
- Change Debug windows name and location. Now located at:  Windows -> General -> Render Pipeline Debug

### Removed
- Removed GlobalLightLoopSettings.maxPlanarReflectionProbes and instead use value of GlobalLightLoopSettings.planarReflectionProbeCacheSize
- Remove EmissiveIntensity parameter and change EmissiveColor to be HDR (Matching Builtin Unity behavior) - Data need to be updated - Launch Edit -> Single Step Upgrade Script -> Upgrade all Materials emissionColor

### Fixed
- Fix issue with LOD transition and instancing
- Fix discrepency between object motion vector and camera motion vector
- Fix issue with spot and dir light gizmo axis not highlighted correctly
- Fix potential crash while register debug windows inputs at startup
- Fix warning when creating Planar reflection
- Fix specular lighting debug mode (was rendering black)
- Allow projector decal with null material to allow to configure decal when HDRP is not set
- Decal atlas texture offset/scale is updated after allocations (used to be before so it was using date from previous frame)

## [2018.1 experimental]

### Added
- Configure the VolumetricLightingSystem code path to be on by default
- Trigger a build exception when trying to build an unsupported platform
- Introduce the VolumetricLightingController component, which can (and should) be placed on the camera, and allows one to control the near and the far plane of the V-Buffer (volumetric "froxel" buffer) along with the depth distribution (from logarithmic to linear)
- Add 3D texture support for DensityVolumes
- Add a better mapping of roughness to mipmap for planar reflection
- The VolumetricLightingSystem now uses RTHandles, which allows to save memory by sharing buffers between different cameras (history buffers are not shared), and reduce reallocation frequency by reallocating buffers only if the rendering resolution increases (and suballocating within existing buffers if the rendering resolution decreases)
- Add a Volumetric Dimmer slider to lights to control the intensity of the scattered volumetric lighting
- Add UV tiling and offset support for decals.
- Add mipmapping support for volume 3D mask textures

### Changed
- Default number of planar reflection change from 4 to 2
- Rename _MainDepthTexture to _CameraDepthTexture
- The VolumetricLightingController has been moved to the Interpolation Volume framework and now functions similarly to the VolumetricFog settings
- Update of UI of cookie, CubeCookie, Reflection probe and planar reflection probe to combo box
- Allow enabling/disabling shadows for area lights when they are set to baked.
- Hide applyRangeAttenuation and FadeDistance for directional shadow as they are not used

### Removed
- Remove Resource folder of PreIntegratedFGD and add the resource to RenderPipeline Asset

### Fixed
- Fix ConvertPhysicalLightIntensityToLightIntensity() function used when creating light from script to match HDLightEditor behavior
- Fix numerical issues with the default value of mean free path of volumetric fog 
- Fix the bug preventing decals from coexisting with density volumes
- Fix issue with alpha tested geometry using planar/triplanar mapping not render correctly or flickering (due to being wrongly alpha tested in depth prepass)
- Fix meta pass with triplanar (was not handling correctly the normal)
- Fix preview when a planar reflection is present
- Fix Camera preview, it is now a Preview cameraType (was a SceneView)
- Fix handling unknown GPUShadowTypes in the shadow manager.
- Fix area light shapes sent as point lights to the baking backends when they are set to baked.
- Fix unnecessary division by PI for baked area lights.
- Fix line lights sent to the lightmappers. The backends don't support this light type.
- Fix issue with shadow mask framesettings not correctly taken into account when shadow mask is enabled for lighting.
- Fix directional light and shadow mask transition, they are now matching making smooth transition
- Fix banding issues caused by high intensity volumetric lighting
- Fix the debug window being emptied on SRP asset reload
- Fix issue with debug mode not correctly clearing the GBuffer in editor after a resize
- Fix issue with ResetMaterialKeyword not resetting correctly ToggleOff/Roggle Keyword
- Fix issue with motion vector not render correctly if there is no depth prepass in deferred

## [2018.1.0f2]

### Added
- Screen Space Refraction projection model (Proxy raycasting, HiZ raymarching)
- Screen Space Refraction settings as volume component
- Added buffered frame history per camera
- Port Global Density Volumes to the Interpolation Volume System.
- Optimize ImportanceSampleLambert() to not require the tangent frame.
- Generalize SampleVBuffer() to handle different sampling and reconstruction methods.
- Improve the quality of volumetric lighting reprojection.
- Optimize Morton Order code in the Subsurface Scattering pass.
- Planar Reflection Probe support roughness (gaussian convolution of captured probe)
- Use an atlas instead of a texture array for cluster transparent decals
- Add a debug view to visualize the decal atlas
- Only store decal textures to atlas if decal is visible, debounce out of memory decal atlas warning.
- Add manipulator gizmo on decal to improve authoring workflow
- Add a minimal StackLit material (work in progress, this version can be used as template to add new material)

### Changed
- EnableShadowMask in FrameSettings (But shadowMaskSupport still disable by default)
- Forced Planar Probe update modes to (Realtime, Every Update, Mirror Camera)
- Screen Space Refraction proxy model uses the proxy of the first environment light (Reflection probe/Planar probe) or the sky
- Moved RTHandle static methods to RTHandles
- Renamed RTHandle to RTHandleSystem.RTHandle
- Move code for PreIntegratedFDG (Lit.shader) into its dedicated folder to be share with other material
- Move code for LTCArea (Lit.shader) into its dedicated folder to be share with other material

### Removed
- Removed Planar Probe mirror plane position and normal fields in inspector, always display mirror plane and normal gizmos

### Fixed
- Fix fog flags in scene view is now taken into account
- Fix sky in preview windows that were disappearing after a load of a new level
- Fix numerical issues in IntersectRayAABB().
- Fix alpha blending of volumetric lighting with transparent objects.
- Fix the near plane of the V-Buffer causing out-of-bounds look-ups in the clustered data structure.
- Depth and color pyramid are properly computed and sampled when the camera renders inside a viewport of a RTHandle.
- Fix decal atlas debug view to work correctly when shadow atlas view is also enabled

## [2018.1.0b13]

...
