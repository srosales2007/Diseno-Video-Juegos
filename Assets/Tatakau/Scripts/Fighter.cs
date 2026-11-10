using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Luchador: movimiento, salto, golpe, patada, cubrirse, salud, daño, aturdimiento y K.O.
/// Las órdenes vienen de un FighterInput (teclado o IA).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Fighter : MonoBehaviour
{
    [Header("Identidad")]
    public string nombre = "Erland";
    [Tooltip("true si el dibujo original mira hacia la derecha")]
    public bool spriteMiraDerecha = true;

    [Header("Movimiento")]
    public float velocidad = 4.5f;
    public float fuerzaSalto = 13f;

    [Header("Salud")]
    public float vidaMax = 100f;
    [Tooltip("Fracción del daño que se recibe al estar cubierto")]
    [Range(0f, 1f)] public float danoCubierto = 0.2f;

    [Header("Ataques")]
    public float danoGolpe = 6f;
    public float danoPatada = 10f;
    public float alcanceGolpe = 1.7f;
    public float alcancePatada = 2.1f;
    public float duracionGolpe = 0.32f;
    public float duracionPatada = 0.46f;
    public float empuje = 4f;

    [Header("Referencias")]
    public SpriteRenderer sprite;
    public SpriteAnimator animador;
    public FighterInput entrada;
    public Fighter rival;
    public Transform pies;
    public LayerMask capaSuelo;

    public float Vida { get; private set; }
    public bool KO => Vida <= 0f;
    public bool EnSuelo { get; private set; }
    public bool EstaAtacando { get; private set; }
    public bool EstaCubierto { get; private set; }
    public int Mirada { get; private set; } = 1;
    public bool PuedeActuar => controlActivo && !KO && aturdido <= 0f;
    /// <summary>Distancia (entre centros) a la que conviene atacar. La usa la IA.</summary>
    public float RangoAtaque => alcanceGolpe + 0.6f;

    /// <summary>Se dispara cada vez que cambia la vida (lo usa el HUD).</summary>
    public event Action<Fighter> VidaCambio;

    Rigidbody2D rb;
    Color colorBase;
    Vector3 spriteBase;
    Coroutine ataque;
    bool controlActivo;
    float aturdido;
    float empujeRestante;
    float destello;
    Color colorDestello;

    static readonly Color ColorCubierto = new Color(0.6f, 0.8f, 1f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (animador == null) animador = GetComponentInChildren<SpriteAnimator>();
        if (entrada == null) entrada = GetComponent<FighterInput>();
        colorBase = sprite.color;
        spriteBase = sprite.transform.localPosition;
        Vida = vidaMax;
    }

    public void ActivarControl(bool activo)
    {
        controlActivo = activo;
        if (!activo) EstaCubierto = false;
    }

    /// <summary>Deja al luchador listo para una ronda nueva.</summary>
    public void Reiniciar(Vector3 posicion)
    {
        StopAllCoroutines();
        ataque = null;
        transform.position = posicion;
        rb.velocity = Vector2.zero;
        Vida = vidaMax;
        EstaAtacando = EstaCubierto = false;
        aturdido = empujeRestante = destello = 0f;
        ResetearSprite();
        VidaCambio?.Invoke(this);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // en pausa

        EnSuelo = pies != null && Physics2D.OverlapBox(pies.position, new Vector2(0.7f, 0.12f), 0f, capaSuelo) != null;

        if (rival != null && !KO) Mirada = rival.transform.position.x >= transform.position.x ? 1 : -1;
        sprite.flipX = spriteMiraDerecha ? Mirada < 0 : Mirada > 0;

        if (aturdido > 0f) aturdido -= Time.deltaTime;
        if (empujeRestante > 0f) empujeRestante -= Time.deltaTime;

        if (entrada != null) entrada.Leer(this, rival);

        if (!PuedeActuar || entrada == null)
        {
            EstaCubierto = false;
            Animar(0f);
            return;
        }

        EstaCubierto = entrada.Cubrirse && EnSuelo && !EstaAtacando;

        if (!EstaAtacando && !EstaCubierto)
        {
            if (entrada.Golpe) ataque = StartCoroutine(Atacar(false));
            else if (entrada.Patada) ataque = StartCoroutine(Atacar(true));
            else if (entrada.Saltar && EnSuelo) rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
        }

        Animar(entrada.Horizontal);
    }

    void FixedUpdate()
    {
        // Durante el retroceso o el K.O. el cuerpo se desliza y frena solo
        if (KO || aturdido > 0f || empujeRestante > 0f)
        {
            rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, 12f * Time.fixedDeltaTime), rb.velocity.y);
            return;
        }

        bool puedeMoverse = PuedeActuar && entrada != null && !EstaCubierto && !(EstaAtacando && EnSuelo);
        float h = puedeMoverse ? entrada.Horizontal : 0f;
        float vel = h * velocidad;
        if (h != 0f && Mathf.Sign(h) != Mirada) vel *= 0.75f; // retroceder es más lento
        rb.velocity = new Vector2(vel, rb.velocity.y);
    }

    void LateUpdate()
    {
        if (destello > 0f)
        {
            destello -= Time.deltaTime;
            sprite.color = colorDestello;
        }
        else
        {
            sprite.color = EstaCubierto ? Color.Lerp(colorBase, ColorCubierto, 0.7f) : colorBase;
        }
    }

    void Animar(float h)
    {
        if (animador == null) return;
        if (!EnSuelo) animador.Reproducir(SpriteAnimator.Estado.Salto);
        else if (Mathf.Abs(h) > 0.1f && PuedeActuar && !EstaCubierto && !EstaAtacando) animador.Reproducir(SpriteAnimator.Estado.Caminar);
        else animador.Reproducir(SpriteAnimator.Estado.Quieto);
    }

    IEnumerator Atacar(bool esPatada)
    {
        EstaAtacando = true;
        float duracion = esPatada ? duracionPatada : duracionGolpe;
        float alcance = esPatada ? alcancePatada : alcanceGolpe;
        float dano = esPatada ? danoPatada : danoGolpe;
        Sprite pose = animador != null ? (esPatada ? animador.patada : animador.golpe) : null;
        Transform t = sprite.transform;
        bool conecto = false;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float p = tiempo / duracion;

            if (pose != null)
            {
                // Con pose dibujada: se muestra la pose y se da un pequeño paso adelante
                if (animador) animador.forzado = p >= 0.2f && p <= 0.85f ? pose : null;
                float paso = p < 0.2f ? 0f : p < 0.4f ? Mathf.Lerp(0f, 0.25f, (p - 0.2f) / 0.2f) : Mathf.Lerp(0.25f, 0f, (p - 0.4f) / 0.6f);
                t.localPosition = spriteBase + new Vector3(paso * Mirada, 0f, 0f);
            }
            else
            {
                // Sin pose: anticipación (hacia atrás) -> estocada (hacia adelante) -> recuperación
                float avance = p < 0.35f ? -0.12f * (p / 0.35f)
                             : p < 0.6f ? Mathf.Lerp(-0.12f, 0.5f, (p - 0.35f) / 0.25f)
                             : Mathf.Lerp(0.5f, 0f, (p - 0.6f) / 0.4f);
                float extension = Mathf.Max(0f, avance) / 0.5f;
                t.localPosition = spriteBase + new Vector3(avance * Mirada, esPatada ? extension * 0.15f : 0f, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, esPatada ? 10f * extension * Mirada : -4f * extension * Mirada);
            }

            if (!conecto && p >= 0.3f && p <= 0.65f) conecto = IntentarGolpe(alcance, dano, esPatada);
            yield return null;
        }

        ResetearSprite();
        EstaAtacando = false;
        ataque = null;
    }

    bool IntentarGolpe(float alcance, float dano, bool esPatada)
    {
        if (rival == null || rival.KO) return false;
        Vector2 centro = (Vector2)transform.position + new Vector2(Mirada * alcance * 0.5f, esPatada ? 1.2f : 2.2f);
        Vector2 tamano = new Vector2(alcance, esPatada ? 1.4f : 1.0f);
        foreach (var col in Physics2D.OverlapBoxAll(centro, tamano, 0f))
        {
            if (col.GetComponentInParent<Fighter>() == rival)
            {
                Vector3 punto = new Vector3(rival.transform.position.x - Mirada * 0.5f, transform.position.y + centro.y - transform.position.y, 0f);
                rival.RecibirDano(dano, Mirada, esPatada, punto);
                return true;
            }
        }
        return false;
    }

    /// <param name="direccion">Hacia dónde empuja el golpe (+1 derecha, -1 izquierda).</param>
    public void RecibirDano(float dano, int direccion, bool fuerte, Vector3 puntoImpacto)
    {
        if (KO) return;

        bool bloqueado = EstaCubierto;
        Vida = Mathf.Max(0f, Vida - (bloqueado ? dano * danoCubierto : dano));

        // Un golpe interrumpe el ataque propio
        if (ataque != null) { StopCoroutine(ataque); ataque = null; }
        EstaAtacando = false;
        ResetearSprite();

        destello = 0.12f;
        colorDestello = bloqueado ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.35f, 0.35f);

        if (bloqueado)
        {
            empujeRestante = 0.12f;
            rb.velocity = new Vector2(direccion * empuje * 0.5f, rb.velocity.y);
            HitSpark.Crear(puntoImpacto, new Color(0.5f, 0.85f, 1f), 0.8f);
            CameraShake.Sacudir(0.04f);
        }
        else
        {
            aturdido = fuerte ? 0.4f : 0.28f;
            rb.velocity = new Vector2(direccion * empuje * (fuerte ? 1.4f : 1f), fuerte ? 3f : 1.5f);
            HitSpark.Crear(puntoImpacto, new Color(1f, 0.85f, 0.4f), fuerte ? 1.5f : 1.1f);
            CameraShake.Sacudir(fuerte ? 0.18f : 0.1f);
        }

        if (KO)
        {
            rb.velocity = new Vector2(direccion * 6f, 6f);
            StartCoroutine(CaerKO(direccion));
        }

        VidaCambio?.Invoke(this);
    }

    IEnumerator CaerKO(int direccion)
    {
        Transform t = sprite.transform;
        float tiempo = 0f;
        while (tiempo < 0.45f)
        {
            tiempo += Time.deltaTime;
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -80f * direccion, tiempo / 0.45f));
            yield return null;
        }
    }

    void ResetearSprite()
    {
        if (animador) animador.forzado = null;
        sprite.transform.localPosition = spriteBase;
        sprite.transform.localRotation = Quaternion.identity;
    }

    void OnDrawGizmosSelected()
    {
        // Zonas de golpe para depurar en la vista Scene
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(Mirada * alcanceGolpe * 0.5f, 2.2f), new Vector3(alcanceGolpe, 1f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + new Vector3(Mirada * alcancePatada * 0.5f, 1.2f), new Vector3(alcancePatada, 1.4f));
    }
}
