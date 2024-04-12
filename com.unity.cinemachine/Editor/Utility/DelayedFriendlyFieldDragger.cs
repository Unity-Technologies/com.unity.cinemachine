using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Provides dragging on a visual element to change a value field with 
    /// isDelayed set, but for int and float driven fields turns off isDelayed while dragging.
    /// </summary>
    class DelayedFriendlyFieldDragger<T> : BaseFieldMouseDragger
    {
        /// <summary>
        /// FieldMouseDraggerForDelayed's constructor.
        /// </summary>
        /// <param name="drivenField">The field.</param>
        public DelayedFriendlyFieldDragger(IValueField<T> drivenField)
        {
            m_DrivenField = drivenField;
            m_DragElement = null;
            m_DragHotZone = new Rect(0, 0, -1, -1);
            dragging = false;
        }

        private readonly IValueField<T> m_DrivenField;
        private VisualElement m_DragElement;
        private Rect m_DragHotZone;
        private bool m_WasDelayed;

        /// <summary>
        /// Is dragging.
        /// </summary>
        public bool dragging { get; set; }

        /// <summary>
        /// Start value before drag.
        /// </summary>
        public T startValue { get; set; }

        /// <inheritdoc />
        public sealed override void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<PointerDownEvent>(UpdateValueOnPointerDown, TrickleDown.TrickleDown);
                m_DragElement.UnregisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);
                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<PointerDownEvent>(UpdateValueOnPointerDown, TrickleDown.TrickleDown);
                m_DragElement.RegisterCallback<PointerUpEvent>(UpdateValueOnPointerUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }
        }

        private bool CanStartDrag(int button, Vector2 localPosition)
        {
            return button == 0 && (m_DragHotZone.width < 0 || m_DragHotZone.height < 0 ||
                m_DragHotZone.Contains(m_DragElement.WorldToLocal(localPosition)));
        }

        private void UpdateValueOnPointerDown(PointerDownEvent evt)
        {
            if (CanStartDrag(evt.button, evt.localPosition))
            {
                // We want to allow dragging when using a mouse in any context and when in an Editor context with any pointer type.
                if (evt.pointerType == PointerType.mouse)
                {
                    m_DragElement.CaptureMouse();
                    ProcessDownEvent(evt);
                }
                else if (m_DragElement.panel.contextType == ContextType.Editor)
                {
                    m_DragElement.CapturePointer(evt.pointerId);
                    ProcessDownEvent(evt);
                }
            }
        }

        private void ProcessDownEvent(EventBase evt)
        {
            // Make sure no other elements can capture the mouse!
            evt.StopPropagation();

            dragging = true;
            m_DragElement.RegisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
            startValue = m_DrivenField.value;

            if (m_DrivenField is TextInputBaseField<float> floatField)
            {
                m_WasDelayed = floatField.isDelayed;
                floatField.isDelayed = false;
            }
            if (m_DrivenField is TextInputBaseField<int> intField)
            {
                m_WasDelayed = intField.isDelayed;
                intField.isDelayed = false;
            }
            m_DrivenField.StartDragging();
            EditorGUIUtility.SetWantsMouseJumping(1);
        }

        private void UpdateValueOnPointerMove(PointerMoveEvent evt)
        {
            ProcessMoveEvent(evt.shiftKey, evt.altKey, evt.deltaPosition);
        }

        private void ProcessMoveEvent(bool shiftKey, bool altKey, Vector2 deltaPosition)
        {
            if (dragging)
            {
                DeltaSpeed s = shiftKey ? DeltaSpeed.Fast : (altKey ? DeltaSpeed.Slow : DeltaSpeed.Normal);
                m_DrivenField.ApplyInputDeviceDelta(deltaPosition, s, startValue);
            }
        }

        private void UpdateValueOnPointerUp(PointerUpEvent evt)
        {
            ProcessUpEvent(evt, evt.pointerId);
        }

        private void ProcessUpEvent(EventBase evt, int pointerId)
        {
            if (dragging)
            {
                dragging = false;
                m_DragElement.UnregisterCallback<PointerMoveEvent>(UpdateValueOnPointerMove);
                m_DragElement.ReleasePointer(pointerId);
                //if (evt is IMouseEvent)
                //    m_DragElement.panel.ProcessPointerCapture(PointerId.mousePointerId);

                EditorGUIUtility.SetWantsMouseJumping(0);
                m_DrivenField.StopDragging();

                if (m_DrivenField is TextInputBaseField<float> floatField)
                    floatField.isDelayed = m_WasDelayed;
                if (m_DrivenField is TextInputBaseField<int> intField)
                    intField.isDelayed = m_WasDelayed;
            }
        }

        private void UpdateValueOnKeyDown(KeyDownEvent evt)
        {
            if (dragging && evt.keyCode == KeyCode.Escape)
            {
                dragging = false;
                m_DrivenField.value = startValue;
                m_DrivenField.StopDragging();
                var target = evt.target as VisualElement;
                IPanel panel = target?.panel;
                panel?.ReleasePointer(PointerId.mousePointerId);
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
        }
    }
}
