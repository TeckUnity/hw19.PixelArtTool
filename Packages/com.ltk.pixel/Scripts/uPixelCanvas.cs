using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

[System.Serializable]
public class uPixelCanvasOp
{
    public enum OpType
    {
        PixelSet,
        Resize,
        AddFrame,
        RemoveFrame,
        PaletteSet,
        BlitOp,
    };

    [System.Serializable]
    public class ResizeData
    {
        public Vector2Int size;
    }

    [System.Serializable]
    public class Group
    {
        public byte value = 0;
        public List<int> positions = null;
    }

    [System.Serializable]
    public class GroupData
    {
        public List<Group> pixelSets = null;
    }

    [System.Serializable]
    public class BlitData
    {
        public Vector2Int offset;
        public Vector2Int size;
        public Color32[] pixels;
        public int frameIndex; // frame we blit to
    }

    public OpType type = OpType.PixelSet;
    public bool includesSnapshot;
    public bool duplicate;
    public int frame = 0;
    public byte value;
    public ResizeData resize = null;
    public List<int> positions = null;
    public GroupData group = null;
    public Palette palette = null;
    public BlitData blitData = null;

    public void Execute(uPixelCanvas canvas)
    {
        canvas.FrameIndex = frame;
        if (type == OpType.PixelSet)
        {
            foreach (var p in positions)
            {
                canvas.GetFrame(frame).PaletteIndices[p] = value;
            }
        }
        else if (type == OpType.AddFrame)
        {
            canvas.AddFrameInternal(duplicate);
        }
        else if (type == OpType.Resize)
        {
            canvas.ResizeInternal(resize.size);
        }
        else if (type == OpType.PaletteSet)
        {
            canvas.SetPaletteInternal(palette);
        }
        else if (type == OpType.BlitOp)
        {
            canvas.DoBlitInternal(blitData); // TODO not using size / offset yet
        }
    }
}


[CreateAssetMenu]
public class uPixelCanvas : ScriptableObject
{

    private class Keyframe
    {
        public Vector2Int Size;
        public int FrameIndex;
        public List<Frame> Frames;
    }

    // The plus & minus 1 used around KEYFRAME_RATE are because we don't need to have a key frame at frame 0 (because we know it's all zero)
    private static readonly int KEYFRAME_RATE = 64; // Generate keyframes every KEYFRAME_RATE frames
    private static readonly Vector2Int DEFAULT_SIZE = new Vector2Int(16, 16);

    public Vector2Int Size;
    public Palette Palette; // TODO make private
    public int FrameIndex;
    [SerializeField]
    private int CanvasOpsTip;
    private int ShadowCanvasOpTip;
    // [System.NonSerialized]
    public List<Frame> Frames;
    public List<uPixelCanvasOp> CanvasOps;
    [System.NonSerialized]
    private List<Keyframe> Keyframes;
    public bool FreezeFuture;

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

