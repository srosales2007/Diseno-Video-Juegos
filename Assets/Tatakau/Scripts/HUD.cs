using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz de combate: barras de salud con "daño retrasado" (franja blanca que baja después),
/// color de la barra según la vida, temporizador, marcadores de rondas y mensajes centrales animados.
/// </summary>
public class HUD : MonoBehaviour
{
    [Header("Luchadores")]
    public Fighter jugador1;
    public Fighter jugador2;

    [Header("Barras (RectTransform que se rellenan)")]
    public RectTransform vida1;
    public RectTransform vida2;
    public RectTransform retraso1;
    public RectTransform retraso2;

    [Header("Textos")]
    public Text nombre1;
    public Text nombre2;
    public Text textoTiempo;
    public Text textoMensaje;
    public Text textoAyuda;

    [Header("Rondas ganadas")]
    public Image[] rondas1 = new Image[0];
    public Image[] rondas2 = new Image[0];

    static readonly Color RondaGanada = new Color(1f, 0.78f, 0.2f);
    static readonly Color RondaVacia = new Color(0.15f, 0.15f, 0.18f, 0.9f);

    float retrasoV1 = 1f, retrasoV2 = 1f;
    float esperaRetraso1, esperaRetraso2;
    float duracionMensaje, tiempoMensaje;

    void Awake()
    {
        OcultarMensaje();
    }

    void Update()
    {
        if (jugador1 == null || jugador2 == null) return;

        float p1 = jugador1.Vida / jugador1.vidaMax;
        float p2 = jugador2.Vida / jugador2.vidaMax;
        ActualizarRetraso(ref retrasoV1, ref esperaRetraso1, p1);
        ActualizarRetraso(ref retrasoV2, ref esperaRetraso2, p2);

        Rellenar(vida1, p1, false);
        Rellenar(retraso1, retrasoV1, false);
        Rellenar(vida2, p2, true);
        Rellenar(retraso2, retrasoV2, true);
        ColorVida(vida1, p1);
        ColorVida(vida2, p2);

        AnimarMensaje();
    }

    void ActualizarRetraso(ref float retraso, ref float espera, float vida)
    {
        if (vida >= retraso) { retraso = vida; espera = 0.5f; return; }
        espera -= Time.deltaTime;
        if (espera <= 0f) retraso = Mathf.MoveTowards(retraso, vida, Time.deltaTime * 0.6f);
    }

    static void Rellenar(RectTransform barra, float p, bool desdeDerecha)
    {
        if (barra == null) return;
        p = Mathf.Clamp01(p);
        if (desdeDerecha)
        {
            barra.anchorMin = new Vector2(1f - p, 0f);
            barra.anchorMax = Vector2.one;
        }
        else
        {
            barra.anchorMin = Vector2.zero;
            barra.anchorMax = new Vector2(p, 1f);
        }
        barra.offsetMin = barra.offsetMax = Vector2.zero;
    }

    static void ColorVida(RectTransform barra, float p)
    {
        if (barra == null) return;
        var img = barra.GetComponent<Image>();
        if (img == null) return;
        Color verde = new Color(0.25f, 0.85f, 0.35f);
        Color amarillo = new Color(1f, 0.8f, 0.15f);
        Color rojo = new Color(0.95f, 0.2f, 0.15f);
        img.color = p > 0.5f ? Color.Lerp(amarillo, verde, (p - 0.5f) * 2f) : Color.Lerp(rojo, amarillo, p * 2f);
    }

    public void ConfigurarNombres(string n1, string n2)
    {
        if (nombre1) nombre1.text = n1.ToUpper();
        if (nombre2) nombre2.text = n2.ToUpper();
    }

    public void ActualizarTiempo(float segundos)
    {
        if (textoTiempo) textoTiempo.text = Mathf.CeilToInt(Mathf.Max(0f, segundos)).ToString("00");
        if (textoTiempo) textoTiempo.color = segundos <= 10f ? new Color(1f, 0.35f, 0.3f) : Color.white;
    }

    public void ActualizarRondas(int ganadas1, int ganadas2)
    {
        for (int i = 0; i < rondas1.Length; i++) if (rondas1[i]) rondas1[i].color = i < ganadas1 ? RondaGanada : RondaVacia;
        for (int i = 0; i < rondas2.Length; i++) if (rondas2[i]) rondas2[i].color = i < ganadas2 ? RondaGanada : RondaVacia;
    }

    /// <param name="duracion">Segundos visibles. 0 = queda hasta llamar OcultarMensaje().</param>
    public void Mensaje(string texto, float duracion)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = texto;
        textoMensaje.gameObject.SetActive(true);
        duracionMensaje = duracion;
        tiempoMensaje = 0f;
    }

    public void OcultarMensaje()
    {
        if (textoMensaje) textoMensaje.gameObject.SetActive(false);
    }

    void AnimarMensaje()
    {
        if (textoMensaje == null || !textoMensaje.gameObject.activeSelf) return;
        tiempoMensaje += Time.unscaledDeltaTime;
        // Entrada con "rebote" de escala
        float e = Mathf.Clamp01(tiempoMensaje / 0.25f);
        float escala = e < 1f ? Mathf.Lerp(2.2f, 1f, 1f - (1f - e) * (1f - e)) : 1f;
        textoMensaje.transform.localScale = Vector3.one * escala;
        if (duracionMensaje > 0f && tiempoMensaje >= duracionMensaje) OcultarMensaje();
    }
}
