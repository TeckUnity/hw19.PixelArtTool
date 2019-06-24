using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class CanvasManipulator : MouseManipulator
{
    private Vector2 m_ClickOrigin;
    protected bool m_Active;
    protected Image Image;

    public CanvasManipulator()
    {
        activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        m_Active = false;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseOverEvent>(OnMouseOver);
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<WheelEvent>(OnMouseWheel);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseOverEvent>(OnMouseOver);
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<WheelEvent>(OnMouseWheel);
    }

    protected void OnMouseOver(MouseOverEvent e)
    {

    }

    protected void OnMouseDown(MouseDownEvent e)
    {
        if (m_Active)
        {
            e.StopImmediatePropagation();
            return;
        }
        if (CanStartManipulation(e))
        {
            Image = target.Q<Image>();
            m_ClickOrigin = target.ChangeCoordinatesTo(Image, e.localMousePosition);
            m_Active = true;
            target.CaptureMouse();
            e.StopPropagation();
        }
    }

    protected void OnMouseMove(MouseMoveEvent e)
    {
        if (!m_Active || !target.HasMouseCapture())
        {
            return;
        }
        Vector2 delta = target.ChangeCoordinatesTo(Image, e.localMousePosition) - m_ClickOrigin;
        Image.style.left = Image.layout.xMin + delta.x;
        Image.style.top = Image.layout.yMin + delta.y;
        e.StopPropagation();
    }

    protected void OnMouseUp(MouseUpEvent e)
    {
        if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
        {
            return;
        }
        m_Active = false;
        target.ReleaseMouse();
        e.StopPropagation();
    }

    protected void OnMouseWheel(WheelEvent e)
    {
        if (Image == null)
        {
            Image = target.Q<Image>();
        }
        Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
        float wheelDelta = -Mathf.Ceil(Mathf.Abs(e.mouseDelta.y)) * Mathf.Sign(e.mouseDelta.y);
        float aspect = Image.image.width / Image.image.height;
        Vector2 oldSize = Image.layout.size;
        Vector2 newSize = new Vector2(wheelDelta + Image.layout.width, wheelDelta * aspect + Image.layout.height);
        if (newSize.x < Image.image.width || newSize.y < Image.image.height)
        {
            return;
        }
        Image.style.width = newSize.x;
        Image.style.height = newSize.y;
        Vector2 deltaSize = newSize - oldSize;
        deltaSize *= mousePosition / newSize;
        Image.style.left = Image.layout.xMin - deltaSize.x;
        Image.style.top = Image.layout.yMin - deltaSize.y;
    }
}