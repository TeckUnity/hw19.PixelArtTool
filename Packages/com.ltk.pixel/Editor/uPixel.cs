using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Text.RegularExpressions;
using UnityEditor.ShortcutManagement;

public class CanvasHistoryCache
{
    public class Entry
    {
        public int lru;
        public int id;
        public Texture2D texture;
    }

    int LRUCounter = 0;
    List<Entry> entries = new List<Entry>();

    public Texture2D GetHistoryPreview(uPixelCanvas canvas, int index)
    {
        int oldEntry = 0;
        int minLRU = LRUCounter + 1;
        for (int i = 0; i < entries.Count; ++i)
        {
            if (entries[i].lru < minLRU)
            {
                oldEntry = i;
                minLRU = entries[i].lru;
            }
            if (index == entries[i].id)
            {
                LRUCounter++;
                entries[i].lru = LRUCounter;
                return entries[i].texture;
            }
        }

        if (entries.Count >= 30)
        {
            entries.RemoveAt(oldEntry);
        }
        Entry e = new Entry();
        LRUCounter++;
        e.lru = LRUCounter;
        e.id = index;
        e.texture = canvas.GetTextureAtTime(index);
        entries.Add(e);
        return e.texture;
    }

    public void ClearCache()
    {
        entries.RemoveRange(0, entries.Count);
    }
}

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
    public static uPixelCanvas pixelAsset { get; private set; }
    private static VisualElement m_Root;
    private static Manipulator m_Tool;
    private static Manipulator m_Manipulator;
    private Image m_Image;
    private Dictionary<Type, Manipulator> m_Tools = new Dictionary<Type, Manipulator>();
    private List<Toggle> m_ToolButtons = new List<Toggle>();
    private Buffer m_DrawBuffer;
    private Buffer m_OverlayBuffer;
    public int paletteIndex { get; private set; }
    private Dictionary<KeyCode, bool> keyPressed = new Dictionary<KeyCode, bool>();
    private int m_HistoryValue;
    public CanvasHistoryCache m_HistoryCache;
    private Vector2Int[] m_Selection;

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

    public void DrawBuffer(params Vector2Int[] coords)
    {
        foreach (var coord in coords)
        {
            if (m_Selection != null && !m_Selection.Contains(coord))
            {
                continue;
            }
            m_DrawBuffer.SetPixel(coord, paletteIndex);
        }
        SetDirty(true);
    }

    public void ClearOverlay()
    {
        m_OverlayBuffer.Clear();
        SetDirty(true);
    }

    public void DrawOverlay(params Vector2Int[] coords)
    {
        foreach (var coord in coords)
        {
            m_OverlayBuffer.SetPixel(coord, 0);
        }
        SetDirty(true);
    }

    public void SetSelection(params Vector2Int[] coords)
    {
        m_Selection = coords;
        DrawOverlay(coords);
        SetDirty(true);
    }

    public void ClearSelection()
    {
        m_Selection = null;
        ClearOverlay();
        SetDirty(true);
    }

    public List<int> GetBrush()
    {
        List<int> pixels = new List<int>();
        for (int i = 0; i < m_DrawBuffer.Indices.Length; i++)
        {
            if (m_DrawBuffer.Indices[i] >= 0 && pixelAsset.GetCurrentFrame().PaletteIndices[i] != m_DrawBuffer.Indices[i])
                pixels.Add(i);
        }
        return pixels;
    }

    public void SetDirty(bool _dirty)
    {
        isDirty = _dirty;
    }

    public bool IsValidCoord(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < pixelAsset.Size.x && coord.y >= 0 && coord.y < pixelAsset.Size.y;
    }

    public int GetPaletteIndex(Vector2Int coord)
    {
        return IsValidCoord(coord) ? pixelAsset.GetCurrentFrame().PaletteIndices[coord.x + coord.y * pixelAsset.Size.x] : paletteIndex;
    }

    public void SetPaletteIndex(int index)
    {
        paletteIndex = index;
        DrawPalette();
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
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color32.Lerp(colors[i], colors[i].Luminance() > 0.5f ? Color.black : Color.white, m_OverlayBuffer.Indices[i] >= 0 ? 0.5f : 0);
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
        if (pixelAsset.Palette == null)
        {
            pixelAsset.Palette = AssetDatabase.LoadAssetAtPath<Palette>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:Palette").First()));
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

    [Shortcut("uPixel/Line", typeof(uPixel), KeyCode.L)]
    static void LineShortcut()
    {
        MouseUpEvent e = MouseUpEvent.GetPooled();
        e.target = m_Root.Q<Toggle>(name: "LineTool");
        window.SwitchTool(e);
    }

    [Shortcut("uPixel/Eyedropper", typeof(uPixel), KeyCode.I)]
    static void EyedropperShortcut()
    {
        MouseUpEvent e = MouseUpEvent.GetPooled();
        e.target = m_Root.Q<Toggle>(name: "EyedropperTool");
        window.SwitchTool(e);
    }

    [Shortcut("uPixel/Add Frame", typeof(uPixel), KeyCode.Equals, ShortcutModifiers.Shift)]
    static void AddFrameShortcut()
    {
        window.AddFrame();
    }

    [Shortcut("uPixel/Next Frame", typeof(uPixel), KeyCode.RightArrow)]
    static void NextFrameShortcut()
    {
        window.NextFrame();
    }

    [Shortcut("uPixel/Previous Frame", typeof(uPixel), KeyCode.LeftArrow)]
    static void PrevFrameShortcut()
    {
        window.PrevFrame();
    }

    [Shortcut("uPixel/Next Color", typeof(uPixel), KeyCode.RightBracket)]
    static void NextColorShortcut()
    {
        window.CyclePalette(1);
    }

    [Shortcut("uPixel/Previous Color", typeof(uPixel), KeyCode.LeftBracket)]
    static void PrevColorShortcut()
    {
        window.CyclePalette(-1);
    }

    [Shortcut("uPixel/Marquee Select", typeof(uPixel), KeyCode.M)]
    static void MarqueeSelectShortcut()
    {
        MouseUpEvent e = MouseUpEvent.GetPooled();
        e.target = m_Root.Q<Toggle>(name: "MarqueeSelectTool");
        window.SwitchTool(e);
    }

    void OnEnable()
    {
        UnityEditor.Undo.postprocessModifications += OnPropMod;
        UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
        window = this;
        string[] search = AssetDatabase.FindAssets("t:asmdef uPixel");
        if (search.Length > 0)
        {
            m_PackagePath = Regex.Match(AssetDatabase.GUIDToAssetPath(search[0]), ".*\\/").ToString();
        }
        m_Tool = null;
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
            m_ToolButtons.Add(o);
        });
        m_Root.focusable = true;
        m_Root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        m_Root.RegisterCallback<KeyUpEvent>(OnKeyUp);
        m_Root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

        m_Image = m_Root.Q<Image>();
        var canvas = m_Root.Q<VisualElement>(className: "canvas");
        var history = m_Root.Q<VisualElement>(name: "History");
        // history.style.width = this.position.width;
        // history.style.top = this.position.height - history.style.height.value.value;
        VisualElement HistoryDrawParent = history.Q<VisualElement>(name: "HistoryDraw");
        HistoryDrawParent.Add(new IMGUIContainer(HistoryDrawOnGUI));
        InitImage();

        DrawPalette();

        m_HistoryValue = pixelAsset.GetHistoryLength();

    }

    private void DrawPalette()
    {
        var palette = m_Root.Q<VisualElement>(name: "palette");
        palette.Clear();
        var paletteSO = new SerializedObject(pixelAsset.Palette);
        var picker = new ObjectField();
        picker.objectType = typeof(Palette);
        picker.BindProperty(paletteSO);
        palette.Add(picker);
        // palette.Add(new IMGUIContainer(PaletteDrawOnGUI));
        palette.style.backgroundColor = Color.black;
        var colorsProp = paletteSO.FindProperty("Colors");
        for (int i = 0; i < colorsProp.arraySize; i++)
        {
            if (i == paletteIndex)
            {
                var entry = new ColorField();
                entry.pickingMode = PickingMode.Position;
                entry.BindProperty(colorsProp.GetArrayElementAtIndex(i));
                entry.showEyeDropper = false;
                entry.showAlpha = false;
                entry.style.width = entry.style.height = 18;
                entry.style.marginLeft = entry.style.marginRight = 0;
                entry.style.marginTop = entry.style.marginBottom = 0;
                entry.style.borderColor = Color.white;
                entry.style.borderLeftWidth = entry.style.borderRightWidth = 1;
                entry.style.borderTopWidth = entry.style.borderBottomWidth = 1;
                entry.style.borderBottomLeftRadius = entry.style.borderBottomRightRadius = 2;
                entry.style.borderTopLeftRadius = entry.style.borderTopRightRadius = 2;
                entry.RegisterValueChangedCallback(OnPaletteEdit);
                palette.Add(entry);
            }
            else
            {
                var index = i;
                var entry = new Button(() =>
                {
                    paletteIndex = index;
                    var colors = palette.Query<VisualElement>();
                    colors.ForEach(c => palette.Remove(c));
                    DrawPalette();
                });
                entry.style.backgroundImage = EditorGUIUtility.whiteTexture;
                entry.style.unityBackgroundImageTintColor = (Color)pixelAsset.Palette.Colors[i];
                entry.style.width = entry.style.height = 16;
                entry.style.marginLeft = entry.style.marginRight = 1;
                entry.style.marginTop = entry.style.marginBottom = 1;
                palette.Add(entry);
            }
        }
        palette.style.height = (pixelAsset.Palette.Colors.Length / 4) * 18;
    }

    private void PaletteDrawOnGUI()
    {
        GUIStyle b = new GUIStyle(GUI.skin.button);
        b.normal.background = EditorGUIUtility.whiteTexture;
        Color c = GUI.color;
        for (int i = 0; i < pixelAsset.Palette.Colors.Length; i++)
        {
            int x = i % 4;
            int y = i / 4;
            GUI.color = pixelAsset.Palette.Colors[i];
            RectOffset ro = new RectOffset(1, 1, 1, 1);
            Rect r = new Rect(x * 16, y * 16, 16, 16);
            if (i == paletteIndex)
            {
                pixelAsset.Palette.Colors[i] = EditorGUI.ColorField(ro.Add(ro.Add(r)), GUIContent.none, pixelAsset.Palette.Colors[i], false, false, false);
            }
            else
            {
                if (GUI.Button(ro.Remove(r), EditorGUIUtility.whiteTexture, b))
                {
                    paletteIndex = i;
                }
            }
        }
    }

    private void OnPaletteEdit(ChangeEvent<Color> e)
    {
        InitImage();
        m_HistoryCache.ClearCache();
    }

    private void OnDisable()
    {
        UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnGeometryChanged(GeometryChangedEvent e)
    {
        m_Root.Q<VisualElement>(className: "canvas").style.height = this.position.height;
        var history = m_Root.Q<VisualElement>(name: "History");
        history.style.width = this.position.width;
        history.style.top = this.position.height - 96;
        var palette = m_Root.Q<VisualElement>(name: "palette");
        palette.style.left = this.position.xMax - 18 * 4;
        palette.style.width = 18 * 4;
        InitImage();
    }

    void OnKeyDown(KeyDownEvent e)
    {
        bool pressed;
        if (keyPressed.TryGetValue(e.keyCode, out pressed))
        {
            if (!pressed)
            {
                keyPressed[e.keyCode] = true;
            }
        }
        else
        {
            keyPressed.Add(e.keyCode, true);
        }
        if (!pressed && keyPressed[e.keyCode])
        {
            if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl)
            {
                if (m_Tool != null)
                {
                    m_Root.Q(name: "canvas").RemoveManipulator(m_Tool);
                }
                m_Manipulator = new EyedropperTool();
                m_Root.Q(name: "canvas").AddManipulator(m_Manipulator);
            }
        }
    }

    void OnKeyUp(KeyUpEvent e)
    {
        // ShortcutManager eats the initial KeyDownEvent for registered keys
        bool pressed;
        if (keyPressed.TryGetValue(e.keyCode, out pressed))
        {
            if (pressed)
            {
                keyPressed[e.keyCode] = false;
            }
        }
        else
        {
            keyPressed.Add(e.keyCode, false);
        }
        if (!keyPressed[e.keyCode])
        {
            if (e.keyCode == KeyCode.LeftControl || e.keyCode == KeyCode.RightControl)
            {
                m_Root.Q(name: "canvas").RemoveManipulator(m_Manipulator);
                if (m_Tool != null)
                {
                    m_Root.Q(name: "canvas").AddManipulator(m_Tool);
                }
            }
        }
    }
    void HistoryDrawOnGUI()
    {
        Event e = Event.current;
        if (m_HistoryCache == null)
        {
            m_HistoryCache = new CanvasHistoryCache();
        }
        GUILayout.BeginHorizontal();

        int historyIndex = Math.Max(m_HistoryValue - 5, 0);
        for (int i = 0; i < 10; ++i)
        {
            Rect rect = GUILayoutUtility.GetRect(32, 32);
            GUILayout.Space(6);
            if (historyIndex + i <= pixelAsset.GetHistoryLengthWithFuture())
            {
                if (historyIndex + i == pixelAsset.GetHistoryLength())
                {
                    EditorGUI.DrawPreviewTexture(new RectOffset(2, 2, 2, 2).Add(rect), EditorGUIUtility.whiteTexture);
                }
                if (historyIndex + i <= pixelAsset.GetHistoryLength())
                    EditorGUI.DrawPreviewTexture(rect, m_HistoryCache.GetHistoryPreview(pixelAsset, historyIndex + i));
                else
                {
                    GUI.DrawTexture(rect, m_HistoryCache.GetHistoryPreview(pixelAsset, historyIndex + i), ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.2f), 0f, 0f);
                }
                if (rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        // TODO: Set history state
                        pixelAsset.StepHistoryTo(historyIndex + i);
                        InitImage();
                        m_HistoryCache.ClearCache();
                    }
                }
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            }
            if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
            {
                m_HistoryValue = Mathf.Clamp(m_HistoryValue + (int)Mathf.Sign(e.delta.y), 0, pixelAsset.GetHistoryLengthWithFuture());
                pixelAsset.StepHistoryTo(m_HistoryValue);
                InitImage();
                m_HistoryCache.ClearCache();
            }
        }
        GUILayout.EndHorizontal();
        // m_HistoryValue = EditorGUILayout.IntSlider(GUIContent.none, m_HistoryValue, 5, pixelAsset.GetHistoryLengthWithFuture());
        m_HistoryValue = Mathf.FloorToInt(GUILayout.HorizontalSlider(m_HistoryValue, 5, pixelAsset.GetHistoryLengthWithFuture()));
        pixelAsset.FreezeFuture = GUILayout.Toggle(pixelAsset.FreezeFuture, "Freeze Future");
    }

    void InitImage()
    {
        if (pixelAsset == null) return;

        // pixelAsset.RerunHistory();

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

    public void AddFrame(bool duplicate = false)
    {
        pixelAsset.AddFrame(duplicate);
        InitImage();
    }

    public void NextFrame()
    {
        GotoFrame(++pixelAsset.FrameIndex);
    }

    public void PrevFrame()
    {
        GotoFrame(--pixelAsset.FrameIndex);
    }

    public void GotoFrame(int index)
    {
        pixelAsset.FrameIndex = (index + pixelAsset.Frames.Count) % pixelAsset.Frames.Count;
        InitImage();
    }

    public void CyclePalette(int delta)
    {
        paletteIndex = (paletteIndex + delta + pixelAsset.Palette.Colors.Length) % pixelAsset.Palette.Colors.Length;
        DrawPalette();
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
        foreach (var tool in m_ToolButtons)
        {
            tool.value = tool == toggle;
        }
        if (m_Tool != null)
        {
            m_Root.RemoveManipulator(m_Tool);
        }
        toggle.value = true;
        toggle.Focus();
        // toggle.Blur();
        if (toolType != null)
        {
            Manipulator tool;
            if (m_Tools.TryGetValue(toolType, out tool))
            {
                m_Tool = m_Tools[toolType];
            }
            else
            {
                m_Tool = System.Activator.CreateInstanceFrom(toolType.Assembly.CodeBase, toolType.FullName).Unwrap() as Manipulator;
                m_Tools.Add(toolType, m_Tool);
            }
            m_Root.Q(name: "canvas").AddManipulator(m_Tool);
        }
    }

    private void OnUndoRedo()
    {
        if (pixelAsset.CheckUndoRedo())
        {
            SetDirty(true);
        }
    }

    private UndoPropertyModification[] OnPropMod(UndoPropertyModification[] modifications)
    {
        foreach (var mod in modifications)
        {
            if (mod.currentValue.target is uPixelCanvas)
            {
                // Debug.LogFormat("Undo mod {0}", mod.currentValue.propertyPath);
            }
        }
        return modifications;
    }
}