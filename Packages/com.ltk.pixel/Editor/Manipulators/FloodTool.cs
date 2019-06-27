using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class FloodTool : ToolBase
{
    protected override void OnMouseDown(UnityEngine.UIElements.MouseDownEvent e)
    {
        m_Active = true;
        target.CaptureMouse();
        m_ClickOrigin = GetImageCoord(Image, e.localMousePosition);
        Vector2Int[] coords = Flood(m_ClickOrigin);
        uPixel.DrawBuffer(coords);
    }

    protected override void OnMouseMove(UnityEngine.UIElements.MouseMoveEvent e)
    {
        Vector2Int imageCoord = GetImageCoord(Image, e.localMousePosition);
        uPixel.ClearBuffer();
        uPixel.DrawBuffer(imageCoord);
        if (!m_Active || !target.HasMouseCapture())
        {
            return;
        }
        Vector2Int[] coords = Flood(imageCoord);
        uPixel.DrawBuffer(coords);
    }

    private Vector2Int[] Flood(Vector2Int coord, float tolerance = 0)
    {
        int colIndex = uPixel.GetPaletteIndex(coord);
        List<Vector2Int> processed = new List<Vector2Int>();
        FloodStep(coord, colIndex, tolerance, ref processed);
        return processed.ToArray();
    }

    private void FloodStep(Vector2Int coord, int colIndex, float tolerance, ref List<Vector2Int> processed)
    {
        if (coord.x < 0 || coord.x > uPixel.pixelAsset.Size.x - 1 || coord.y < 0 || coord.y > uPixel.pixelAsset.Size.y - 1)
        {
            return;
        }
        if (processed.Contains(coord))
        {
            return;
        }
        if (Mathf.Abs(uPixel.GetPaletteIndex(coord) - colIndex) > tolerance)
        {
            return;
        }
        // uPixel.DrawBuffer(coord);
        processed.Add(coord);
        FloodStep(coord + Vector2Int.right, colIndex, tolerance, ref processed);
        FloodStep(coord + Vector2Int.left, colIndex, tolerance, ref processed);
        FloodStep(coord + Vector2Int.up, colIndex, tolerance, ref processed);
        FloodStep(coord + Vector2Int.down, colIndex, tolerance, ref processed);
    }
}
