#if CINEMACHINE_UIELEMENTS && UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine
{
    class DebugText : IDisposable
    {
        Label m_DebugLabel;
        StyleColor m_OriginalTextColor;

        public DebugText(Camera outputCamera)
        {
            m_DebugLabel = new Label
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    backgroundColor = new StyleColor(new Color(0.5f, 0.5f, 0.5f, 0.5f)),
                    marginBottom = new StyleLength(new Length(2, LengthUnit.Pixel)),
                    marginTop = new StyleLength(new Length(2, LengthUnit.Pixel)),
                    marginLeft = new StyleLength(new Length(2, LengthUnit.Pixel)),
                    marginRight = new StyleLength(new Length(2, LengthUnit.Pixel)),
                    paddingBottom = new StyleLength(new Length(0, LengthUnit.Pixel)),
                    paddingTop = new StyleLength(new Length(0, LengthUnit.Pixel)),
                    paddingLeft = new StyleLength(new Length(0, LengthUnit.Pixel)),
                    paddingRight = new StyleLength(new Length(0, LengthUnit.Pixel)),
                    fontSize = new StyleLength(new Length(12, LengthUnit.Pixel)),
                    color = new StyleColor(Color.white),
                    position = new StyleEnum<Position>(Position.Relative),
                    alignSelf = new StyleEnum<Align>(Align.FlexStart)
                }
            };
            m_OriginalTextColor = m_DebugLabel.style.color;
                
            var debugUIContainer = CinemachineDebug.GetOrCreateUIContainer(outputCamera);
            debugUIContainer.Add(m_DebugLabel);
        }

        public void SetTextColor(Color color) => m_DebugLabel.style.color = new StyleColor(color);
        public void RestoreOriginalTextColor() => m_DebugLabel.style.color = m_OriginalTextColor;
        public void SetText(string text) => m_DebugLabel.text = text;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~DebugText()
        {
            ReleaseUnmanagedResources();
        }
            
        void ReleaseUnmanagedResources()
        {
            if (m_DebugLabel != null)
            {
                m_DebugLabel.RemoveFromHierarchy();
                m_DebugLabel = null;
            }
        }
    }
}
#endif
