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

    public void ImportPng(string importPath)
    {
        var importTexture = new Texture2D(2, 2);
        byte[] fileData = File.ReadAllBytes(importPath);
        importTexture.LoadImage(fileData);
        // Palette loads unique colors from the texture
        Palette.PopulateFromTexture(importTexture);
        // TODO for now clear the frame list down to one frame
        Frames = new List<Frame>() {new Frame()};
        var thisFrame = Frames[0];
        // Now walk the texture and get the palette index for each pixel
        var pixels = importTexture.GetPixels32();
        thisFrame.PaletteIndices = new int[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            thisFrame.PaletteIndices[i] = Palette.GetIndexOfColor(pixels[i]);
        }
    }
}
