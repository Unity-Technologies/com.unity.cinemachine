using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    public enum EaseType
    {
        Linear,
        EaseInOut
    }
    
    
    [UnitCategory("Cinemachine")]
    [TypeIcon(typeof(CinemachineCamera))]
    public class CrossFade : Unit
    {
        [SerializeAs(nameof(easeType))] private EaseType _easeType = EaseType.Linear;
        
        [DoNotSerialize]
        [Inspectable, UnitHeaderInspectable("")]
        public EaseType easeType
        {
            get { return _easeType; }
            set { _easeType = value; }
        }
        
        [PortLabel("Head")]
        [DoNotSerialize] public ValueInput head;
        
        [PortLabel("Start")]
        [DoNotSerialize] public ValueInput start;
        
        [PortLabel("End")]
        [DoNotSerialize] public ValueInput end;

        private float resultValue1;
        private float resultValue2;
        [DoNotSerialize] [PortLabelHidden] public ValueOutput weightOut;
        [DoNotSerialize] [PortLabelHidden] public ValueOutput weightIn;

        protected override void Definition()
        {
            head = ValueInput<float>(nameof(head), 0);
            start = ValueInput<float>(nameof(start), 5);
            end = ValueInput<float>(nameof(end), 10);
            weightOut = ValueOutput(nameof(weightOut), (flow) =>
            {
                var time = 0f;
                switch (easeType)
                {
                    case EaseType.Linear:
                        time = Mathf.InverseLerp(flow.GetValue<float>(start), flow.GetValue<float>(end), flow.GetValue<float>(head));
                        break;
                    case EaseType.EaseInOut:
                        time = EaseIn(Mathf.InverseLerp(flow.GetValue<float>(start), flow.GetValue<float>(end), flow.GetValue<float>(head)));
                        break;
                }
                var t = Mathf.Clamp01(Mathf.Lerp (0, 1, time));
                resultValue1 = 1 - t;
                resultValue2 = t;
                return resultValue1;
            });
            
            weightIn = ValueOutput(nameof(weightIn), (_) => resultValue2);
        }
        
        float EaseIn(float t)
        {
            return t * t;
        }
    }
}