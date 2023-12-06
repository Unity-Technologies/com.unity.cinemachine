using Unity.VisualScripting;

namespace Unity.Cinemachine.VisualScripting
{
    [Descriptor(typeof(Damp))]
    public class DampingFloatDescriptor : UnitDescriptor<Damp>
    {
        public DampingFloatDescriptor(Damp unit) : base(unit) {}

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
        {
            base.DefinedPort(port, description);
            switch (port.key)
            {
                case  nameof(Damp.time):
                    description.summary = "Approximate time in second to reach the target";
                    break;
                case nameof(Damp.rawValue):
                    description.summary = "The value to damp.";
                    break;
                case nameof(Damp.resetInputTrigger):
                case nameof(Damp.reset):
                    description.summary = "Resets the damping to the current value.";
                    break;
                case nameof(Damp.result):
                    description.summary = "The damped result";
                    break;
            }
        }
    }
}
