#if UNITY_2017_3_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;

namespace UnityEngine.Recorder.Input
{
    public class ScreenCaptureInput : RecorderInput
    {
        bool m_ModifiedResolution;

        public Texture2D image { get; private set; }

        public ScreenCaptureInputSettings scSettings
        {
            get { return (ScreenCaptureInputSettings)settings; }
        }

        public int outputWidth { get; protected set; }
        public int outputHeight { get; protected set; }

        public override void NewFrameReady(RecordingSession session)
        {
            image = ScreenCapture.CaptureScreenshotAsTexture();
        }

        public override void BeginRecording(RecordingSession session)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
#if UNITY_EDITOR
            switch (scSettings.m_OutputSize)
            {
                case EImageDimension.Window:
                {
                    GameViewSize.GetGameRenderSize(out screenWidth, out screenHeight);
                    outputWidth = screenWidth;
                    outputHeight = screenHeight;

                    if (scSettings.m_ForceEvenSize)
                    {
                        outputWidth = (outputWidth + 1) & ~1;
                        outputHeight = (outputHeight + 1) & ~1;
                    }
                    break;
                }

                default:
                {
                    outputHeight = (int)scSettings.m_OutputSize;
                    outputWidth = (int)(outputHeight * AspectRatioHelper.GetRealAR(scSettings.m_AspectRatio));

                    if (scSettings.m_ForceEvenSize)
                    {
                        outputWidth = (outputWidth + 1) & ~1;
                        outputHeight = (outputHeight + 1) & ~1;
                    }

                    break;
                }
            }

            int w, h;
            GameViewSize.GetGameRenderSize(out w, out h);
            if (w != outputWidth || h != outputHeight)
            {
                var size = GameViewSize.SetCustomSize(outputWidth, outputHeight) ?? GameViewSize.AddSize(outputWidth, outputHeight);
                if (GameViewSize.m_ModifiedResolutionCount == 0)
                    GameViewSize.BackupCurrentSize();
                else
                {
                    if (size != GameViewSize.currentSize)
                    {
                        Debug.LogError("Requestion a resultion change while a recorder's input has already requested one! Undefined behaviour.");
                    }
                }
                GameViewSize.m_ModifiedResolutionCount++;
                m_ModifiedResolution = true;
                GameViewSize.SelectSize(size);
            }
#endif

        }

        public override void FrameDone(RecordingSession session)
        {
            UnityHelpers.Destroy(image);
            image = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
#if UNITY_EDITOR
                if (m_ModifiedResolution)
                {
                    GameViewSize.m_ModifiedResolutionCount--;
                    if (GameViewSize.m_ModifiedResolutionCount == 0)
                        GameViewSize.RestoreSize();
                }
#endif
            }

            base.Dispose(disposing);
        }
    }
}

#endif