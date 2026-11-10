using UnityEngine;

/// <summary>
/// Fuente de órdenes para un luchador. El luchador no sabe si lo controla una persona o la IA:
/// solo lee estas propiedades. Así el mismo personaje sirve para el modo 1 jugador y 2 jugadores.
/// </summary>
public abstract class FighterInput : MonoBehaviour
{
    public float Horizontal { get; protected set; }
    public bool Saltar { get; protected set; }
    public bool Golpe { get; protected set; }
    public bool Patada { get; protected set; }
    public bool Cubrirse { get; protected set; }

    /// <summary>Lo llama el Fighter una vez por frame, antes de leer las órdenes.</summary>
    public abstract void Leer(Fighter yo, Fighter rival);

    protected void Limpiar()
    {
        Horizontal = 0f;
        Saltar = Golpe = Patada = Cubrirse = false;
    }
}
