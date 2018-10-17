#ifdef UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
    #error Mixing HDCamera and legacy Unity matrix definitions
#endif

#ifndef UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
#define UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED

#if defined(USING_STEREO_MATRICES)

#define UNITY_MATRIX_V     _ViewMatrixStereo[unity_StereoEyeIndex]
#define UNITY_MATRIX_I_V   _InvViewMatrixStereo[unity_StereoEyeIndex]
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(_ProjMatrixStereo[unity_StereoEyeIndex])
#define UNITY_MATRIX_I_P   _InvProjMatrixStereo[unity_StereoEyeIndex]
#define UNITY_MATRIX_VP    _ViewProjMatrixStereo[unity_StereoEyeIndex]
#define UNITY_MATRIX_I_VP  _InvViewProjMatrixStereo[unity_StereoEyeIndex]
#define UNITY_MATRIX_UNJITTERED_VP _ViewProjMatrixStereo[unity_StereoEyeIndex] // Since VR doesn't need to add jitter, just use normal VP matrix
#define UNITY_MATRIX_PREV_VP _PrevViewProjMatrixStereo[unity_StereoEyeIndex]

#else

#define UNITY_MATRIX_V     _ViewMatrix
#define UNITY_MATRIX_I_V   _InvViewMatrix
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(_ProjMatrix)
#define UNITY_MATRIX_I_P   _InvProjMatrix
#define UNITY_MATRIX_VP    _ViewProjMatrix
#define UNITY_MATRIX_I_VP  _InvViewProjMatrix
#define UNITY_MATRIX_UNJITTERED_VP _NonJitteredViewProjMatrix
#define UNITY_MATRIX_PREV_VP _PrevViewProjMatrix

#endif // USING_STEREO_MATRICES

#endif // UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
