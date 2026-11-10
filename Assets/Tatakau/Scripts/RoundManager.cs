using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sistema de rondas del GDD: combate al mejor de 3 (gana quien llegue a 2 rondas),
/// 99 segundos por ronda. Si se acaba el tiempo gana quien tenga más salud (Time Out).
/// </summary>
public class RoundManager : MonoBehaviour
{
    [Header("Luchadores")]
    public Fighter jugador1;
    public Fighter jugador2;
    public Transform inicio1;
    public Transform inicio2;

    [Header("Reglas")]
    public int rondasParaGanar = 2;
    public float tiempoRonda = 99f;

    [Header("Interfaz")]
    public HUD hud;
    public GameObject panelResultado;
    public Text textoResultado;

    public bool EnPelea { get; private set; }
    public bool PartidaTerminada { get; private set; }

    int victorias1, victorias2, ronda;
    float tiempo;

    void Awake()
    {
        Time.timeScale = 1f;
    }

    void Start()
    {
        jugador1.rival = jugador2;
        jugador2.rival = jugador1;
        ConfigurarModo();
        if (panelResultado) panelResultado.SetActive(false);
        StartCoroutine(Partida());
    }

    void ConfigurarModo()
    {
        bool dos = GameSettings.DosJugadores;
        var ia = jugador2.GetComponent<AIController>();
        var teclado2 = jugador2.GetComponent<PlayerControls>();
        jugador2.entrada = dos ? (FighterInput)teclado2 : ia;
        if (jugador2.entrada == null) jugador2.entrada = ia != null ? (FighterInput)ia : teclado2;

        hud.ConfigurarNombres(jugador1.nombre, jugador2.nombre + (dos ? "" : " (IA)"));
        if (hud.textoAyuda)
        {
            hud.textoAyuda.text = dos
                ? "J1: A/D mover · W saltar · J golpe · K patada · L cubrirse      J2: Flechas mover/saltar · Num1/, golpe · Num2/. patada · Num3/- cubrirse      Esc: pausa"
                : "A/D mover · W saltar · J golpe · K patada · L cubrirse · Esc pausa";
        }
    }

    IEnumerator Partida()
    {
        while (victorias1 < rondasParaGanar && victorias2 < rondasParaGanar)
        {
            ronda++;
            yield return Ronda();
        }

        PartidaTerminada = true;
        Fighter ganador = victorias1 > victorias2 ? jugador1 : jugador2;
        hud.Mensaje(ganador.nombre.ToUpper() + " GANA", 0f);
        yield return new WaitForSeconds(1.8f);
        hud.OcultarMensaje();

        if (textoResultado)
        {
            bool ganoJugador = ganador == jugador1 || GameSettings.DosJugadores;
            string titulo = GameSettings.DosJugadores ? "¡" + ganador.nombre.ToUpper() + " GANA!" : (ganoJugador ? "¡VICTORIA!" : "DERROTA");
            textoResultado.text = titulo + "\n<size=48>" + jugador1.nombre + "  " + victorias1 + "  -  " + victorias2 + "  " + jugador2.nombre + "</size>";
        }
        if (panelResultado)
        {
            panelResultado.SetActive(true);
            var boton = panelResultado.GetComponentInChildren<Button>();
            if (boton) boton.Select();
        }
    }

    IEnumerator Ronda()
    {
        jugador1.Reiniciar(inicio1.position);
        jugador2.Reiniciar(inicio2.position);
        jugador1.ActivarControl(false);
        jugador2.ActivarControl(false);
        tiempo = tiempoRonda;
        hud.ActualizarTiempo(tiempo);
        hud.ActualizarRondas(victorias1, victorias2);

        hud.Mensaje(victorias1 == rondasParaGanar - 1 && victorias2 == rondasParaGanar - 1 ? "RONDA FINAL" : "RONDA " + ronda, 1.2f);
        yield return new WaitForSeconds(1.3f);
        hud.Mensaje("¡PELEA!", 0.8f);
        yield return new WaitForSeconds(0.3f);

        jugador1.ActivarControl(true);
        jugador2.ActivarControl(true);
        EnPelea = true;

        while (!jugador1.KO && !jugador2.KO && tiempo > 0f)
        {
            tiempo -= Time.deltaTime;
            hud.ActualizarTiempo(tiempo);
            yield return null;
        }

        EnPelea = false;
        jugador1.ActivarControl(false);
        jugador2.ActivarControl(false);

        bool porKO = jugador1.KO || jugador2.KO;
        int ganador;
        if (porKO) ganador = jugador1.KO && jugador2.KO ? 0 : (jugador2.KO ? 1 : 2);
        else ganador = jugador1.Vida > jugador2.Vida ? 1 : (jugador2.Vida > jugador1.Vida ? 2 : 0);

        if (ganador == 1) victorias1++;
        else if (ganador == 2) victorias2++;

        hud.Mensaje(porKO ? "K.O." : "¡TIEMPO!", 1.4f);
        if (porKO)
        {
            // Cámara lenta en el K.O.
            Time.timeScale = 0.35f;
            yield return new WaitForSecondsRealtime(0.9f);
            Time.timeScale = 1f;
        }
        else
        {
            yield return new WaitForSeconds(1.2f);
        }

        hud.ActualizarRondas(victorias1, victorias2);
        hud.Mensaje(ganador == 0 ? "EMPATE" : (ganador == 1 ? jugador1 : jugador2).nombre.ToUpper() + " GANA LA RONDA", 1.4f);
        yield return new WaitForSeconds(1.8f);
    }

    public void Revancha()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSettings.EscenaMenu);
    }
}
