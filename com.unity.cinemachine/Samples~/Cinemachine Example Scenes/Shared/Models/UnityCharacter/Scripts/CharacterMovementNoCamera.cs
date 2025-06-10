using UnityEngine;

namespace Cinemachine.Examples
{
    // WASD to move, Space to sprint
    public class CharacterMovementNoCamera : MonoBehaviour
    {
        public Transform InvisibleCameraOrigin;

        public float StrafeSpeed = 0.1f;
        public float TurnSpeed = 3;
        public float Damping = 0.2f;
        public float VerticalRotMin = -80;
        public float VerticalRotMax = 80;
        public KeyCode sprintJoystick = KeyCode.JoystickButton2;
        public KeyCode sprintKeyboard = KeyCode.Space;

        private bool isSprinting;
        private Animator anim;
        private Rigidbody rb;
        private float currentStrafeSpeed;
        private Vector2 currentVelocity;
        private Vector2 input;
        private Vector2 rotInput;
        private float speed;

        void OnEnable()
        {
            anim = GetComponent<Animator>();
#if UNITY_6000_0_OR_NEWER
            anim.updateMode = AnimatorUpdateMode.Fixed;
            anim.animatePhysics = true;
#else
            anim.updateMode = AnimatorUpdateMode.AnimatePhysics;
#endif
            anim.applyRootMotion = true;
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            currentVelocity = Vector2.zero;
            currentStrafeSpeed = 0;
            isSprinting = false;
        }

        void Update()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            rotInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            isSprinting = (Input.GetKey(sprintJoystick) || Input.GetKey(sprintKeyboard)) && speed > 0;

            if (InvisibleCameraOrigin != null)
            {
                var rot = InvisibleCameraOrigin.localRotation.eulerAngles;
                rot.x -= rotInput.y * TurnSpeed;
                if (rot.x > 180)
                    rot.x -= 360;
                rot.x = Mathf.Clamp(rot.x, VerticalRotMin, VerticalRotMax);
                InvisibleCameraOrigin.localRotation = Quaternion.Euler(rot);
            }
    #else
            InputSystemHelper.EnableBackendsWarningMessage();
    #endif
        }

        void FixedUpdate()
        {
            anim.SetFloat("Speed", speed);
            anim.SetFloat("Direction", speed);
            anim.SetBool("isSprinting", isSprinting);

            // strafing
            speed = input.y;
            speed = Mathf.Clamp(speed, -1f, 1f);
            speed = Mathf.SmoothDamp(anim.GetFloat("Speed"), speed, ref currentVelocity.y, Damping);

            currentStrafeSpeed = Mathf.SmoothDamp(
                currentStrafeSpeed, input.x * StrafeSpeed, ref currentVelocity.x, Damping);
            //rb.MovePosition(rb.position + rb.rotation * Vector3.right * currentStrafeSpeed);
            rb.AddForce(rb.rotation * Vector3.right * currentStrafeSpeed, ForceMode.VelocityChange);

            var euler = rb.rotation.eulerAngles;
            euler.y += rotInput.x * TurnSpeed;
            var rot = Quaternion.Euler(euler);

            // Rigidbody.MoveRotation breaks interpolation for non-kinematic rigidbodies, so don't use it
            var angle = Vector3.SignedAngle(
                rb.rotation * Vector3.forward, rot * Vector3.forward, Vector3.up) * Mathf.Deg2Rad / Time.fixedDeltaTime;
            rb.angularVelocity = Vector3.up * angle;
        }
    }
}
