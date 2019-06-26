using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static float Luminance(this Color32 c)
    {
        Color col = c;
        return col.r * 0.299f + col.g * 0.587f + col.b * 0.114f;
    }
}
