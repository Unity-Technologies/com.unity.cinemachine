using UnityEngine;

namespace Cinemachine.Examples
{
    [AddComponentMenu("")] // Don't display in add component menu
    public class CharacterMovement2D : MonoBehaviour
    {
        public KeyCode sprintJoystick = KeyCode.JoystickButton2;
        public KeyCode jumpJoystick = KeyCode.JoystickButton0;
        public KeyCode sprintKeyboard = KeyCode.LeftShift;
        public KeyCode jumpKeyboard = KeyCode.Space;
        public float jumpVelocity = 7f;
        public float groundTolerance = 0.2f;
        public bool checkGroundForJump = true;

        float speed = 0f;
        bool isSprinting = false;
        Animator anim;
        Vector2 input;
        float velocity;
        bool headingleft = false;
        Quaternion targetrot;
        Rigidbody rb;

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
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            targetrot = transform.rotation;        
        }

        private void Update()
        {
    #if ENABLE_LEGACY_INPUT_MANAGER
            input.x = Input.GetAxis("Horizontal");
            speed = Mathf.Abs(input.x);
            speed = Mathf.SmoothDamp(anim.GetFloat("Speed"), speed, ref velocity, 0.1f);

            // Jump
            if ((Input.GetKeyDown(jumpJoystick) || Input.GetKeyDown(jumpKeyboard)))
            {
                rb.AddForce(new Vector3(0, jumpVelocity, 0), ForceMode.Impulse);
            }

            // set sprinting
            if ((Input.GetKeyDown(sprintJoystick) || Input.GetKeyDown(sprintKeyboard)) && input != Vector2.zero) 
                isSprinting = true;
            if ((Input.GetKeyUp(sprintJoystick) || Input.GetKeyUp(sprintKeyboard)) || input == Vector2.zero) 
                isSprinting = false;
    #else
            InputSystemHelper.EnableBackendsWarningMessage();
    #endif
        }

        void FixedUpdate()
        {
            anim.SetFloat("Speed", speed);
            anim.SetBool("isSprinting", isSprinting);

            // Check if direction changes
            if ((input.x < 0f && !headingleft) || (input.x > 0f && headingleft))
            {  
                if (input.x < 0f) targetrot = Quaternion.Euler(0, 270, 0);
                if (input.x > 0f) targetrot = Quaternion.Euler(0, 90, 0);
                headingleft = !headingleft;
            }
            // Rotate player if direction changes
            var rot = Quaternion.Lerp(rb.rotation, targetrot, Time.deltaTime * 20f);

            // Rigidbody.MoveRotation breaks interpolation for non-kinematic rigidbodies, so don't use it
            var angle = Vector3.SignedAngle(
                rb.rotation * Vector3.forward, rot * Vector3.forward, Vector3.up) * Mathf.Deg2Rad / Time.fixedDeltaTime;
            rb.angularVelocity = Vector3.up * angle;
        }

        public bool isGrounded()
        {
            if (checkGroundForJump)
                return Physics.Raycast(transform.position, Vector3.down, groundTolerance);
            return true;
        }
    }
}
