using UnityEngine;

/// <summary>
/// Animador de sprites por estados (quieto, caminar, salto). En el salto elige el frame según la
/// velocidad vertical. Si un estado tiene un solo frame, aplica una animación procedural
/// (respiración / rebote) para que el personaje no se vea estático.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public enum Estado { Quieto, Caminar, Salto }

    public Sprite[] quieto = new Sprite[0];
    public Sprite[] caminar = new Sprite[0];
    public Sprite[] salto = new Sprite[0];
    [Header("Poses de ataque")]
    public Sprite golpe;
    public Sprite patada;
    /// <summary>Si tiene valor, se muestra este sprite en lugar de la animación (lo usa el Fighter al atacar).</summary>
    [HideInInspector] public Sprite forzado;
    public float fpsQuieto = 8f;
    public float fpsCaminar = 11f;
    public Rigidbody2D cuerpo;

    SpriteRenderer sr;
    Estado estado;
    float t;
    Vector3 escalaBase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaBase = transform.localScale;
    }

    public void Reproducir(Estado nuevo)
    {
        if (nuevo == estado) return;
        estado = nuevo;
        t = 0f;
    }

    void Update()
    {
        t += Time.deltaTime;

        if (forzado != null)
        {
            sr.sprite = forzado;
            transform.localScale = escalaBase;
            return;
        }

        Sprite[] frames = quieto;
        if (estado == Estado.Caminar && Tiene(caminar)) frames = caminar;
        else if (estado == Estado.Salto && Tiene(salto)) frames = salto;
        if (!Tiene(frames)) return;

        int i;
        if (frames == salto && cuerpo != null)
        {
            float vy = cuerpo.velocity.y;
            i = vy > 7f ? 1 : vy > 2f ? 2 : vy > -2f ? 3 : vy > -7f ? 4 : 5;
            i = Mathf.Clamp(i, 0, frames.Length - 1);
        }
        else
        {
            float fps = estado == Estado.Caminar ? fpsCaminar : fpsQuieto;
            i = (int)(t * fps) % frames.Length;
        }
        sr.sprite = frames[i];

        float s = 0f;
        if (frames.Length == 1)
            s = estado == Estado.Caminar ? Mathf.Abs(Mathf.Sin(t * 12f)) * 0.04f : Mathf.Sin(t * 3f) * 0.015f;
        transform.localScale = new Vector3(escalaBase.x * (1f - s * 0.5f), escalaBase.y * (1f + s), escalaBase.z);
    }

    static bool Tiene(Sprite[] a) => a != null && a.Length > 0;
}
