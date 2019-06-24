using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CustomAssetHandler
{
    [UnityEditor.Callbacks.OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (Selection.activeObject as uPixelCanvas != null)
        {
            var canvasEditorWindow = EditorWindow.GetWindow<uPixel>("uPixel");
            return true;
        }
        return false; // we did not handle the open
    }
}
