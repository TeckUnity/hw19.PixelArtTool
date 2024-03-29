﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(uPixelCanvas))]
public class PixelAssetInspector : Editor
{
    Texture m_Texture;
    Image m_Image;
    VisualElement m_Root;

    void OnEnable()
    {
        var pixelAsset = target as uPixelCanvas;
        m_Texture = pixelAsset.ToTexture2D();
    }

    public override VisualElement CreateInspectorGUI()
    {
        m_Root = new VisualElement();
        // root.Bind(serializedObject);
        // SerializedProperty property = serializedObject.GetIterator();
        // if (property.NextVisible(true)) // Expand first child.
        // {
        //     do
        //     {
        //         var field = new PropertyField(property);
        //         field.name = "PropertyField:" + property.propertyPath;
        //         if (property.propertyPath == "m_Script" && serializedObject.targetObject != null)
        //             field.SetEnabled(false);
        //         if (property.propertyPath != "CanvasOps")
        //             m_Root.Add(field);
        //     }
        //     while (property.NextVisible(false));
        // }
        var paletteProp = serializedObject.FindProperty("Palette");
        var palette = new ObjectField("Palette");
        palette.objectType = typeof(Palette);
        palette.BindProperty(paletteProp);
        if (paletteProp.objectReferenceValue == null)
        {
            paletteProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Palette>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:Palette").First()));
        }
        m_Root.Add(palette);
        var size = new Vector2IntField("Size");
        size.value = serializedObject.FindProperty("Size").vector2IntValue;
        m_Root.Add(size);
        var button = new Button(() => ResizeCanvas(size.value));
        button.text = "Resize Canvas";
        m_Root.Add(button);
        int frameCount = serializedObject.FindProperty("Frames").arraySize;
        var frameSlider = new SliderInt("Frame", 0, frameCount - 1);
        frameSlider.BindProperty(serializedObject.FindProperty("FrameIndex"));
        frameSlider.RegisterValueChangedCallback(ChangeFrame);
        m_Root.Add(frameSlider);
        var frameIndex = new IntegerField("Frame");
        frameIndex.BindProperty(serializedObject.FindProperty("FrameIndex"));
        frameIndex.RegisterValueChangedCallback(ChangeFrame);
        frameIndex.SetEnabled(false);
        m_Root.Add(frameIndex);

        var importFrameButton = new Button(() => ImportFrame());
        importFrameButton.text = "Import Image";
        m_Root.Add(importFrameButton);

        m_Image = new Image();
        m_Image.image = m_Texture;
        var desiredSize = 128f;
        if (m_Texture.width >= m_Texture.height)
        {
            m_Image.style.width = desiredSize;
            m_Image.style.height = desiredSize * (m_Texture.height / (float)m_Texture.width);
        }
        else
        {
            m_Image.style.height = desiredSize;
            m_Image.style.width = desiredSize * (m_Texture.width / (float)m_Texture.height);
        }
        // m_Image.RegisterCallback<MouseDownEvent>(RandomisePixels);
        var spacer = new VisualElement();
        spacer.style.height = 10;
        m_Root.Add(spacer);
        m_Root.Add(m_Image);
        return m_Root;
    }

    void ChangeFrame(ChangeEvent<int> e)
    {
        Refresh();
    }

    public void Refresh()
    {
        var pixelAsset = target as uPixelCanvas;
        m_Texture = pixelAsset.ToTexture2D();
        m_Image.image = m_Texture;
    }

    void ResizeCanvas(Vector2Int newSize)
    {
        var pixelAsset = target as uPixelCanvas;
        pixelAsset.Resize(newSize);
        m_Texture = pixelAsset.ToTexture2D();
        m_Image.image = m_Texture;
        m_Root.Q<Vector2IntField>().value = newSize;
        Repaint();
    }

    void ImportFrame()
    {
        var importPath = EditorUtility.OpenFilePanel("Select image", "", "png");
        if (importPath.Length > 0)
        {
            if (File.Exists(importPath))
            {
                var pixelAsset = target as uPixelCanvas;
                pixelAsset.ImportPng(importPath);
            }
        }
    }

    void RandomisePixels(MouseDownEvent e)
    {
        var pixelAsset = target as uPixelCanvas;
        pixelAsset.RandomisePixels();
        m_Texture = pixelAsset.ToTexture2D();
        m_Image.image = m_Texture;
    }
}
