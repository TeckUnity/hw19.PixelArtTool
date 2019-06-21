using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;

[ScriptedImporter(1, "pal")]
public class PalImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var pal = ScriptableObject.CreateInstance<Palette>();
        var lines = File.ReadAllLines(ctx.assetPath);
        var colors = new List<Color>();
        foreach (var line in lines)
        {
            var rgb = line.Split(' ');
            if (rgb.Length < 3)
            {
                continue;
            }
            colors.Add(new Color(float.Parse(rgb[0]) / 255, float.Parse(rgb[1]) / 255, float.Parse(rgb[2]) / 255));
        }
        pal.Colors = colors.ToArray();
        int dim = Mathf.CeilToInt(Mathf.Sqrt(colors.Count));
        while (colors.Count < dim * dim)
        {
            colors.Add(Color.black);
        }
        var cb = new Color[colors.Count];
        for (int y = 0; y < dim; y++)
        {
            for (int x = 0; x < dim; x++)
            {
                cb[y * dim + x] = colors[(dim - (y + 1)) * dim + x];
            }
        }
        // colors.Sort((c0, c1) => c0.grayscale.CompareTo(c1.grayscale));
        Texture2D t = new Texture2D(dim, dim);
        t.SetPixels(cb);
        t.Apply();
        ctx.AddObjectToAsset(Path.GetFileNameWithoutExtension(ctx.assetPath), pal, t);
        ctx.SetMainObject(pal);
    }
}

[CustomEditor(typeof(Palette))]
public class PaletteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        float x = 0;
        float y = 0;
        int dim = (int)(EditorGUIUtility.currentViewWidth / 8);
        var pal = target as Palette;
        Color guiColor = GUI.color;
        GUILayout.Label("", GUILayout.Height(dim * Mathf.Ceil(pal.Colors.Length / 8f)));
        // using (var scope = new GUILayout.AreaScope(new Rect(0, 0, Screen.width, dim * Mathf.Ceil(pal.Colors.Length / 8f))))
        // using (var scope = new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Height(dim * Mathf.Ceil(pal.Colors.Length / 8f))))
        {
            foreach (var c in pal.Colors)
            {
                GUI.color = c;
                GUI.DrawTexture(new Rect(x + 1, y + 1, dim - 2, dim - 2), EditorGUIUtility.whiteTexture);
                x += dim;
                if (x > dim * 7)
                {
                    x = 0;
                    y += dim;
                }
            }
        }
        GUI.color = guiColor;
    }
}