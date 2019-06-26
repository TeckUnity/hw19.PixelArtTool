using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class uPixelBehaviour : MonoBehaviour
{

    public uPixelCanvas Canvas;

    // Scale factor for the Canvas's texture - e.g. 3 means every pixel in the Canvas texture is drawn 3x3 on our sprite
    public int Scale = 1;
    private int m_lastScale = 1; // TODO this is a bit horrible, just to test

    public bool Animate = false;

    public int CurrentFrame = 0;

    public float FrameLen = 0.2f;

    private float m_frameElapsed = 0f;

    private SpriteRenderer m_SpriteRenderer;

    // We use this to scale up the Canvas's Texture2D reasonably efficiently
    private RenderTexture m_ScaledRenderTex;

    private Texture2D m_ScaledTex2D;

    // Material we use to draw on the scaled render texture
    private Material m_drawMaterial;

    void Start()
    {
        // Make sure we have a SpriteRenderer
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        if (m_SpriteRenderer == null)
        {
            m_SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        m_drawMaterial = new Material(Shader.Find("Sprites/Default"));
    }
    
    void Update()
    {
        if (Canvas != null)
        {
            if (Animate)
            {
                m_frameElapsed += Time.deltaTime;
                if (m_frameElapsed >= FrameLen)
                {
                    CurrentFrame++;
                    if (CurrentFrame >= Canvas.Frames.Count)
                        CurrentFrame = 0;
                    m_frameElapsed = 0f;
                    // Force re-aquistion of texture:
                    m_SpriteRenderer.sprite = null;
                }
            }
            if (m_SpriteRenderer.sprite == null || m_lastScale != Scale)
            {
                m_lastScale = Scale;
                m_SpriteRenderer.sprite = GetScaledSprite();
            }
        }
    }

    private Sprite GetScaledSprite()
    {
        if (Canvas == null || Scale < 1)
            return null;
        var tex = Canvas.ToTexture2D(CurrentFrame); // TODO seems like we shouldn't be doing this every time - maybe Canvas needs 'dirty' flag?
        int w = tex.width * Scale;
        int h = tex.height * Scale;
        if (m_ScaledRenderTex != null)
        {
            m_ScaledRenderTex.Release();
            m_ScaledRenderTex = null;
        }

        m_ScaledRenderTex = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var sourcePixels = tex.GetPixels32();

        // Get set up to draw on the render tex:
        RenderTexture.active = m_ScaledRenderTex;
        m_drawMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, m_ScaledRenderTex.width, m_ScaledRenderTex.height, 0);
        GL.Clear(false, true, Color.clear);
        // Walk source texture, and write to destination render tex based on source values. We just write a (scaled)
        // GL Quad for every source pixel.
        GL.Begin(GL.QUADS);
        for (int y = 0; y < tex.height; y++)
        {
            // Note that the render texture y axis goes from top to bottom (0 is top) while texture2d runs from
            // bottom to top (0 is bottom)
            var rendTop = h - ((y + 1) * Scale);
            var rendBottom = h - (y * Scale);
            for (int x = 0; x < tex.width; x++)
            {
                var rendLeft = x * Scale;
                var rendRight = (x + 1) * Scale;
                var pix = sourcePixels[(y * tex.width) + x];
                GL.Color(pix);
                GL.Vertex3(rendLeft, rendTop, 0f);
                GL.Vertex3(rendRight, rendTop, 0f);
                GL.Vertex3(rendRight, rendBottom, 0f);
                GL.Vertex3(rendLeft, rendBottom, 0f);
            }
        }
        GL.End();
        GL.PopMatrix();

        // We need a texture2d rather than a render texture for the sprite - make sure we have one of the right size
        if (m_ScaledTex2D == null)
        {
            m_ScaledTex2D = new Texture2D(w, h, TextureFormat.ARGB32, false, true);
        }
        else
        {
            m_ScaledTex2D.Resize(w, h);
        }

        // Our render tex is still active, so this just copies whole of render texture into whole of the texture2d
        m_ScaledTex2D.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        m_ScaledTex2D.Apply();
        // Finished with RenderTex
        RenderTexture.active = null;

        float ppu = Camera.main.pixelHeight / (2f * Camera.main.orthographicSize); // TODO should user be able to set this?

        // Make sprite from our scaled tex2d
        return Sprite.Create(m_ScaledTex2D, new Rect(0, 0, m_ScaledTex2D.width, m_ScaledTex2D.height),
            new Vector2(0.5f,0.5f), ppu);

    }
    
}
