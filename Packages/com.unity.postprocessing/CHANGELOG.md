# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.2] - 2018-12-05

### Fixed
- Made the package manager happy.

## [2.1.1] - 2018-11-30

### Fixed
- Optimized volume texture blending.
- Switch compilation issues with 2019.1+.

### Changed
- Chromatic aberration is now forced to "fast mode" when running on GLES2.0 platforms due to compatibility issues.

## [2.1.0] - 2018-11-26

### Changed
- Minor version bump following the release of 2018.3 and verified compatibility with 2019.1.

## [2.0.20] - 2018-11-22

### Fixed
- Camera viewport wasn't working properly when outputting directly to the backbuffer.
- More improvements to VR support.
- Compatibility fixes for 2017.1 to 2017.4.
- Post-processing wouldn't work when loaded from an asset bundle.
- Compilation issue when Cinemachine is used with Post-processing.

### Changed
- Scriptable Render Pipelines should now call `PostProcessLayer.UpdateVolumeSystem(Camera, CommandBuffer)` at the beginning of the frame.

## [2.0.17-preview]

### Fixed
- First pass at improving VR support.
- Assert on Invalid LDR Lookup Texture size; added a check in the inspector for the user.
- Improved performance on Unity 2019.1+ by avoiding unnecessary blits if no other image effect is active.
- Use new ASTC enums on unity 2019.1+.

## [2.0.16-preview]

### Fixed
- Grain shader compilation errors on some mobile GPUs.
- Compilation issue with Unity 2019.1+ due to an internal API change.

## [2.0.15-preview]

### Fixed
- Warning on `[ShaderIncludePath]` in 2018.3+.

## [2.0.14-preview]

### Fixed
- Bloom flicker in single-pass double-wide stereo rendering.
- Right eye bloom offset in single-pass double-wide stereo rendering.
- If any parent of PostProcessingVolume has non-identity scale the Gizmo is rendered incorrectly.
- Cleanup error when going back'n'forth between Builtins & Scriptable pipelines.

### Changed
- Use `ExecuteAlways` in 2018.3+ for better compatibility with "Prefab Mode".

## [2.0.13-preview]

### Fixed
- Compilation issue with Unity 2019.1.
- Screen-space reflection memory leak.

## [2.0.12-preview]

### Fixed
- Ambient Occlusion could distort the screen on Android/Vulkan.
- Warning about SettingsProvider in 2018.3.
- Fixed issue with physical camera mode not working with post-processing.
- Fixed thread group warning message on Metal and Intel Iris.
- Fixed compatibility with versions pre-2018.2.

## [2.0.10-preview]

### Fixed
- Better handling of volumes in nested-prefabs.
- The Multi-scale volumetric obscurance effect wasn't properly releasing some of its temporary targets.
- N3DS deprecation warnings in 2018.3.

## [2.0.9-preview]

### Changed
- Update assembly definitions to output assemblies that match Unity naming convention (Unity.*).

## [2.0.8-preview]

### Fixed
- Post-processing is now working with VR SRP in PC.
- Crash on Vulkan when blending 3D textures.
- `RuntimeUtilities.DestroyVolume()` works as expected now.
- Excessive CPU usage on PS4 due to a badly initialized render texture.

### Changed
- Improved volume texture blending.

### Added
- `Depth` debug mode can now display linear depth instead of the raw platform depth.

## [2.0.7-preview]

### Fixed
- Post-processing wasn't working on Unity 2018.3.

### Added
- Bloom now comes with a `Clamp` parameter to limit the amount of bloom that comes with ultra-bright pixels.

## [2.0.6-preview]

### Fixed
- On large scenes, the first object you'd add to a profile could throw a `NullReferenceException`. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))
- Dithering now works correctly in dark areas when working in Gamma mode.
- Colored grain wasn't colored when `POSTFX_DEBUG_STATIC_GRAIN` was set.
- No more warning in the console when `POSTFX_DEBUG_STATIC_GRAIN` is set.

### Changed
- Minor scripting API improvements. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))
- More implicit casts for `VectorXParameter` and `ColorParameter` to `Vector2`, `Vector3` and `Vector4`.
- Script-instantiated profiles in volumes are now properly supported in the inspector. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))
- Improved volume UI & styling.

## [2.0.5-preview]

### Fixed
- More XR/Switch related fixes.

## [2.0.4-preview]

### Fixed
- Temporal Anti-aliasing creating NaN values in some cases. ([#337](https://github.com/Unity-Technologies/PostProcessing/issues/337))
- Auto-exposure has been fixed to work the same way it did before the full-compute port.
- XR compilation errors on Xbox One & Switch (2018.2).
- `ArgumentNullException` when attempting to get a property sheet for a null shader. ([#515](https://github.com/Unity-Technologies/PostProcessing/pull/515))
- Stop NaN Propagation not working for opaque-only effects.
- HDR color grading had a slight color temperature offset.
- PSVita compatibility.
- Tizen warning on 2018.2.
- Errors in the console when toggling lighting on/off in the scene view when working in Deferred.
- Debug monitors now work properly with HDRP.

### Added
- Contribution slider for the LDR Lut.
- Support for proper render target load/store actions on mobile (2018.2).

### Changed
- Slightly improved speed & quality of Temporal Anti-aliasing.
- Improved volume texture blending.
- Improved support for LDR Luts of sizes other than 1024x32. ([#507](https://github.com/Unity-Technologies/PostProcessing/issues/507))
- Bloom's `Fast Mode` has been made faster.
- Depth of Field focus is now independent from the screen resolution.
- The number of variants for some shaders has been reduced to improve first-build speed. The biggest one, Uber, is down to 576 variants.

## [2.0.3-preview] - 2018-03-13

### Fixed
- Disabled debug compute shaders on OpenGL ES3 to avoid crashes on a lot of Android devices.
- `NullReferenceException` while mixing volumes and global volumes. ([#498](https://github.com/Unity-Technologies/PostProcessing/issues/498))

### Changed
- Improved performances when blending between identical textures.

## [2.0.2-preview] - 2018-03-07

This is the first release of *PostProcessing*.
