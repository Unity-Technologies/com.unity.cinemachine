#if CINEMACHINE_UNITY_INPUTSYSTEM

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Cinemachine
{
    static class CinemachineDefaultMouseInput
    {
        static DefaultMouseInput s_DefaultMouseInput = new DefaultMouseInput();
        public static InputActionReference GetInputActionReference()
        {
            return InputActionReference.Create(s_DefaultMouseInput.CMInputProvider.MouseXY);
        }
        
        // GENERATED AUTOMATICALLY FROM 'Assets/DefaultMouseInput.inputactions'
        class @DefaultMouseInput : IInputActionCollection, IDisposable
        {
            public InputActionAsset asset { get; }

            public @DefaultMouseInput()
            {
                asset = InputActionAsset.FromJson(@"{
                    ""name"": ""DefaultMouseInput"",
                    ""maps"": [
                        {
                            ""name"": ""CMInputProvider"",
                            ""id"": ""58fd55e8-1d7c-4e1c-8b41-c5b0b9b084e1"",
                            ""actions"": [
                                {
                                    ""name"": ""MouseXY"",
                                    ""type"": ""Value"",
                                    ""id"": ""0fb909e5-6fae-4b52-9c6a-42e266a91c63"",
                                    ""expectedControlType"": ""Vector2"",
                                    ""processors"": """",
                                    ""interactions"": """"
                                }
                            ],
                            ""bindings"": [
                                {
                                    ""name"": """",
                                    ""id"": ""49707a4f-ce91-4104-80ee-405a8a9b4cfe"",
                                    ""path"": ""<Pointer>/delta"",
                                    ""interactions"": """",
                                    ""processors"": ""ScaleVector2"",
                                    ""groups"": ""Mouse"",
                                    ""action"": ""MouseXY"",
                                    ""isComposite"": false,
                                    ""isPartOfComposite"": false
                                }
                            ]
                        }
                    ],
                    ""controlSchemes"": []
                }");

                // CMInputProvider
                m_CMInputProvider = asset.FindActionMap("CMInputProvider", throwIfNotFound: true);
                m_CMInputProvider_MouseXY = m_CMInputProvider.FindAction("MouseXY", throwIfNotFound: true);
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(asset);
            }

            public InputBinding? bindingMask
            {
                get => asset.bindingMask;
                set => asset.bindingMask = value;
            }

            public ReadOnlyArray<InputDevice>? devices
            {
                get => asset.devices;
                set => asset.devices = value;
            }

            public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

            public bool Contains(InputAction action)
            {
                return asset.Contains(action);
            }

            public IEnumerator<InputAction> GetEnumerator()
            {
                return asset.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Enable()
            {
                asset.Enable();
            }

            public void Disable()
            {
                asset.Disable();
            }

            // CMInputProvider
            private readonly InputActionMap m_CMInputProvider;
            private ICMInputProviderActions m_CMInputProviderActionsCallbackInterface;
            private readonly InputAction m_CMInputProvider_MouseXY;

            public struct CMInputProviderActions
            {
                private @DefaultMouseInput m_Wrapper;

                public CMInputProviderActions(@DefaultMouseInput wrapper)
                {
                    m_Wrapper = wrapper;
                }

                public InputAction @MouseXY => m_Wrapper.m_CMInputProvider_MouseXY;

                public InputActionMap Get()
                {
                    return m_Wrapper.m_CMInputProvider;
                }

                public void Enable()
                {
                    Get().Enable();
                }

                public void Disable()
                {
                    Get().Disable();
                }

                public bool enabled => Get().enabled;

                public static implicit operator InputActionMap(CMInputProviderActions set)
                {
                    return set.Get();
                }

                public void SetCallbacks(ICMInputProviderActions instance)
                {
                    if (m_Wrapper.m_CMInputProviderActionsCallbackInterface != null)
                    {
                        @MouseXY.started -= m_Wrapper.m_CMInputProviderActionsCallbackInterface.OnMouseXY;
                        @MouseXY.performed -= m_Wrapper.m_CMInputProviderActionsCallbackInterface.OnMouseXY;
                        @MouseXY.canceled -= m_Wrapper.m_CMInputProviderActionsCallbackInterface.OnMouseXY;
                    }

                    m_Wrapper.m_CMInputProviderActionsCallbackInterface = instance;
                    if (instance != null)
                    {
                        @MouseXY.started += instance.OnMouseXY;
                        @MouseXY.performed += instance.OnMouseXY;
                        @MouseXY.canceled += instance.OnMouseXY;
                    }
                }
            }

            public CMInputProviderActions @CMInputProvider => new CMInputProviderActions(this);

            public interface ICMInputProviderActions
            {
                void OnMouseXY(InputAction.CallbackContext context);
            }
        }
    }
}

#endif