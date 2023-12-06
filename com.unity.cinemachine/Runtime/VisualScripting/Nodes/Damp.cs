using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    [UnitCategory("Cinemachine")]
    [TypeIcon(typeof(CinemachineCamera))]
    public class Damp : Unit
    {
        [SerializeAs(nameof(easeType))] private bool _flow = true;

        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Flow")]
        public bool easeType
        {
            get { return _flow; }
            set { _flow = value; }
        }

        [DoNotSerialize] [PortLabel("Update")] public ControlInput updateInputTrigger;

        [DoNotSerialize] [PortLabel("Reset")] public ControlInput resetInputTrigger;

        [DoNotSerialize] [PortLabelHidden] public ControlOutput outputTrigger;

        [PortLabel("Value")] [DoNotSerialize] public ValueInput rawValue;

        [PortLabel("Time")] [DoNotSerialize] public ValueInput time;

        [PortLabel("Reset")] [DoNotSerialize] public ValueInput reset;

        private float velocity;

        private float resultValue;

        private bool isInitialized;

        [DoNotSerialize] [PortLabelHidden] public ValueOutput result;

        protected override void Definition()
        {
            rawValue = ValueInput<float>(nameof(rawValue), 0);
            time = ValueInput<float>(nameof(time), 1);
            result = ValueOutput(nameof(result), (flow) =>
            {
                if (!_flow)
                {
                    if (flow.GetValue<bool>(reset))
                    {
                        velocity = 0;
                        resultValue = flow.GetValue<float>(rawValue);
                    }
                    UpdateAndInitialize(flow);
                }

                return resultValue;
            });

            if (_flow)
            {
                outputTrigger = ControlOutput(nameof(outputTrigger));
                updateInputTrigger = ControlInput(nameof(updateInputTrigger), (flow) =>
                {
                    UpdateAndInitialize(flow);
                    return outputTrigger;
                });
                resetInputTrigger = ControlInput(nameof(resetInputTrigger), (flow) =>
                {
                    velocity = 0;
                    resultValue = flow.GetValue<float>(rawValue);
                    return outputTrigger;
                });
                Succession(updateInputTrigger, outputTrigger);
                Succession(resetInputTrigger, outputTrigger);
            }
            else
            {
                reset = ValueInput<bool>(nameof(reset), false);
            }
        }

        void UpdateAndInitialize(Flow flow)
        {
            var value = flow.GetValue<float>(rawValue);
            if (!isInitialized)
            {
                resultValue = value;
                isInitialized = true;
            }

            resultValue = Mathf.SmoothDamp(resultValue, flow.GetValue<float>(rawValue), ref velocity,
                flow.GetValue<float>(time));
        }
    }
}