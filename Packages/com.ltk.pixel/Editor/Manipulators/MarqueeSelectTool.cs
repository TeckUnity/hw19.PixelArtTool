using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class MarqueeSelectTool : ToolBase
{
    private List<Rect> m_Selections = new List<Rect>();
    private Rect currentRect
    {
        get
        {
            return m_Selections[m_Selections.Count - 1];
        }
        set
        {
            m_Selections[m_Selections.Count - 1] = value;
        }
    }

    protected override void OnMouseDown(MouseDownEvent e)
    {
        if (m_Active)
        {
            return;
        }
        if (!e.shiftKey)
        {
            m_Selections.Clear();
            // uPixel.ClearOverlay();
            uPixel.ClearSelection();
        }
        m_ClickOrigin = GetImageCoord(Image, e.localMousePosition);
        m_Selections.Add(new Rect(m_ClickOrigin, Vector2Int.zero));
        m_Active = true;
        target.CaptureMouse();
        // uPixel.DrawOverlay(m_ClickOrigin);
    }

    protected override void OnMouseMove(MouseMoveEvent e)
    {
        var imageCoord = GetImageCoord(Image, e.localMousePosition);
        if (!m_Active || !target.HasMouseCapture())
        {
            m_ClickOrigin = imageCoord;
            return;
        }
        uPixel.ClearOverlay();
        var min = Vector2Int.Min(m_ClickOrigin, imageCoord);
        var max = Vector2Int.Max(m_ClickOrigin, imageCoord);
        currentRect = new Rect(min, max - min);
        var coords = new List<Vector2Int>();
        foreach (var rect in m_Selections)
        {
            for (int y = 0; y <= rect.height; y++)
            {
                for (int x = 0; x <= rect.width; x++)
                {
                    coords.Add(new Vector2Int((int)rect.min.x, (int)rect.min.y) + new Vector2Int(x, y));
                    // uPixel.DrawOverlay(new Vector2Int((int)rect.min.x, (int)rect.min.y) + new Vector2Int(x, y));
                }
            }
        }
        uPixel.SetSelection(coords.ToArray());
    }

    protected override void OnMouseUp(MouseUpEvent e)
    {
        m_Active = false;
        target.ReleaseMouse();
    }
}
