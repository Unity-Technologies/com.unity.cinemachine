using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachinePOV))]
    internal sealed class CinemachinePOVEditor : BaseEditor<CinemachinePOV>
    {
        private void OnEnable()
        {
            Target.UpdateInputAxisProvider();
        }
    }
}
