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

public struct ArraySlice
{
    public int start;
    public int count;
};

public class Palette : ScriptableObject
{
    public Color32[] ReducedColourSet;
    public Color32[] Colors;
    private Dictionary<Color32, int> m_ColorIndexDict; // just optimisation to get index for color quickly. Must be kept aligned with Colors

    private void OnEnable()
    {
        if (Colors == null)
        {
            return;
        }
        if (m_ColorIndexDict == null)
        {
            PopulateColorIndexDict();
        }
    }

    public void Init()
    {
        PopulateColorIndexDict();
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

    public void CalculateReducedPaletteSet(int maxColors)
    {
        int maxColorsPow2 = Mathf.FloorToInt(Mathf.Pow(2, Mathf.Ceil(Mathf.Log(maxColors, 2))));
        int buckets = 1;

        // Create an array to be split
        Color32[] colorSet = new Color32[Colors.Length];
        ArraySlice[] splits = new ArraySlice[maxColorsPow2];
        splits[0].start = 0;
        splits[0].count = Colors.Length;
        Array.Copy(Colors, colorSet, Colors.Length);
        while (buckets < maxColorsPow2)
        {
            for (int split_idx = 0; split_idx < buckets; ++split_idx)
            {
                ArraySlice split = splits[split_idx];
                int split_end = split.start + split.count;
                // Determine the largest range in a color channel
                int rMin = int.MaxValue, rMax = int.MinValue;
                int gMin = int.MaxValue, gMax = int.MinValue;
                int bMin = int.MaxValue, bMax = int.MinValue;
                for (int i = split.start; i < split_end; ++i)
                {
                    Color32 col = colorSet[i];
                    rMin = Math.Min(col.r, rMin);
                    gMin = Math.Min(col.g, gMin);
                    bMin = Math.Min(col.b, bMin);
                    rMax = Math.Max(col.r, rMax);
                    gMax = Math.Max(col.g, gMax);
                    bMax = Math.Max(col.b, bMax);
                }

                int rRange = rMax - rMin;
                int gRange = gMax - gMin;
                int bRange = bMax - bMin;

                Comparer<Color32> compare = Comparer < Color32 > .Create(new Comparison<Color32>((lhs, rhs) => lhs.b - rhs.b));

                if (rRange > gRange)
                {
                    if (rRange > bRange)
                        compare = Comparer<Color32>.Create(new Comparison<Color32>((lhs, rhs) => lhs.r - rhs.r));
                }
                else if (gRange > bRange)
                {
                    compare = Comparer<Color32>.Create(new Comparison<Color32>((lhs, rhs) => lhs.g - rhs.g));
                }

                // sort and split
                Array.Sort<Color32>(colorSet, split.start, split.count, compare);
            }

            buckets = buckets << 1;
            int bucketSize = Mathf.RoundToInt(Colors.Length / buckets);
            for (int i = 0; i < buckets; ++i)
            {
                splits[i].start = i * bucketSize;
                splits[i].count = bucketSize;
                if (splits[i].start+bucketSize > Colors.Length)
                {
                    splits[i].count = Colors.Length - splits[i].start;
                }
            }
        }

        // splits will now have an array of segments that can be averaged to get our new palette
        ReducedColourSet = new Color32[maxColorsPow2];
        for (int i = 0; i < maxColorsPow2; ++i)
        {
            ArraySlice s = splits[i];
            int s_end = s.start + s.count;
            int r = colorSet[s.start].r;
            int g = colorSet[s.start].g;
            int b = colorSet[s.start].b;
            for (int c = s.start+1; c < s_end; ++c)
            {
                r += colorSet[c].r;
                g += colorSet[c].g;
                b += colorSet[c].b;
            }

            ReducedColourSet[i] = new Color32((byte)(r / s.count & 0xFF), (byte)(g / s.count & 0xFF), (byte)(b / s.count & 0xFF), 255);
        }
    }

    public int GetIndexOfColor(Color32 color)
    {
        // TODO what if we can't find the color?
        return m_ColorIndexDict[color];
    }
}
