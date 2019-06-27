using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class LineTool : ToolBase
{
    protected virtual bool IsContinuous()
    {
        return false;
    }

    protected override void OnMouseDown(UnityEngine.UIElements.MouseDownEvent e)
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
            m_ClickOrigin = GetImageCoord(Image, e.localMousePosition);
            Vector2Int[] coords = GetTargetCoords(m_ClickOrigin);
            uPixel.DrawBuffer(coords);
        }
        e.StopPropagation();
    }

    protected override void OnMouseMove(MouseMoveEvent e)
    {
        Vector2Int mouseCoord = GetImageCoord(Image, e.localMousePosition);
        if (IsContinuous())
        {
            if (!m_Active || !target.HasMouseCapture())
            {
                uPixel.ClearBuffer();
                m_ClickOrigin = mouseCoord;
            }
        }
        else
        {
            uPixel.ClearBuffer();
            if (!m_Active || !target.HasMouseCapture())
            {
                m_ClickOrigin = mouseCoord;
            }
        }
        Vector2Int[] coords = Line(m_ClickOrigin, mouseCoord);
        uPixel.DrawBuffer(coords);
        if (IsContinuous())
        {
            m_ClickOrigin = mouseCoord;
        }
    }

    protected Vector2Int[] Line(Vector2Int p0, Vector2Int p1)
    {
        int dx = Mathf.Abs(p1.x - p0.x);
        int dy = Mathf.Abs(p1.y - p0.y);
        int sx = p0.x < p1.x ? 1 : -1;
        int sy = p0.y < p1.y ? 1 : -1;
        int x0 = p0.x;
        int y0 = p0.y;
        float err = dx - dy;
        int y = p0.y;
        List<Vector2Int> coords = new List<Vector2Int>();
        coords.AddRange(GetTargetCoords(new Vector2Int(p0.x, p0.y)));
        coords.AddRange(GetTargetCoords(new Vector2Int(p1.x, p1.y)));
        for (int i = 0; i < Image.image.width * Image.image.height; i++)
        {
            if (p0.x == p1.x && p0.y == p1.y)
            {
                break;
            }
            float e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                p0.x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                p0.y += sy;
                p0.y = Mathf.Clamp(p0.y, -Image.image.height, Image.image.height);
            }
            int idx = p0.y * Image.image.width + p0.x;
            coords.AddRange(GetTargetCoords(new Vector2Int(p0.x, p0.y)));
        }
        return coords.ToArray();
    }
}
