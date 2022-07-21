#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_UNITY_IMGUI
#endif

using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Cinemachine.Utility
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    public class CinemachineDebug
    {
        static HashSet<Object> s_Clients;
        static List<StringBuilder> m_AvailableStringBuilders;

#if CINEMACHINE_UNITY_IMGUI
        /// <summary>Release a screen rectangle previously obtained through GetScreenPos()</summary>
        /// <param name="client">The client caller.  Used as a handle.</param>
        public static void ReleaseScreenPos(Object client)
        {
            if (s_Clients != null && s_Clients.Contains(client))
                s_Clients.Remove(client);
        }
        
        /// <summary>Reserve an on-screen rectangle for debugging output.</summary>
        /// <param name="client">The client caller.  This is used as a handle.</param>
        /// <param name="text">Sample text, for determining rectangle size</param>
        /// <param name="style">What style will be used to draw, used here for
        /// determining rect size</param>
        /// <returns>An area on the game screen large enough to print the text
        /// in the style indicated</returns>
        public static Rect GetScreenPos(Object client, string text, GUIStyle style)
        {
            if (s_Clients == null)
                s_Clients = new ();
            if (!s_Clients.Contains(client))
                s_Clients.Add(client);

            var pos = Vector2.zero;
            var size = style.CalcSize(new GUIContent(text));
            if (s_Clients != null)
            {
                foreach (var c in s_Clients)
                {
                    if (c == client)
                        break;
                    pos.y += size.y;
                }
            }
            return new Rect(pos, size);
        }
#endif

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public delegate void OnGUIDelegate();

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public static OnGUIDelegate OnGUIHandlers;

        /// <summary>Get a preallocated StringBuilder from the pool</summary>
        /// <returns>The preallocated StringBuilder from the pool.  
        /// Client must call ReturnToPool when done</returns>
        public static StringBuilder SBFromPool()
        {
            if (m_AvailableStringBuilders == null || m_AvailableStringBuilders.Count == 0)
                return new StringBuilder();
            var sb = m_AvailableStringBuilders[m_AvailableStringBuilders.Count - 1];
            m_AvailableStringBuilders.RemoveAt(m_AvailableStringBuilders.Count - 1);
            sb.Length = 0;
            return sb;
        }

        /// <summary>Return a StringBuilder to the preallocated pool</summary>
        /// <param name="sb">The string builder object to return to the pool</param>
        public static void ReturnToPool(StringBuilder sb)
        {
            if (m_AvailableStringBuilders == null)
                m_AvailableStringBuilders = new List<StringBuilder>();
            m_AvailableStringBuilders.Add(sb);
        }
    }
}
