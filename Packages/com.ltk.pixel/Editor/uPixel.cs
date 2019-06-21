using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;

public class uPixel : EditorWindow
{
    private static string m_PackagePath;

    [MenuItem("Window/uPixel")]
    public static void Init()
    {
        GetWindow<uPixel>("uPixel");
    }

    [UnityEditor.ShortcutManagement.Shortcut("uPixel/Brush", typeof(uPixel), KeyCode.B)]
    static void Test()
    {
        Debug.Log("Test");
    }

    public void OnFocus()
    {
        string[] search = AssetDatabase.FindAssets("t:asmdef uPixel");
        if (search.Length > 0)
        {
            m_PackagePath = Regex.Match(AssetDatabase.GUIDToAssetPath(search[0]), ".*\\/").ToString();
        }
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        root.Clear();
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_PackagePath + "Editor/uPixel.uxml");
        VisualElement labelFromUXML = visualTree.CloneTree();
        // A stylesheet can be added to a VisualElement.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(m_PackagePath + "Editor/uPixel.uss");
        // The style will be applied to the VisualElement and all of its children.
        root.styleSheets.Add(styleSheet);
        root.Add(labelFromUXML);
        root.Q<VisualElement>(className: "canvas").StretchToParentSize();
        root.Q<VisualElement>(className: "canvas").style.height = this.position.height;
        root.Q<Image>().style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>(m_PackagePath + "T_Canvas.png");
        var ti = AssetImporter.GetAtPath(m_PackagePath + "T_TestImage.png") as TextureImporter;
        ti.isReadable = true;
        ti.SaveAndReimport();
        root.Q<Image>().image = AssetDatabase.LoadAssetAtPath<Texture>(m_PackagePath + "T_TestImage.png");
        root.Q<Image>().style.width = new StyleLength(root.Q<Image>().image.width);
        root.Q<Image>().style.height = new StyleLength(root.Q<Image>().image.height);
        root.Q(name = "canvas").AddManipulator(new CanvasManipulator());
    }
}