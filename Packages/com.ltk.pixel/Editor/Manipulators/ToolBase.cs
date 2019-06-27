using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class ToolBase : MouseManipulator
{
    protected Vector2Int m_ClickOrigin;
    protected bool m_Active;
    protected Image Image
    {
        get
        {
            return target.Q<Image>();
        }
    }
    protected uPixel uPixel;
    protected int m_Size = 1;
    private uPixelCanvasOp pixelOp = new uPixelCanvasOp();

    public ToolBase()
    {
        activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse });
        activators.Add(new ManipulatorActivationFilter() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
        m_Active = false;
        uPixel = EditorWindow.GetWindow<uPixel>();
    }

    protected Vector2Int GetImageCoord(Image image, Vector2 position)
    {
        position = target.ChangeCoordinatesTo(Image, position);
        Vector2Int imageCoord = new Vector2Int((int)(position.x / Image.style.width.value.value * Image.image.width), (int)(position.y / Image.style.height.value.value * Image.image.height));
        imageCoord.y = (Image.image.height - 1) - imageCoord.y;
        return imageCoord;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseOverEvent>(OnMouseOver);
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<WheelEvent>(OnMouseWheel);
        uPixel.ClearBuffer();
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseOverEvent>(OnMouseOver);
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<WheelEvent>(OnMouseWheel);
        uPixel.ClearBuffer();
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
            m_Active = true;
            target.CaptureMouse();
            Vector2Int mouseCoord = GetImageCoord(Image, e.localMousePosition);
            Vector2Int[] coords = GetTargetCoords(mouseCoord);
            uPixel.DrawBuffer(coords);

            e.StopPropagation();
        }
    }

    protected virtual void OnMouseMove(MouseMoveEvent e)
    {
        Vector2Int mouseCoord = GetImageCoord(Image, e.localMousePosition);
        if (!m_Active || !target.HasMouseCapture())
        {
            uPixel.ClearBuffer();
        }
        Vector2Int[] coords = GetTargetCoords(mouseCoord);
        uPixel.DrawBuffer(coords);

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
        //uPixel.FlushBuffer();
        pixelOp.positions = uPixel.GetBrush();
        pixelOp.value = (byte)uPixel.paletteIndex;
        pixelOp.frame = uPixel.pixelAsset.FrameIndex;
        if (pixelOp.positions.Count > 0)
        {
            uPixel.pixelAsset.DoCanvasOperation(pixelOp);
            uPixel.m_HistoryCache.ClearCache(uPixel.pixelAsset.GetHistoryLength() - 1);
            pixelOp = new uPixelCanvasOp();
        }
        uPixel.RefreshInspector();
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
        Vector2Int mouseCoord = GetImageCoord(Image, e.localMousePosition);
        uPixel.ClearBuffer();
        Vector2Int[] coords = GetTargetCoords(mouseCoord);
        uPixel.DrawBuffer(coords);
        e.StopPropagation();
    }

    protected virtual Vector2Int[] GetTargetCoords(Vector2Int coord)
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