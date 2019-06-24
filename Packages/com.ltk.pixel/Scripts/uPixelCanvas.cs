using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class uPixelCanvas : ScriptableObject
{
    public Vector2Int Size;
    public Palette Palette;
    public List<Frame> Frames;
    public Stack<UndoStep> UndoStack;
    public Stack<UndoStep> RedoStack;

    [System.Serializable]
    public class Frame
    {
        public int[] PaletteIndices;
    }

    [System.Serializable]
    public class UndoStep
    {

    }

    public void RandomisePixels()
    {
        if (Frames[0].PaletteIndices.Length != Size.x * Size.y)
        {
            Frames[0].PaletteIndices = new int[Size.x * Size.y];
        }
        for (int i = 0; i < Frames[0].PaletteIndices.Length; i++)
        {
            Frames[0].PaletteIndices[i] = Random.Range(0, Palette.Colors.Length);
        }
    }

    public void ImportPng(string importPath)
    {
        var importTexture = new Texture2D(2, 2);
        byte[] fileData = File.ReadAllBytes(importPath);
        importTexture.LoadImage(fileData);
        // Palette loads unique colors from the texture
        Palette.PopulateFromTexture(importTexture);
        // TODO for now clear the frame list down to one frame
        Frames = new List<Frame>() { new Frame() };
        var thisFrame = Frames[0];
        // Now walk the texture and get the palette index for each pixel
        var pixels = importTexture.GetPixels32();
        thisFrame.PaletteIndices = new int[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            thisFrame.PaletteIndices[i] = Palette.GetIndexOfColor(pixels[i]);
        }
    }

    public Texture2D ToTexture2D()
    {
        Texture2D t = new Texture2D(Size.x, Size.y);
        t.filterMode = FilterMode.Point;
        Color32[] colors = new Color32[Size.x * Size.y];
        for (int i = 0; i < Frames[0].PaletteIndices.Length; i++)
        {
            colors[i] = Palette.Colors[Frames[0].PaletteIndices[i]];
        }
        t.SetPixels32(colors);
        t.Apply();
        return t;
    }
}