        public Frame(uPixelCanvas parentCanvas, Vector2Int overrideSize)
        {
            m_parentCanvas = parentCanvas;
            PaletteIndices = new int[overrideSize.x * overrideSize.y];
        }
    }

    public uPixelCanvas()
    {
        Size = DEFAULT_SIZE;
        CanvasOps = new List<uPixelCanvasOp>();
        CanvasOpsTip = 0;
        ShadowCanvasOpTip = 0;
        FreezeFuture = false;
        ResetFrames();
    }

    void GenerateKeyFrameNow()
    {
        Keyframe newkey = new Keyframe();
        newkey.Size = Size;
        newkey.FrameIndex = FrameIndex;
        newkey.Frames = new List<Frame>();
        foreach (var frame in Frames)
        {
            Frame newframe = new Frame(this, Size);
            newframe.PaletteIndices = frame.PaletteIndices.Clone() as int[];
            newkey.Frames.Add(frame);
        }
        Keyframes.Add(newkey);
    }

    void TrimKeyFrames()
    {
        int requiredcount = (CanvasOpsTip / KEYFRAME_RATE) - 1;
        if (requiredcount < 0)
        {
            Keyframes.RemoveRange(0, Keyframes.Count);
        }
        else if (Keyframes.Count > requiredcount)
        {
            Keyframes.RemoveRange(requiredcount, Keyframes.Count - requiredcount);
        }
    }

    int ApplyNearestKeyframe(int where)
    {
        if (Keyframes.Count == 0) return 0;

        int keyindex = (where / KEYFRAME_RATE) - 1;
        if (keyindex < 0) return 0;

        if (keyindex >= Keyframes.Count)
        {
            keyindex = Keyframes.Count - 1;
        }

        foreach (var frame in Keyframes[keyindex].Frames)
        {
            Frame newframe = new Frame(this, Keyframes[keyindex].Size);
            newframe.PaletteIndices = frame.PaletteIndices.Clone() as int[];
            Frames.Add(newframe);
        }

        FrameIndex = Keyframes[keyindex].FrameIndex;
        Size = Keyframes[keyindex].Size;

        return (keyindex + 1) * KEYFRAME_RATE;
    }

    public void AddFrame(bool duplicate = false)
    {
        uPixelCanvasOp addFrameOp = new uPixelCanvasOp();
        addFrameOp.type = uPixelCanvasOp.OpType.AddFrame;
        addFrameOp.frame = FrameIndex;
        addFrameOp.duplicate = duplicate;
        DoCanvasOperation(addFrameOp);
    }
    //TODO: make private
    public void AddFrameInternal(bool duplicate = false)
    {
        Frame newFrame = new Frame(this);
        if (duplicate)
        {
            GetCurrentFrame().PaletteIndices.CopyTo(newFrame.PaletteIndices, 0);
        }
        FrameIndex++;
        Frames.Insert(FrameIndex, newFrame);
        Debug.Log(Frames.Count);
    }

    public void Resize(Vector2Int newSize)
    {
        uPixelCanvasOp resizeOp = new uPixelCanvasOp();
        resizeOp.type = uPixelCanvasOp.OpType.Resize;
        resizeOp.resize = new uPixelCanvasOp.ResizeData();
        resizeOp.resize.size = newSize;
        DoCanvasOperation(resizeOp);
    }

    //TODO: make private
    public void ResizeInternal(Vector2Int newSize)
    {
        Vector2Int delta = newSize - Size;
        int leftPad = delta.x / 2;
        int rightPad = delta.x - leftPad;
        int topPad = delta.y / 2;
        int bottomPad = delta.y - topPad;
        for (int f = 0; f < Frames.Count; f++)
        {
            int[] newIndices = new int[newSize.x * newSize.y];
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

    public void SetPalette(Palette palette)
    {
        uPixelCanvasOp setPaletteOp = new uPixelCanvasOp();
        setPaletteOp.type = uPixelCanvasOp.OpType.PaletteSet;
        setPaletteOp.palette = palette;
        DoCanvasOperation(setPaletteOp);
    }

    public void SetPaletteInternal(Palette palette)
    {
        Palette = palette;
    }

    public void DoBlit(Vector2Int offset, Vector2Int size, Color32[] pixels, int frameIndex)
    {
        uPixelCanvasOp blitOp = new uPixelCanvasOp();
        blitOp.type = uPixelCanvasOp.OpType.BlitOp;
        blitOp.blitData = new uPixelCanvasOp.BlitData();
        blitOp.blitData.size = size;
        blitOp.blitData.offset = offset;
        blitOp.blitData.pixels = pixels;
        blitOp.blitData.frameIndex = frameIndex;
        DoCanvasOperation(blitOp);
    }

    public void DoBlitInternal(uPixelCanvasOp.BlitData blitData)
    {
        FrameIndex = blitData.frameIndex;
        var currentFrame = GetCurrentFrame();
        currentFrame.PaletteIndices = new int[blitData.pixels.Length];
        for (int i = 0; i < blitData.pixels.Length; i++)
        {
            currentFrame.PaletteIndices[i] = Palette.GetIndexOfColor(blitData.pixels[i]);
        }
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
        Size = DEFAULT_SIZE;
        Frames = new List<Frame>() { new Frame(this) };
        FrameIndex = 0;
        if (Keyframes == null)
        {
            Keyframes = new List<Keyframe>();
        }
    }

    public int GetHistoryLength()
    {
        return CanvasOpsTip;
    }

    public int GetHistoryLengthWithFuture()
    {
        return CanvasOps.Count;
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

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ImportPng(string importPath)
    {
        var importTexture = new Texture2D(2, 2);
        byte[] fileData = File.ReadAllBytes(importPath);
        importTexture.LoadImage(fileData);

        // Add frame for this image:
        this.AddFrame();

        // Palette loads unique colors from the texture
        if (Palette == null)
        {
            // TODO Not sure what the best thing to do here it - to get a Palette we need to create the asset.
            // TODO For now I'm going to create it in the same location as this Canvas:
            var newPalette = CreateInstance<Palette>();
            string path = AssetDatabase.GetAssetPath(this);
            path = path.Substring(0, path.LastIndexOf("."));
            path += "_palette.asset";
            AssetDatabase.CreateAsset(newPalette, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            this.SetPalette(newPalette);
        }
        Palette.PopulateFromTexture(importTexture);

        var newSize = new Vector2Int(importTexture.width, importTexture.height);

        this.Resize(newSize);

        // TODO we aren't actually using the size and offset yet...
        DoBlit(new Vector2Int(0, 0), newSize, importTexture.GetPixels32(), this.Frames.Count - 1);

    }

    public Texture2D ToTexture2D(int frameOverride = -1)
    {
        Texture2D t = new Texture2D(Size.x, Size.y);
        t.filterMode = FilterMode.Point;
        // Sanity check we have at least one frame and a palette
        if (Frames.Count > 0 && Palette != null)
        {
            Frame theFrame = GetCurrentFrame();
            if (frameOverride >= 0 && frameOverride < Frames.Count)
                theFrame = Frames[frameOverride];
            Color32[] colors = t.GetPixels32();
            for (int i = 0; i < theFrame.PaletteIndices.Length; i++)
            {
                colors[i] = Palette.Colors[theFrame.PaletteIndices[i]];
            }

            t.SetPixels32(colors);
            t.Apply();
        }
        return t;
    }

    public Texture2D GetTextureAtTime(int operation)
    {
        if (operation >= CanvasOpsTip)
        {
            // TODO: These two ExecuteCanvasOps calls result in an additional Frame getting created
            // when doing AddFrame
            ResetFrames();
            ExecuteCanvasOps(0, operation);
            Texture2D tex = ToTexture2D();
            ExecuteCanvasOps(0, CanvasOpsTip);
            return tex;
        }
        else
        {
            ResetFrames();
            ExecuteCanvasOps(0, operation);
            Texture2D tex = ToTexture2D();
            ExecuteCanvasOps(operation, CanvasOpsTip);
            return tex;
        }
    }

    private void ExecuteCanvasOps(int startIndex, int endIndex)
    {
        int jumpStart = startIndex;
        if (endIndex < CanvasOpsTip)
        {
            jumpStart = ApplyNearestKeyframe(endIndex);
        }
        int OpCount = CanvasOps.Count;
        for (int i = jumpStart; i < OpCount && i < endIndex; ++i)
        {
            CanvasOps[i].Execute(this);

            if (i % KEYFRAME_RATE == 0 && i / KEYFRAME_RATE > Keyframes.Count && i > 0)
            {
                GenerateKeyFrameNow();
            }
        }
    }

    public void DoCanvasOperation(uPixelCanvasOp op)
    {
        List<uPixelCanvasOp> rebaseFuture = null;
        // if a Redo history exists, calling this destroys this
        if (CanvasOps.Count > CanvasOpsTip)
        {
            if (FreezeFuture)
            {
                rebaseFuture = CanvasOps.GetRange(CanvasOpsTip, CanvasOps.Count - CanvasOpsTip);
            }

            CanvasOps.RemoveRange(CanvasOpsTip, CanvasOps.Count - CanvasOpsTip);
            TrimKeyFrames();
        }
        CanvasOps.Add(op);

        UnityEditor.Undo.RecordObject(this, string.Format("uPixelCanvas: {0}", op.GetType().ToString()));
        ExecuteCanvasOps(CanvasOpsTip, CanvasOpsTip + 1);
        CanvasOpsTip = CanvasOps.Count;
        ShadowCanvasOpTip = CanvasOpsTip;

        if (CanvasOpsTip % KEYFRAME_RATE == 0 && CanvasOpsTip > 0)
        {
            GenerateKeyFrameNow();
        }

        if (rebaseFuture != null)
        {
            CanvasOps.AddRange(rebaseFuture);
        }
    }

    public void RerunHistory()
    {
        ResetFrames();
        ExecuteCanvasOps(0, CanvasOpsTip);
        ShadowCanvasOpTip = CanvasOpsTip;
    }

    public void StepHistoryTo(int pointInTime)
    {
        pointInTime = System.Math.Min(pointInTime, CanvasOps.Count);
        ResetFrames();
        ExecuteCanvasOps(0, pointInTime);
        CanvasOpsTip = pointInTime;
        ShadowCanvasOpTip = pointInTime;
    }

    public bool CheckUndoRedo()
    {
        /*
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
        */
        return false;
    }
}
