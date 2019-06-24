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

                root.Add(field);
            }
            while (property.NextVisible(false));
        }
        root.Q<VisualElement>(name: "PropertyField:Frames").RegisterCallback<MouseDownEvent>(RandomisePixels);
        Image image = new Image();
        image.image = m_Texture;
        image.style.width = image.style.height = 128;
        root.Add(image);
        return root;
    }

    void RandomisePixels(MouseDownEvent e)
    {
        if (e.button == 1)
        {
            (target as uPixelCanvas).RandomisePixels();
        }
    }
}
