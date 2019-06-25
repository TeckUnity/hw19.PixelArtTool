using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class ToolBase : MouseManipulator
{
    private Vector2 m_ClickOrigin;
    protected bool m_Active;
    protected Image Image;
    protected uPixel uPixel;
    protected int m_Size = 1;

    public ToolBase()
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

    protected virtual void OnMouseOver(MouseOverEvent e)
    {
    }

    protected virtual void OnMouseDown(MouseDownEvent e)
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
            Vector2Int[] coords = GetPixelCoords(mouseCoords);
            foreach (var coord in coords)
            {
                uPixel.DrawBuffer(coord);
            }
            e.StopPropagation();
        }
    }

    protected virtual void OnMouseMove(MouseMoveEvent e)
    {
        Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
        Vector2Int mouseCoords = new Vector2Int((int)(mousePosition.x / Image.style.width.value.value * Image.image.width), (int)(mousePosition.y / Image.style.height.value.value * Image.image.height));
        mouseCoords.y = (Image.image.height - 1) - mouseCoords.y;
        if (!m_Active || !target.HasMouseCapture())
        {
            uPixel.ClearBuffer();
        }
        Vector2Int[] coords = GetPixelCoords(mouseCoords);
        foreach (var coord in coords)
        {
            uPixel.DrawBuffer(coord);
        }
        if (!m_Active || !target.HasMouseCapture())
        {
            e.StopPropagation();
        }
    }

    protected virtual void OnMouseUp(MouseUpEvent e)
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

    protected virtual void OnMouseWheel(WheelEvent e)
    {
        if (e.shiftKey)
        {
            uPixel.CyclePalette((int)Mathf.Sign(-e.mouseDelta.y));
        }
        else
        {
            m_Size += (int)Mathf.Sign(-e.mouseDelta.y);
            m_Size = Mathf.Max(1, m_Size);
        }
        Vector2 mousePosition = target.ChangeCoordinatesTo(Image, e.localMousePosition);
        Vector2Int mouseCoords = new Vector2Int((int)(mousePosition.x / Image.style.width.value.value * Image.image.width), (int)(mousePosition.y / Image.style.height.value.value * Image.image.height));
        mouseCoords.y = (Image.image.height - 1) - mouseCoords.y;
        uPixel.ClearBuffer();
        Vector2Int[] coords = GetPixelCoords(mouseCoords);
        foreach (var coord in coords)
        {
            uPixel.DrawBuffer(coord);
        }
        e.StopPropagation();
    }

    protected virtual Vector2Int[] GetPixelCoords(Vector2Int coord)
    {
        var coords = new List<Vector2Int>();
        float radius = (float)m_Size / 2f;
        Vector2 v;
        for (int y = -Mathf.CeilToInt(radius); y < Mathf.CeilToInt(radius); y++)
        {
            for (int x = -Mathf.CeilToInt(radius); x < Mathf.CeilToInt(radius); x++)
            {
                v = new Vector2(x, y);
                float r = Mathf.Repeat(radius + 0.5f, 1);
                if ((v + Vector2.one * (r)).sqrMagnitude > radius * radius)
                {
                    continue;
                }
                coords.Add(new Vector2Int((int)v.x, (int)v.y) + coord);
            }
        }
        return coords.ToArray();
    }
}