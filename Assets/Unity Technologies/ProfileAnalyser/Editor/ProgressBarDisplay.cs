using UnityEngine;
using UnityEditor;

namespace ProfileAnalyser
{
    public class ProgressBarDisplay
    {
        private int m_totalFrames;
        private int m_currentFrame;
        private string m_title;
        private string m_description;

        public void InitProgressBar(string title, string description, int frames)
        {
            m_currentFrame = 0;
            m_totalFrames = frames;

            m_title = title;
            m_description = description;

            EditorUtility.DisplayProgressBar(m_title, m_description, m_currentFrame);
        }

		public void AdvanceProgressBar()
        {
            m_currentFrame++;
            int currentFrame = Mathf.Clamp(0, m_currentFrame, m_totalFrames);
            float progress = (float)currentFrame / m_totalFrames;
            EditorUtility.DisplayProgressBar(m_title, m_description, progress);
        }

        public void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
