using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Displays a button in the game view that will bring up a window with text.
    /// </summary>
    [ExecuteInEditMode]
    public class SamplesHelpGUI : MonoBehaviour
    {
#if UNITY_EDITOR
        void Start()
        {
            if (Selection.activeGameObject is null)
            {
                Selection.activeGameObject = gameObject;
            }
        }
#endif

        [Tooltip("The text to display on the button")] [SerializeField]
        string ButtonText = "Help";

        [Tooltip("Screen position of the button in normalized screen coords")] [SerializeField]
        Vector2 Position = new Vector3(0.9f, 0.05f);

        [Tooltip("Text is displayed when this is checked")] [SerializeField]
        bool Displayed = false;

        [TextArea(minLines: 10, maxLines: 50)] [SerializeField]
        string HelpText;

        [SerializeField] 
        CinemachineInformations[] sampleInformations;
        
        [SerializeField]
        bool isTutorialValid = true;

        Vector2 m_Size = Vector2.zero;

        public void InvalidTutorial()
        {
            isTutorialValid = false;
        }
        
        private void OnGUI()
        {
            if(!Application.isPlaying)
                return;
            
            // Establish button size (only once)
            if (m_Size == Vector2.zero)
                m_Size = GUI.skin.button.CalcSize(new GUIContent(ButtonText));

            if (Displayed)
            {
                var size = GUI.skin.label.CalcSize(new GUIContent(HelpText));
                var halfSize = size * 0.5f;

                float maxWidth = Mathf.Min(Screen.width / 2, size.x);
                float left = Screen.width * 0.5f - maxWidth * 0.5f;
                float top = Screen.height * 0.4f - halfSize.y;

                var windowRect = new Rect(left, top, maxWidth, size.y);
                var title = SceneManager.GetActiveScene().name;
                GUILayout.Window(400, windowRect, (id) =>
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label(HelpText);
                    GUILayout.EndVertical();
                    if (GUILayout.Button("Got it!"))
                        Displayed = false;
                }, title);
            }
            else
            {
                var pos = Position * new Vector2(Screen.width, Screen.height);
                if (GUI.Button(new Rect(pos, m_Size), ButtonText))
                    Displayed = true;
            }
        }
    }

    [Serializable]
    public struct CinemachineInformations
    {
        [SerializeField] GameObject m_Target;
        [SerializeField] string m_TargetComponent;
        [SerializeField] string m_Feature;
        [SerializeField] string m_Description;
        [SerializeField] AssemblySearch m_AssemblySearch;

        public GameObject target
        {
            get => m_Target;
        }

        public string targetComponent
        {
            get => m_TargetComponent;
        }

        public string feature
        {
            get => m_Feature;
        }

        public string description
        {
            get => m_Description;
        }

        public Assembly assembly
        {
            get
            {
                if (m_AssemblySearch == AssemblySearch.Cinemachine)
                    return Assembly.GetAssembly(typeof(CmCamera));;
                
                return Assembly.GetAssembly(typeof(Camera));;
            }
        }

        public string assemblyName
        {
            get
            {
                if (m_AssemblySearch == AssemblySearch.Cinemachine)
                    return "Cinemachine";
                
                return "UnityEngine";
            }
        }

        public enum AssemblySearch
        {
            Cinemachine,
            UnityEngine
        }
    }
}