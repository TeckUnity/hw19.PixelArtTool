using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DrawTool : MouseManipulator
{
    private Vector2 m_ClickOrigin;
    protected bool m_Active;
    protected Image Image;
    protected uPixel uPixel;

    public DrawTool()
    {
        activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        m_Active = false;
        uPixel = EditorWindow.GetWindow<uPixel>();
    }

    protected override void RegisterCallbacksOnTarget()
    {
        Image = target.Q<Image>();
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
            Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
            Vector2Int mouseCoords = new Vector2Int((int)(mousePosition.x / Image.style.width.value.value * Image.image.width), (int)(mousePosition.y / Image.style.height.value.value * Image.image.height));
            mouseCoords.y = (Image.image.height - 1) - mouseCoords.y;
            uPixel.DrawBuffer(mouseCoords);
            e.StopPropagation();
        }
    }

    protected void OnMouseMove(MouseMoveEvent e)
    {
        Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
        Vector2Int mouseCoords = new Vector2Int((int)(mousePosition.x / Image.style.width.value.value * Image.image.width), (int)(mousePosition.y / Image.style.height.value.value * Image.image.height));
        mouseCoords.y = (Image.image.height - 1) - mouseCoords.y;
        if (!m_Active || !target.HasMouseCapture())
        {
            uPixel.ClearBuffer();
        }
        uPixel.DrawBuffer(mouseCoords);
        if (!m_Active || !target.HasMouseCapture())
        {
            e.StopPropagation();
        }
    }

    protected void OnMouseUp(MouseUpEvent e)
    {
        if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
        {
            return;
        }
        m_Active = false;
        uPixel.FlushBuffer();
        target.ReleaseMouse();
        e.StopPropagation();
    }

    protected void OnMouseWheel(WheelEvent e)
    {
        uPixel.CyclePalette((int)Mathf.Sign(e.mouseDelta.y));
        Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
        Vector2Int mouseCoords = new Vector2Int((int)(mousePosition.x / Image.style.width.value.value * Image.image.width), (int)(mousePosition.y / Image.style.height.value.value * Image.image.height));
        mouseCoords.y = (Image.image.height - 1) - mouseCoords.y;
        uPixel.ClearBuffer();
        uPixel.DrawBuffer(mouseCoords);
        e.StopPropagation();
    }
}