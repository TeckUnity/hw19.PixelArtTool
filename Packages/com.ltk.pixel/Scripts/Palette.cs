using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class uPixelColor32Extensions // TODO put this in some utils file
{
    public static bool IsEqualTo(this Color32 self, Color32 other)
    {
        return self.r == other.r && self.g == other.g && self.b == other.b && self.a == other.a;
    }
}

public class Color32Comparer : IEqualityComparer<Color32> // TODO put this in some utils file
{

    public bool Equals(Color32 first, Color32 second)
    {
        return first.IsEqualTo(second);
    }

    public int GetHashCode(Color32 col)
    {
        int hash = 17;
        hash = (hash ^ col.r) * 16777619;
        hash = (hash ^ col.g) * 16777619;
        hash = (hash ^ col.b) * 16777619;
        hash = (hash ^ col.a) * 16777619;
        return hash;
    }
}

public class Palette : ScriptableObject
{
    public Color32[] Colors;
    private Dictionary<Color32, int> m_ColorIndexDict; // just optimisation to get index for color quickly. Must be kept aligned with Colors

    private void OnEnable()
    {
        if (m_ColorIndexDict == null)
        {
            PopulateColorIndexDict();
        }
    }

    private void PopulateColorIndexDict()
    {
        m_ColorIndexDict = new Dictionary<Color32, int>(new Color32Comparer());
        for (int i = 0; i < Colors.Length; i++)
        {
            m_ColorIndexDict[Colors[i]] = i;
        }
    }

    public void PopulateFromTexture(Texture2D tex)
    {
        // TODO this crudely just imports every unique color - should give user control over paletisation
        HashSet<Color32> hashSet = new HashSet<Color32>(new Color32Comparer());
        var pixels = tex.GetPixels32();
        // Generate set of unique colors from the texture:
        foreach (var col in pixels)
        {
            if (!hashSet.Contains(col))
            {
                hashSet.Add(col);
            }
        }
        // Populate member array - this will be in arbitrary order TODO add some ordering / sorting functions for Palette
        this.Colors = new Color32[hashSet.Count];
        hashSet.CopyTo(Colors);
        PopulateColorIndexDict();
    }

    public int GetIndexOfColor(Color32 color)
    {
        // TODO what if we can't find the color?
        return m_ColorIndexDict[color];
    }
}
