using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class uPixelCanvasOp
{
    public bool includesSnapshot;
    public int frame = 0;
    public byte value;
    public List<int> positions;

    public void Execute(uPixelCanvas canvas)
    {
        foreach (var p in positions)
        {
            canvas.GetFrame(frame).PaletteIndices[p] = value;
        }
    }
}


[CreateAssetMenu]
public class uPixelCanvas : ScriptableObject
{

    private static readonly Vector2Int DEFAULT_SIZE = new Vector2Int(64, 64);

    public Vector2Int Size;
    public Palette Palette;
    public int FrameIndex;
    [SerializeField]
    private int CanvasOpsTip;
    private int ShadowCanvasOpTip;
    // [System.NonSerialized]
    public List<Frame> Frames;
    public List<uPixelCanvasOp> CanvasOps;

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

    public uPixelCanvas()
    {
        Size = DEFAULT_SIZE;
        CanvasOps = new List<uPixelCanvasOp>();
        CanvasOpsTip = 0;
        ShadowCanvasOpTip = 0;
        ResetFrames();
    }

    public void AddFrame(bool duplicate = false)
    {
        Frame newFrame = new Frame(this);
        if (duplicate)
        {
            GetCurrentFrame().PaletteIndices.CopyTo(newFrame.PaletteIndices, 0);
        }
        FrameIndex++;
        Frames.Insert(FrameIndex, newFrame);
    }

    public void Resize(Vector2Int newSize)
    {
        Vector2Int delta = newSize - Size;
        int leftPad = delta.x / 2;
        int rightPad = delta.x - leftPad;
        int topPad = delta.y / 2;
        int bottomPad = delta.y - topPad;
        for (int f = 0; f < Frames.Count; f++)
        {
            int[] newIndices = new int[newSize.x * newSize.y];
            Debug.Log(newIndices.Length);
            for (int y = 0; y < newSize.y; y++)
            {
                for (int x = 0; x < newSize.x; x++)
                {
                    int oldY = y - bottomPad;
                    int oldX = x - leftPad;
                    int idx = oldY * Size.x + oldX;
                    newIndices[y * newSize.x + x] = oldY < 0 || oldY >= Size.y || oldX < 0 || oldX >= Size.x ? 0 : Frames[f].PaletteIndices[idx];
                }
            }
            Frames[f].PaletteIndices = newIndices;
        }
        Size = newSize;
    }

    public Frame GetCurrentFrame()
    {
        return Frames[FrameIndex];
    }

    public Frame GetFrame(int index)
    {
        return Frames[index];
    }

    public void ResetFrames()
    {
        Frames = new List<Frame>() { new Frame(this) };
    }

    public int GetHistoryLength()
    {
        return CanvasOpsTip;
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

    public Texture2D GetTextureAtTime(int operation)
    {
        ResetFrames();
        ExecuteCanvasOps(0, operation);
        Texture2D tex = ToTexture2D();
        ExecuteCanvasOps(operation, CanvasOpsTip);
        return tex;
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

    public void DoCanvasOperation(uPixelCanvasOp op)
    {
        // if a Redo history exists, calling this destroys this
        if (CanvasOps.Count > CanvasOpsTip)
        {
            CanvasOps.RemoveRange(CanvasOpsTip, CanvasOps.Count - CanvasOpsTip);
        }
        CanvasOps.Add(op);

        UnityEditor.Undo.RecordObject(this, string.Format("uPixelCanvas: {0}", op.GetType().ToString()));
        ExecuteCanvasOps(CanvasOpsTip, CanvasOpsTip + 1);
        CanvasOpsTip = CanvasOps.Count;
        ShadowCanvasOpTip = CanvasOpsTip;
    }

    public void RerunHistory()
    {
        ResetFrames();
        ExecuteCanvasOps(0, CanvasOpsTip);
        ShadowCanvasOpTip = CanvasOpsTip;
    }

    public bool CheckUndoRedo()
    {
        if (ShadowCanvasOpTip != CanvasOpsTip)
        {
            ResetFrames();
            ExecuteCanvasOps(0, CanvasOpsTip);
            if (CanvasOpsTip > CanvasOps.Count)
            {
                CanvasOpsTip = CanvasOps.Count;
            }
            ShadowCanvasOpTip = CanvasOpsTip;
            return true;
        }
        return false;
    }
}
