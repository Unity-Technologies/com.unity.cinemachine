using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    [UnitCategory("Cinemachine")]
    [TypeIcon(typeof(CinemachineCamera))]
    public class ImpulseSource : Unit
    {
        [DoNotSerialize] [PortLabelHidden] public ControlInput inputTrigger;
        
        [DoNotSerialize]
        [NullMeansSelf]
        [PortLabel("Target")]
        [PortLabelHidden]
        public ValueInput target { get; private set; }

        protected override void Definition()
        {
            inputTrigger = ControlInput("inputTrigger", (flow) =>
            {
                var impulseSource = flow.GetComponent<CinemachineImpulseSource>(target);
                impulseSource.GenerateImpulse();
                return null;
            });
            target = ValueInput<GameObject>(nameof(target), null).NullMeansSelf();
        }
    }
}
