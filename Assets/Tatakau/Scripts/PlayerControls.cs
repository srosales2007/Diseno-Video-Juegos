using UnityEngine;

/// <summary>
/// Controles de teclado de un jugador humano. Las teclas son configurables desde el Inspector.
/// </summary>
public class PlayerControls : FighterInput
{
    [Header("Movimiento")]
    public KeyCode izquierda = KeyCode.A;
    public KeyCode derecha = KeyCode.D;
    public KeyCode saltar = KeyCode.W;

    [Header("Combate")]
    public KeyCode golpe = KeyCode.J;
    public KeyCode patada = KeyCode.K;
    public KeyCode cubrirse = KeyCode.L;

    [Header("Teclas alternativas (opcional)")]
    public KeyCode golpeAlt = KeyCode.None;
    public KeyCode patadaAlt = KeyCode.None;
    public KeyCode cubrirseAlt = KeyCode.None;

    public override void Leer(Fighter yo, Fighter rival)
    {
        float h = 0f;
        if (Input.GetKey(izquierda)) h -= 1f;
        if (Input.GetKey(derecha)) h += 1f;
        Horizontal = h;

        Saltar = Input.GetKeyDown(saltar);
        Golpe = Presiono(golpe) || Presiono(golpeAlt);
        Patada = Presiono(patada) || Presiono(patadaAlt);
        Cubrirse = Mantiene(cubrirse) || Mantiene(cubrirseAlt);
    }

    /// <summary>Configuración del jugador 2: flechas + teclado numérico (o , . -).</summary>
    public void ConfigurarJugador2()
    {
        izquierda = KeyCode.LeftArrow;
        derecha = KeyCode.RightArrow;
        saltar = KeyCode.UpArrow;
        golpe = KeyCode.Keypad1;
        patada = KeyCode.Keypad2;
        cubrirse = KeyCode.Keypad3;
        golpeAlt = KeyCode.Comma;
        patadaAlt = KeyCode.Period;
        cubrirseAlt = KeyCode.Minus;
    }

    static bool Presiono(KeyCode k) => k != KeyCode.None && Input.GetKeyDown(k);
    static bool Mantiene(KeyCode k) => k != KeyCode.None && Input.GetKey(k);
}
