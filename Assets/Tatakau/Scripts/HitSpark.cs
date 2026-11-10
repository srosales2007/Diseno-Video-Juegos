using UnityEngine;

/// <summary>Destello circular que aparece en el punto de impacto. La textura se genera por código.</summary>
public class HitSpark : MonoBehaviour
{
    static Sprite circulo;

    SpriteRenderer sr;
    float tamano;
    float t;

    public static void Crear(Vector3 posicion, Color color, float tamano)
    {
        if (circulo == null) circulo = CrearCirculo();
        var go = new GameObject("Impacto");
        go.transform.position = posicion;
        var render = go.AddComponent<SpriteRenderer>();
        render.sprite = circulo;
        render.color = color;
        render.sortingOrder = 50;
        var chispa = go.AddComponent<HitSpark>();
        chispa.sr = render;
        chispa.tamano = tamano;
        go.transform.localScale = Vector3.one * tamano * 0.3f;
    }

    void Update()
    {
        t += Time.deltaTime / 0.18f;
        transform.localScale = Vector3.one * tamano * Mathf.Lerp(0.3f, 1.2f, t);
        Color c = sr.color;
        c.a = 1f - t;
        sr.color = c;
        if (t >= 1f) Destroy(gameObject);
    }

    static Sprite CrearCirculo()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Sqrt(a)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
