using System;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    [UnitCategory("Cinemachine")]
    [TypeIcon(typeof(CinemachineCamera))]
    public class Sequencer : DynamicUnit, IMultiInputUnit
    {
        //Multiple input
        [DoNotSerialize] public ReadOnlyCollection<ValueInput> multiInputs { get; private set; }
        [DoNotSerialize] public ReadOnlyCollection<ValueOutput> multiOutput { get; private set; }
        [SerializeAs(nameof(inputCount))] private int _inputCount = 2;

        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("Count")]
        public int inputCount
        {
            get { return _inputCount; }
            set { _inputCount = Mathf.Clamp(value, 2, 8); }
        }

        [DoNotSerialize] public ValueInput head;
        [DoNotSerialize] public ValueOutput current;

        protected override void Definition()
        {
            head = ValueInput<float>(nameof(head), 0);
            current = ValueOutput<float>(nameof(current));
            multiInputs = this.InitializeMultipleInput<float>("S", 1, inputCount);
            var outputFlow = new Func<Flow, float>[inputCount];
            for (var i = 0; i < inputCount; i++)
            {
                outputFlow[i] = (flow) =>
                {
                    return flow.GetValue<float>(multiInputs[i]);
                };
            }
            multiOutput = this.InitializeMultipleOutput<float>("W", outputFlow);

            for (var i = 0; i < multiInputs.Count; i++)
            {
                //multiOutput[i]
            }
        }

        void CalculateSequence(Flow flow)
        {
            for (var i = 0; i < multiInputs.Count; i++)
            {
                
            }
        }
    }
}