using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyedropperTool : ToolBase
{
    protected override void OnMouseDown(UnityEngine.UIElements.MouseDownEvent e)
    {
        m_ClickOrigin = GetImageCoord(Image, e.localMousePosition);
        uPixel.SetPaletteIndex(uPixel.GetPaletteIndex(m_ClickOrigin));
    }

    protected override void OnMouseMove(UnityEngine.UIElements.MouseMoveEvent e)
    {
    }

    protected override void OnMouseUp(UnityEngine.UIElements.MouseUpEvent e)
    {
    }

    protected override void OnMouseWheel(UnityEngine.UIElements.WheelEvent e)
    {
    }
}
