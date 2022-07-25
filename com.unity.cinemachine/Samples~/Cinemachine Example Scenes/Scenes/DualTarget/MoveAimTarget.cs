using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    public class MoveAimTarget : MonoBehaviour, IInputAxisSource
    {
        public CinemachineBrain Brain;
        public RectTransform ReticleImage;

        [Tooltip("How far to raycast to place the aim target")]
        public float AimDistance = 200;

        [Tooltip("Objects on these layers will be detected")]
        public LayerMask CollideAgainst = 1;

        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  "
            + "It's a good idea to set this field to the player's tag")]
        public string IgnoreTag = string.Empty;

        /// <summary>The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation</summary>
        [Header("Input Axes")]
        [Tooltip("The Horizontal axis.  Value is -180..180.  Controls the horizontal orientation")]
        public InputAxis HorizontalAxis = new InputAxis { Range = new Vector2(-180, 180), Wrap = true };

        /// <summary>The Vertical axis.  Value is -90..90. Controls the vertical orientation</summary>
        [Tooltip("The Vertical axis.  Value is -90..90. Controls the vertical orientation")]
        public InputAxis VerticalAxis = new InputAxis { Range = new Vector2(-70, 70) };

        /// <summary>Report the available input axes</summary>
        /// <param name="axes">Output list to which the axes will be added</param>
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = HorizontalAxis, Name = "Aim Look X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = VerticalAxis, Name = "Aim Look Y", AxisIndex = 1 });
        }
        
        private void OnValidate()
        {
            VerticalAxis.Validate();
            HorizontalAxis.Validate();
            AimDistance = Mathf.Max(1, AimDistance);
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(PlaceReticle);
            CinemachineCore.CameraUpdatedEvent.AddListener(PlaceReticle);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(PlaceReticle);
        }

        private void Update()
        {
            if (Brain == null)
                return;

            PlaceTarget();
        }

        private void PlaceTarget()
        {
            var rot = Quaternion.Euler(VerticalAxis.Value, HorizontalAxis.Value, 0);
            var camPos = Brain.CurrentCameraState.RawPosition;
            transform.position = GetProjectedAimTarget(camPos + rot * Vector3.forward, camPos);
        }

        private Vector3 GetProjectedAimTarget(Vector3 pos, Vector3 camPos)
        {
            var origin = pos;
            var fwd = (pos - camPos).normalized;
            pos += AimDistance * fwd;
            if (CollideAgainst != 0 && RaycastIgnoreTag(
                new Ray(origin, fwd),
                out RaycastHit hitInfo, AimDistance, CollideAgainst))
            {
                pos = hitInfo.point;
            }

            return pos;
        }

        private bool RaycastIgnoreTag(
            Ray ray, out RaycastHit hitInfo, float rayLength, int layerMask)
        {
            const float PrecisionSlush = 0.001f;
            float extraDistance = 0;
            while (Physics.Raycast(
                ray, out hitInfo, rayLength, layerMask,
                QueryTriggerInteraction.Ignore))
            {
                if (IgnoreTag.Length == 0 || !hitInfo.collider.CompareTag(IgnoreTag))
                {
                    hitInfo.distance += extraDistance;
                    return true;
                }

                // Ignore the hit.  Pull ray origin forward in front of obstacle
                Ray inverseRay = new Ray(ray.GetPoint(rayLength), -ray.direction);
                if (!hitInfo.collider.Raycast(inverseRay, out hitInfo, rayLength))
                    break;
                float deltaExtraDistance = rayLength - (hitInfo.distance - PrecisionSlush);
                if (deltaExtraDistance < PrecisionSlush)
                    break;
                extraDistance += deltaExtraDistance;
                rayLength = hitInfo.distance - PrecisionSlush;
                if (rayLength < PrecisionSlush)
                    break;
                ray.origin = inverseRay.GetPoint(rayLength);
            }

            return false;
        }

        void PlaceReticle(CinemachineBrain brain)
        {
            if (brain == null || brain != Brain || ReticleImage == null || brain.OutputCamera == null)
                return;
            PlaceTarget(); // To eliminate judder
            CameraState state = brain.CurrentCameraState;
            var cam = brain.OutputCamera;
            var r = cam.WorldToScreenPoint(transform.position);
            var r2 = new Vector2(r.x - cam.pixelWidth * 0.5f, r.y - cam.pixelHeight * 0.5f);
            ReticleImage.anchoredPosition = r2;
        }
    }
}