using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class uPixelCanvasOpBase
{
    public abstract void Execute(uPixelCanvas canvas);

    public bool includesSnapshot;
}

public class ChangePixel : uPixelCanvasOpBase
{
    public int frame;
    public byte value;
    public Vector2Int position;

    public override void Execute(uPixelCanvas canvas)
    {
        int index = (position.y * canvas.Size.x) + position.x;
        canvas.Frames[frame].PaletteIndices[index] = value;
    }
}

[CreateAssetMenu]
public class uPixelCanvas : ScriptableObject
{

    private static readonly Vector2Int DEFAULT_SIZE = new Vector2Int(64, 64);

    public Vector2Int Size;
    public Palette Palette;
    public List<Frame> Frames;
    public int FrameIndex;
    public int CanvasOpsTip;
    public List<uPixelCanvasOpBase> CanvasOps;
    // Can these be removed?
    //public Stack<UndoStep> UndoStack;
    //public Stack<UndoStep> RedoStack;

    [System.Serializable]
    public class Frame
    {
        private uPixelCanvas m_parentCanvas;
        public int[] PaletteIndices;

        public Frame(uPixelCanvas parentCanvas)
        {
            m_parentCanvas = parentCanvas;
            PaletteIndices = new int[m_parentCanvas.Size.x * m_parentCanvas.Size.y];
        }

    }

    //[System.Serializable]
    //public class UndoStep
    //{
    //
    //}

    public uPixelCanvas()
    {
        Size = DEFAULT_SIZE;
        ResetFrames();
    }

    public Frame GetCurrentFrame()
    {
        return Frames[FrameIndex];
    }

    public void ResetFrames()
    {
        Frames = new List<Frame>() { new Frame(this) };
    }

    public void RandomisePixels()
    {
        if (GetCurrentFrame().PaletteIndices.Length != Size.x * Size.y)
        {
            GetCurrentFrame().PaletteIndices = new int[Size.x * Size.y];
        }
        for (int i = 0; i < GetCurrentFrame().PaletteIndices.Length; i++)
        {
            GetCurrentFrame().PaletteIndices[i] = Random.Range(0, Palette.Colors.Length);
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
        Frames = new List<Frame>() { new Frame(this) };
        var thisFrame = GetCurrentFrame();
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
        // Sanity check we have at least one frame and a palette
        if (Frames.Count > 0 && Palette != null)
        {
            Color32[] colors = t.GetPixels32();
            for (int i = 0; i < GetCurrentFrame().PaletteIndices.Length; i++)
            {
                colors[i] = Palette.Colors[GetCurrentFrame().PaletteIndices[i]];
            }

            t.SetPixels32(colors);
            t.Apply();
        }
        return t;
    }

    private void ExecuteCanvasOps(int startIndex, int endIndex)
    {
        // TODO: Jump to newest SnapShot/Keyframe
        int OpCount = CanvasOps.Count;
        for (int i = startIndex; i < OpCount && i < endIndex; ++i)
        {
            CanvasOps[i].Execute(this);
        }
    }

    public void DoCanvasOperation(uPixelCanvasOpBase op)
    {
        // if a Redo history exists, calling this destroys this
        if (CanvasOps.Count > CanvasOpsTip)
        {
            CanvasOps.RemoveRange(CanvasOpsTip, CanvasOps.Count - CanvasOpsTip);
        }
        CanvasOps.Add(op);
        ExecuteCanvasOps(CanvasOpsTip, CanvasOpsTip+1);
        CanvasOpsTip += 1;
    }

    public void UndoCanvasOperations(int count)
    {
        //Clear the data
        ResetFrames();
        // Re-run frames to return to the old state
        CanvasOpsTip -= count;
        // Don't remove the CanvasOps just yet. Wait until a new op is run
        ExecuteCanvasOps(0, CanvasOpsTip);
    }

    public void RedoCanvasOperations(int count)
    {
        if (CanvasOps.Count == CanvasOpsTip)
        {
            // Nothing left to do.
            return;
        }
        ExecuteCanvasOps(CanvasOpsTip, CanvasOpsTip + count);
        CanvasOpsTip += count;
    }
}
