using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEditor.ShortcutManagement;

public class uPixel : EditorWindow
{
    public class Buffer
    {
        public Vector2Int Size;
        public int[] Indices;
        public bool isDirty;

        public Buffer(uPixelCanvas pixelAsset)
        {
            Size = pixelAsset.Size;
            Indices = new int[Size.x * Size.y];
            Clear();
        }

        private bool IsValidCoord(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < Size.x && coord.y >= 0 && coord.y < Size.y;
        }

        public void Clear()
        {
            for (int i = 0; i < Indices.Length; i++)
            {
                Indices[i] = -1;
            }
            isDirty = true;
        }

        public void Flush(ref int[] paletteIndices)
        {
            for (int i = 0; i < paletteIndices.Length; i++)
            {
                // TODO: reference canvas' current frame property when available
                paletteIndices[i] = Indices[i] < 0 ? paletteIndices[i] : Indices[i];
            }
            Clear();
        }

        public Vector2Int GetCoord(int index)
        {
            return new Vector2Int(Indices[index % Size.x], Indices[index / Size.x]);
        }

        public void SetPixel(Vector2Int coord, int paletteIndex)
        {
            if (!IsValidCoord(coord))
            {
                return;
            }
            Indices[coord.x + coord.y * Size.x] = paletteIndex;
            isDirty = true;
        }
    }

    private static uPixel window;
    private static string m_PackagePath;
    private static uPixelCanvas pixelAsset;
    private static VisualElement m_Root;
    private static Manipulator m_Tool;
    private static Manipulator m_Manipulator;
    private Image m_Image;
    private List<Toggle> m_Tools = new List<Toggle>();
    private Buffer m_DrawBuffer;
    private Buffer m_OverlayBuffer;
    private int paletteIndex;

    private bool isDirty;

    [MenuItem("Window/uPixel")]
    public static void Init()
    {
        Init(null);
    }

    public void ClearBuffer()
    {
        m_DrawBuffer.Clear();
        SetDirty(true);
    }

    public void DrawBuffer(Vector2Int coord)
    {
        m_DrawBuffer.SetPixel(coord, paletteIndex);
        SetDirty(true);
    }

    public void FlushBuffer()
    {
        m_DrawBuffer.Flush(ref pixelAsset.GetCurrentFrame().PaletteIndices);
        SetDirty(true);
    }

    public void SetDirty(bool _dirty)
    {
        isDirty = _dirty;
    }

    void Update()
    {
        if (!isDirty)
        {
            return;
        }
        Color32[] colors = new Color32[pixelAsset.GetCurrentFrame().PaletteIndices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = pixelAsset.Palette.Colors[m_DrawBuffer.Indices[i] < 0 ? pixelAsset.GetCurrentFrame().PaletteIndices[i] : m_DrawBuffer.Indices[i]];
        }
        (m_Image.image as Texture2D).SetPixels32(colors);
        (m_Image.image as Texture2D).Apply();
        EditorUtility.SetDirty(pixelAsset);
        SetDirty(false);
        Repaint();
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
            InitImage();
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
        UnityEditor.Undo.postprocessModifications += OnUndoRedo;
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

        m_Image = m_Root.Q<Image>();
        InitImage();
    }

    void InitImage()
    {
        window.m_DrawBuffer = new Buffer(pixelAsset);
        window.m_OverlayBuffer = new Buffer(pixelAsset);

        float minLen = Math.Min(this.position.width, this.position.height);
        float imageSize = minLen / 2f;

        Texture2D t = pixelAsset != null ? pixelAsset.ToTexture2D() : Selection.activeObject as uPixelCanvas ? (Selection.activeObject as uPixelCanvas).ToTexture2D() : null;
        m_Image.image = t;
        m_Image.style.width = new StyleLength(imageSize);
        m_Image.style.height = new StyleLength(imageSize);
        m_Image.style.left = (this.position.width - imageSize) / 2f;
        m_Image.style.top = (this.position.height - imageSize) / 2f;
        Color32[] colors = new Color32[t.width * t.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }
        t.SetPixels32(colors);
    }

    public void CyclePalette(int delta)
    {
        paletteIndex = (paletteIndex + delta + pixelAsset.Palette.Colors.Length) % pixelAsset.Palette.Colors.Length;
        SetDirty(true);
    }

    void SwitchTool(MouseUpEvent e)
    {
        var toggle = e.target as Toggle;
        Type toolType = Type.GetType(toggle.name);
        if (m_Tool != null && m_Tool.GetType() == toolType)
        {
            return;
        }
        foreach (var tool in m_Tools)
        {
            tool.value = tool == toggle;
        }
        if (m_Tool != null)
        {
            m_Root.RemoveManipulator(m_Tool);
        }
        toggle.value = true;
        toggle.Focus();
        toggle.Blur();
        if (toolType != null)
        {
            m_Tool = System.Activator.CreateInstanceFrom(toolType.Assembly.CodeBase, toolType.FullName).Unwrap() as Manipulator;
            m_Root.Q(name: "canvas").AddManipulator(m_Tool);
        }
    }

    private UndoPropertyModification[] OnUndoRedo(UndoPropertyModification[] modifications)
    {
        return modifications;
    }
}