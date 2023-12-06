using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.VisualScripting;
using UnityEngine;

namespace Unity.Cinemachine.VisualScripting
{
    
    /// <summary>
    /// A set of utility functions to create nodes with dynamic Inputs, Outputs and types 
    /// </summary>
    public static class DynamicNodes
    {
        /// <summary>
        /// Initialize a read only collection of Value Input.
        /// </summary>
        /// <param name="dynamicUnit">The Unit base class.</param>
        /// <param name="appendName">The name before the input number.</param>
        /// <param name="defaultValue">The Default value of the input</param>
        /// <param name="inputCount">The number of inputs.</param>
        /// <typeparam name="T">The type of the input.</typeparam>
        /// <returns>Read only collection of inputs</returns>
        public static ReadOnlyCollection<ValueInput> InitializeMultipleInput <T>(this DynamicUnit dynamicUnit, string appendName, T defaultValue, int inputCount)
        {
            var multiInputs = new List<ValueInput>();

            var readOnlyInputs = multiInputs.AsReadOnly();

            for (var i = 0; i < inputCount; i++)
            {
                var newInput = dynamicUnit.GetValueInput<T>(appendName + i);
                newInput.SetDefaultValue(defaultValue);
                multiInputs.Add(newInput );
            }

            return readOnlyInputs;
        }
        
        /// <summary>
        /// Initialize a read only collection of Value Output.
        /// </summary>
        /// <param name="dynamicUnit">The Unit base class.</param>
        /// <param name="appendName">The name before the input number.</param>
        /// <param name="outputCount">The number of outputs.</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>Read only collection of outputs</returns>
        public static ReadOnlyCollection<ValueOutput> InitializeMultipleOutput <T>(this DynamicUnit dynamicUnit, string appendName, int outputCount)
        {
            var multiOutput = new List<ValueOutput>();

            var readOnlyOutputs = multiOutput.AsReadOnly();

            for (var i = 0; i < outputCount; i++)
            {
                var newOutput = dynamicUnit.GetValueOutput<T>(appendName + i);
                multiOutput.Add(newOutput);
            }

            return readOnlyOutputs;
        }

        /// <summary>
        /// Initialize a read only collection of Value Output.
        /// </summary>
        /// <param name="dynamicUnit">The Unit base class.</param>
        /// <param name="appendName">The name before the input number.</param>
        /// <param name="outputCount">The number of outputs.</param>
        /// <param name="getValue">The value</param>
        /// <typeparam name="T">The type of the output.</typeparam>
        /// <returns>Read only collection of outputs</returns>
        public static ReadOnlyCollection<ValueOutput> InitializeMultipleOutput <T>(this DynamicUnit dynamicUnit, string appendName,  Func<Flow, T> [] getValue)
        {
            var multiOutput = new List<ValueOutput>();

            var readOnlyOutputs = multiOutput.AsReadOnly();

            for (var i = 0; i < getValue.Length; i++)
            {
                var newOutput = dynamicUnit.GetValueOutput<T>(appendName + i, getValue[i]);
                multiOutput.Add(newOutput);
            }

            return readOnlyOutputs;
        }
        
        /// <summary>
        /// Get the component of a value input.
        /// </summary>
        /// <param name="flow">The current flow.</param>
        /// <param name="target">The Value input to get the component from.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <returns>The component returned.</returns>
        public static T GetComponent<T>(this Flow flow, ValueInput target, bool addIfNull = true) where T : MonoBehaviour
        {
            var component = flow.GetValue<T>(target);
            if (component == null && addIfNull)
            {
                var gameObject = flow.GetValue<GameObject>(target);
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
