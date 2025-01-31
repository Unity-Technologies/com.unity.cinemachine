#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using System;
using UnityEditor.EditorTools;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// This was a base class for Cinemachine tools, but is not longer used.
    /// </summary>
    [Obsolete("This class is no longer used.")]
    public abstract class CinemachineTool : EditorTool, IDrawSelectedHandles
    {
        /// <summary>Implement this to set your Tool's icon and tooltip.</summary>
        /// <returns>A GUIContent with an icon set.</returns>
        protected abstract GUIContent GetIcon();

        /// <summary>This lets the editor find the icon of the tool.</summary>
        public override GUIContent toolbarIcon => null;

        /// <summary>This is called when the Tool is selected in the editor.</summary>
        public override void OnActivated() => base.OnActivated();

        /// <summary>This is called when the Tool is deselected in the editor.</summary>
        public override void OnWillBeDeactivated() => base.OnWillBeDeactivated();

        /// <summary>Implement IDrawSelectedHandles to draw gizmos for this tool even if it is not the active tool.</summary>
        public void OnDrawHandles() {}

        /// <summary>Get the path to the tool's icon asset.</summary>
        /// <returns>The path to the icon asset.</returns>
        private protected string GetIconPath() => string.Empty;
    }
}
#endif
