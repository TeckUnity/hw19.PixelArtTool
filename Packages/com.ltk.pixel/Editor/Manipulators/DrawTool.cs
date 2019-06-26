using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DrawTool : LineTool
{
    protected override bool IsContinuous()
    {
        return true;
    }
}