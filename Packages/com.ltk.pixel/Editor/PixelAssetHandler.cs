using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

public class PixelAssetHandler
{
    [OnOpenAsset(0)]
    public static bool OnOpenPixelAsset(int instanceId, int line)
    {
        var pixelAsset = EditorUtility.InstanceIDToObject(instanceId) as uPixelCanvas;
        if (pixelAsset)
        {
            uPixel.Init(pixelAsset);
            return true;
        }
        return false;
    }
}
