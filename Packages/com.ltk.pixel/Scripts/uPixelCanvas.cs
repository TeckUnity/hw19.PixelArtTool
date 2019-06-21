using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class uPixelCanvas : ScriptableObject
{
    public Vector2Int Size;
    public Color32[] Palette;
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
}
