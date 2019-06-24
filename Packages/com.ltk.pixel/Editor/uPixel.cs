using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEditor.ShortcutManagement;

public class uPixel : EditorWindow
{
    private static uPixel window;
    private static string m_PackagePath;
    private static uPixelCanvas pixelAsset;
    private static VisualElement m_Root;
    private static Manipulator m_Tool;
    private static Manipulator m_Manipulator;
    private Image m_Image;
    private List<Toggle> m_Tools = new List<Toggle>();

    [MenuItem("Window/uPixel")]
    public static void Init()
    {
        Init(null);
    }

    public static void Init(uPixelCanvas _pixelAsset)
    {
        if (_pixelAsset != null)
        {
            pixelAsset = _pixelAsset;
        }
        GetWindow<uPixel>("uPixel");
    }

    void OnSelectionChange()
    {
        var _pixelAsset = Selection.activeObject as uPixelCanvas;
        if (_pixelAsset != null)
        {
            pixelAsset = _pixelAsset;
        }
    }

    [ClutchShortcut("uPixel/Pan", typeof(uPixel), KeyCode.Space)]
    static void PanShortcut(ShortcutArguments args)
    {
        if (args.stage == ShortcutStage.Begin)
        {
            if (m_Tool != null)
            {
                m_Root.RemoveManipulator(m_Tool);
            }
            m_Manipulator = new CanvasManipulator();
            m_Root.Q(name: "canvas").AddManipulator(m_Manipulator);
            return;
        }
        m_Root.Q(name: "canvas").RemoveManipulator(m_Manipulator);
        if (m_Tool != null)
        {
            m_Root.AddManipulator(m_Tool);
        }
    }

    [Shortcut("uPixel/Draw", typeof(uPixel), KeyCode.B)]
    static void DrawShortcut()
    {
        MouseUpEvent e = MouseUpEvent.GetPooled();
        e.target = m_Root.Q<Toggle>(name: "DrawTool");
        window.SwitchTool(e);
    }

    void OnEnable()
    {
        window = this;
        string[] search = AssetDatabase.FindAssets("t:asmdef uPixel");
        if (search.Length > 0)
        {
            m_PackagePath = Regex.Match(AssetDatabase.GUIDToAssetPath(search[0]), ".*\\/").ToString();
        }
        // Each editor window contains a root VisualElement object
        m_Root = rootVisualElement;
        m_Root.Clear();
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_PackagePath + "Editor/uPixel.uxml");
        VisualElement labelFromUXML = visualTree.CloneTree();
        // A stylesheet can be added to a VisualElement.
        // var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(m_PackagePath + "Editor/uPixel.uss");
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/uPixel.uss");
        // The style will be applied to the VisualElement and all of its children.
        m_Root.styleSheets.Add(styleSheet);
        m_Root.Add(labelFromUXML);
        m_Root.Q<VisualElement>(className: "canvas").StretchToParentSize();
        m_Root.Q<VisualElement>(className: "canvas").style.height = this.position.height;
        m_Root.Q<Image>().style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>(m_PackagePath + "T_Canvas.png");
        m_Root.Query<Toggle>().ForEach(o =>
        {
            o.RegisterCallback<MouseUpEvent>(SwitchTool);
            m_Tools.Add(o);
        });

        float minLen = Math.Min(this.position.width, this.position.height);
        float imageSize = minLen / 2f;

        m_Image = m_Root.Q<Image>();
        Texture t = pixelAsset != null ? pixelAsset.ToTexture2D() : Selection.activeObject as uPixelCanvas ? (Selection.activeObject as uPixelCanvas).ToTexture2D() : null;
        m_Image.image = t;
        m_Image.style.width = new StyleLength(imageSize);
        m_Image.style.height = new StyleLength(imageSize);
        m_Image.style.left = (this.position.width - imageSize) / 2f;
        m_Image.style.top = (this.position.height - imageSize) / 2f;
    }

    void SwitchTool(MouseUpEvent e)
    {
        var toggle = e.target as Toggle;
        foreach (var tool in m_Tools.Where(t => t != toggle))
        {
            tool.value = false;
        }
        if (m_Tool != null)
        {
            m_Root.RemoveManipulator(m_Tool);
        }
        toggle.value = true;
        toggle.Focus();
        m_Tool = new DrawTool();
        m_Root.Q(name: "canvas").AddManipulator(m_Tool);
    }

    void OnUndoRedo(ExecuteCommandEvent e)
    {
        // Check if undo or redo, modify undo/redo stack accordingly
        Debug.Log(e.imguiEvent.modifiers + " " + e.imguiEvent.keyCode);
    }
}