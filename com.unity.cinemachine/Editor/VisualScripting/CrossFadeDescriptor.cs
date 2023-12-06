using Unity.VisualScripting;

namespace Unity.Cinemachine.VisualScripting
{
    [Descriptor(typeof(CrossFade))]
    public class CrossFadeDescriptor : UnitDescriptor<CrossFade>
    {
        public CrossFadeDescriptor(CrossFade unit) : base(unit) {}

        protected override void DefinedPort(IUnitPort port, UnitPortDescription description)
        {
            base.DefinedPort(port, description);
            switch (port.key)
            {
                case  nameof(CrossFade.head):
                    description.summary = "Determine where the cross fade is. ";
                    break;
                case nameof(CrossFade.start):
                    description.summary = "Determines the end of the fade. When head = start, Weight In = 1 and Weight Out = 0";
                    break;
                case nameof(CrossFade.end):
                    description.summary = "Determines the end of the fade. When head = end, Weight In = 0 and Weight Out = 1";
                    break;
                case nameof(CrossFade.weightIn):
                    description.summary = "The track faded in.";
                    break;
                case nameof(CrossFade.weightOut):
                    description.summary = "The track faded out.";
                    break;
            }
        }
    }
}