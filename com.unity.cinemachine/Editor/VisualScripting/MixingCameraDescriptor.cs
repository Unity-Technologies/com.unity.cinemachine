using Unity.VisualScripting;

namespace Unity.Cinemachine.VisualScripting
{
    [Descriptor(typeof(MixingCamera))]
    public class MixingCameraDescriptor : UnitDescriptor<MixingCamera>
    {
        public MixingCameraDescriptor(MixingCamera unit) : base(unit) {}

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
        {
            base.DefinedPort(port, description);
            switch (port.key)
            {
                case nameof(MixingCamera.inputTrigger):
                    description.summary = "Update the component values";
                    break;
                case  nameof(MixingCamera.target):
                    description.summary = "Component targeted. Default is this. If none are found a new one will be added on trigger";
                    break;
                case "Weight0":
                    description.summary = "Weight of the first CinemachineCamera. -1 does not update component value.";
                    break;
                case "Weight1":
                    description.summary = "Weight of the first CinemachineCamera. -1 does not update component value.";
                    break;
            }
        }
    }
}
