using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Pausa con Esc o P: congela el tiempo y muestra Continuar / Reiniciar / Menú.</summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject panel;
    public RoundManager partida;

    bool pausado;

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) && !Input.GetKeyDown(KeyCode.P)) return;
        if (!pausado && partida != null && !partida.EnPelea) return; // solo durante la pelea
        Alternar();
    }

    public void Alternar()
    {
        pausado = !pausado;
        if (panel)
        {
            panel.SetActive(pausado);
            var boton = panel.GetComponentInChildren<Button>();
            if (pausado && boton) boton.Select();
        }
        Time.timeScale = pausado ? 0f : 1f;
    }

    public void Continuar()
    {
        if (pausado) Alternar();
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSettings.EscenaMenu);
    }
}
