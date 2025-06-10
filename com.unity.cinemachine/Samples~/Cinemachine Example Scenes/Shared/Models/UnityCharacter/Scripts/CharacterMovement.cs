using UnityEngine;

namespace Cinemachine.Examples
{
    [AddComponentMenu("")] // Don't display in add component menu
    public class CharacterMovement : MonoBehaviour
    {
        public bool useCharacterForward = false;
        public KeyCode sprintJoystick = KeyCode.JoystickButton2;
        public KeyCode sprintKeyboard = KeyCode.Space;

        private float speed = 0f;
        private float direction = 0f;
        private bool isSprinting = false;
        private Rigidbody rb;
        private Animator anim;
        private Vector2 input;
        private Camera mainCamera;
        private float velocity;

        void Start()
        {
            anim = GetComponent<Animator>();
#if UNITY_6000_0_OR_NEWER
            anim.updateMode = AnimatorUpdateMode.Fixed;
            anim.animatePhysics = true;
#else
            anim.updateMode = AnimatorUpdateMode.AnimatePhysics;
#endif
            anim.applyRootMotion = true;
            mainCamera = Camera.main;
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        }

        void Update()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");

            // set speed to both vertical and horizontal inputs
            if (useCharacterForward)
                speed = Mathf.Abs(input.x) + input.y;
            else
                speed = Mathf.Abs(input.x) + Mathf.Abs(input.y);

            speed = Mathf.Clamp(speed, 0f, 1f);
            speed = Mathf.SmoothDamp(anim.GetFloat("Speed"), speed, ref velocity, 0.1f);

            if (input.y < 0f && useCharacterForward)
                direction = input.y;
            else
                direction = 0f;

            isSprinting = (Input.GetKey(sprintJoystick) || Input.GetKey(sprintKeyboard)) && input != Vector2.zero && direction >= 0f;
    #else
            InputSystemHelper.EnableBackendsWarningMessage();
    #endif
        }

        // Interact with Rigidbody only in FixedUpdate
        void FixedUpdate()
        {
            anim.SetFloat("Speed", speed);
            anim.SetFloat("Direction", direction);
            anim.SetBool("isSprinting", isSprinting);

            // Update target direction relative to the camera view or player forward
            var tr = useCharacterForward ? transform : mainCamera.transform;
            var right = tr.right;
            var forward = tr.forward;
            forward.y = 0;
            var targetDir = input.x * right + (useCharacterForward ? Mathf.Abs(input.y) : input.y) * forward;

            if (input == Vector2.zero || targetDir.magnitude < 0.1f)
                rb.angularVelocity = Vector3.zero;
            else
            {
                targetDir = targetDir.normalized;
                var currentDir = rb.rotation * Vector3.forward;

                // Rigidbody.MoveRotation breaks interpolation for non-kinematic rigidbodies, so don't use it
                var angle = Vector3.SignedAngle(currentDir, targetDir, Vector3.up) * Mathf.Deg2Rad / Time.fixedDeltaTime;
                rb.angularVelocity = Vector3.up * angle;
            }
        }
    }
}
