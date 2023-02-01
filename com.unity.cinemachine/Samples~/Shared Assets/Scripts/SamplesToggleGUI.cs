using UnityEngine;
using UnityEngine.Events;

public class SamplesToggleGUI : MonoBehaviour
{
    [Tooltip("Screen position of the button in normalized screen coords")]
    public Vector2 Position = new Vector3(0.01f, 0.01f);

    [Tooltip("Text displayed on button")]
    public string Text = "";
    
    [Tooltip("Value of the toggle")]
    public bool ToggleValue = true;
    
    public UnityEvent<bool> OnValueChanged = new UnityEvent<bool>();
    
    Vector2 m_Size = Vector2.zero;
    void OnGUI()
    {
        // Establish button size (only once)
        if (m_Size == Vector2.zero)
        {
            var s = GUI.skin.box.CalcSize(new GUIContent(Text));
            m_Size.x = Mathf.Max(m_Size.x, s.x + s.y);
            m_Size.y = Mathf.Max(m_Size.y, s.y);
        }

        // Draw buttons
        const float vSpace = 3.0f;
        var pos = Position * new Vector2(Screen.width, Screen.height);
        
        var r = new Rect(pos, m_Size);
        GUI.Label(r, "", GUI.skin.box); // background
        var prevToggleValue = ToggleValue;
        ToggleValue = GUI.Toggle(r, ToggleValue, Text);
        if (prevToggleValue != ToggleValue)
            OnValueChanged.Invoke(ToggleValue);
        pos.y += m_Size.y + vSpace;
    }
}
