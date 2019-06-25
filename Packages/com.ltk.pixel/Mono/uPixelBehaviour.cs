using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class uPixelBehaviour : MonoBehaviour
{

    public uPixelCanvas Canvas;

    private SpriteRenderer m_SpriteRenderer;

    void Start()
    {
        // Make sure we have a SpriteRenderer
        m_SpriteRenderer = GetComponent<SpriteRenderer>();
        if (m_SpriteRenderer == null)
        {
            m_SpriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    void Update()
    {
        // TODO animation
        if (m_SpriteRenderer.sprite == null && Canvas != null)
        {
            var tex = Canvas.ToTexture2D();
            m_SpriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }
    }
    
}
