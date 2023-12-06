using System;
using Unity.VisualScripting;

namespace Unity.Cinemachine.VisualScripting
{
    public abstract class DynamicUnit : Unit
    {
        public ValueInput GetValueInput<T>(string key)
        {
            return ValueInput<T>(key);
        }

        public ValueOutput GetValueOutput<T>(string key)
        {
            return ValueOutput<T>(key);
        }
        
        public ValueOutput GetValueOutput<T>(string key, Func<Flow, T> getValue)
        {
            return ValueOutput<T>(key, getValue);
        }
    }
}
