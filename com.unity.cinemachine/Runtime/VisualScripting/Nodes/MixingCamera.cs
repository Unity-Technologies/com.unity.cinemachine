using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    [UnitCategory("Cinemachine")]
    [TypeIcon(typeof(CinemachineMixingCamera))]
    public class MixingCamera : DynamicUnit, IMultiInputUnit
    {
        //Multiple input
        [DoNotSerialize] public ReadOnlyCollection<ValueInput> multiInputs { get; protected set; }
        [SerializeAs(nameof(inputCount))] private int _inputCount = 2;

        [DoNotSerialize] [PortLabelHidden] public ControlInput inputTrigger;
        [DoNotSerialize] [PortLabelHidden] public ControlOutput outputTrigger;

        [DoNotSerialize]
        [NullMeansSelf]
        [PortLabel("Target")]
        [PortLabelHidden]
        public ValueInput target { get; private set; }
        
        
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Child Count")]
        public int inputCount
        {
            get { return _inputCount; }
            set { _inputCount = Mathf.Clamp(value, 2, 8); }
        }
        
        protected override void Definition()
        {
            multiInputs = this.InitializeMultipleInput<float>("Weight", -1, inputCount);
            inputTrigger = ControlInput("inputTrigger", (flow) =>
            {
                var mixingCamera = flow.GetComponent<CinemachineMixingCamera>(target);

                for (int i = 0; i < multiInputs.Count; i++)
                {
                    var inputValue = flow.GetValue<float>(multiInputs[i]);
                    if (inputValue >= 0)
                        mixingCamera.SetWeight(i, inputValue);
                }

                return outputTrigger;
            });
            outputTrigger = ControlOutput("outputTrigger");
            target = ValueInput<GameObject>(nameof(target), null).NullMeansSelf();
            Succession(inputTrigger, outputTrigger);
        }
    }
}