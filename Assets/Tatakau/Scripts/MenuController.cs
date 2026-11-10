using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Pantalla de inicio: 1 jugador (vs IA), 2 jugadores, controles y salir.</summary>
public class MenuController : MonoBehaviour
{
    public GameObject panelPrincipal;
    public GameObject panelControles;
    public Button primerBoton;
    public Button botonVolver;

    void Start()
    {
        Time.timeScale = 1f;
        MostrarControles(false);
    }

    public void UnJugador()
    {
        GameSettings.DosJugadores = false;
        SceneManager.LoadScene(GameSettings.EscenaPelea);
    }

    public void DosJugadores()
    {
        GameSettings.DosJugadores = true;
        SceneManager.LoadScene(GameSettings.EscenaPelea);
    }

    public void AbrirControles() => MostrarControles(true);
    public void CerrarControles() => MostrarControles(false);

    void MostrarControles(bool mostrar)
    {
        if (panelControles) panelControles.SetActive(mostrar);
        if (panelPrincipal) panelPrincipal.SetActive(!mostrar);
        Button seleccionar = mostrar ? botonVolver : primerBoton;
        if (seleccionar) seleccionar.Select();
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
