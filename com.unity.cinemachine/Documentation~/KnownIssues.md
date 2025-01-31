# Known Issues

## Accumulation Buffer Projection Matrix
If accumulation's "Anti-aliasing" option is enabled and the scene contains a Cinemachine camera cut, the camera's FOV will be incorrect after the cut.
**Workaround**: Reset the projection matrix every frame, after CinemachineBrain has modified the camera.


    public class FixProjection : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera.main.ResetProjectionMatrix();
        }
    }
