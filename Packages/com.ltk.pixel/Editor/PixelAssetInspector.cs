using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(uPixelCanvas))]
public class PixelAssetInspector : Editor
{
    Texture m_Texture;
    Image m_Image;

    void OnEnable()
    {
        var pixelAsset = target as uPixelCanvas;
        m_Texture = pixelAsset.ToTexture2D();
    }

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();
        root.Bind(serializedObject);
        SerializedProperty property = serializedObject.GetIterator();
        if (property.NextVisible(true)) // Expand first child.
        {
            do
            {
                var field = new PropertyField(property);
                field.name = "PropertyField:" + property.propertyPath;

                if (property.propertyPath == "m_Script" && serializedObject.targetObject != null)
                    field.SetEnabled(false);
                if (property.propertyPath != "CanvasOps")
                    root.Add(field);
            }
            while (property.NextVisible(false));
        }
        // root.Q<VisualElement>(name: "PropertyField:Frames").RegisterCallback<MouseDownEvent>(RandomisePixels);
        m_Image = new Image();
        m_Image.image = m_Texture;
        m_Image.style.width = m_Image.style.height = 128;
        m_Image.RegisterCallback<MouseDownEvent>(RandomisePixels);
        root.Add(m_Image);
        return root;
    }

    void RandomisePixels(MouseDownEvent e)
    {
        var pixelAsset = target as uPixelCanvas;
        pixelAsset.RandomisePixels();
        m_Texture = pixelAsset.ToTexture2D();
        m_Image.image = m_Texture;
    }
}
