using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    [ExecuteAlways]
    [SaveDuringPlay]
    public class MouseScrollZoom2D : CinemachineExtension, IInputAxisSource
    {
        /// <summary>Axis representing the current horizontal rotation.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Axis representing the current horizontal rotation.  Value is in degrees "
            + "and represents a rotation about the Y axis.")]
        public InputAxis ZoomAxis = DefaultZoomAxis;
        
        static InputAxis DefaultZoomAxis => new () { Value = 40, Range = new Vector2(20, 60), Wrap = false, Center = 40 };
        
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
            {
                var zoomValue = ZoomAxis.Value - ZoomAxis.Center;
                if (state.Lens.Orthographic)
                {
                    
                }
                else
                {
                    state.Lens.FieldOfView += zoomValue;
                }
            }
        }
        
        public void GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { DrivenAxis = () => ref ZoomAxis, Name = "Zoom", AxisIndex = 2 });
        }

        void Reset()
        {
            ZoomAxis = DefaultZoomAxis;
        }
    }
}
