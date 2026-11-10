using UnityEngine;

/// <summary>
/// IA simple del rival. Cada cierto tiempo (tiempo de reacción) toma una decisión según la distancia
/// al jugador, y reacciona cuando el jugador inicia un ataque cerca (intenta cubrirse).
/// </summary>
public class AIController : FighterInput
{
    public enum Decision { Esperar, Acercarse, Alejarse, Golpear, Patear, Cubrirse, Saltar }

    [Header("Personalidad")]
    [Range(0f, 1f)] public float agresividad = 0.65f;
    [Range(0f, 1f)] public float probabilidadCubrirse = 0.45f;

    [Header("Tiempo de reacción (s)")]
    public float reaccionMin = 0.18f;
    public float reaccionMax = 0.45f;

    public Decision DecisionActual { get; private set; } = Decision.Esperar;

    float temporizador;
    bool rivalAtacabaAntes;

    public override void Leer(Fighter yo, Fighter rival)
    {
        Limpiar();
        if (rival == null || !yo.PuedeActuar || rival.KO)
        {
            DecisionActual = Decision.Esperar;
            return;
        }

        float dx = rival.transform.position.x - yo.transform.position.x;
        float distancia = Mathf.Abs(dx);
        float haciaRival = Mathf.Sign(dx);

        // Reacción: el jugador acaba de empezar un ataque y está cerca
        bool rivalAtaca = rival.EstaAtacando;
        if (rivalAtaca && !rivalAtacabaAntes && distancia < yo.RangoAtaque + 0.8f && Random.value < probabilidadCubrirse)
        {
            DecisionActual = Decision.Cubrirse;
            temporizador = 0.45f;
        }
        rivalAtacabaAntes = rivalAtaca;

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f) Decidir(distancia, yo.RangoAtaque);

        switch (DecisionActual)
        {
            case Decision.Acercarse:
                Horizontal = haciaRival;
                if (distancia <= yo.RangoAtaque) temporizador = 0f; // ya llegó: decidir otra vez
                break;
            case Decision.Alejarse:
                Horizontal = -haciaRival;
                break;
            case Decision.Cubrirse:
                Cubrirse = true;
                break;
            case Decision.Golpear:
                Golpe = true;
                DecisionActual = Decision.Esperar;
                break;
            case Decision.Patear:
                Patada = true;
                DecisionActual = Decision.Esperar;
                break;
            case Decision.Saltar:
                Saltar = true;
                Horizontal = haciaRival;
                DecisionActual = Decision.Acercarse;
                break;
        }
    }

    void Decidir(float distancia, float rango)
    {
        temporizador = Random.Range(reaccionMin, reaccionMax);
        float r = Random.value;

        if (distancia > rango)
        {
            if (r < 0.07f) DecisionActual = Decision.Saltar;
            else if (r < 0.35f + agresividad * 0.6f) DecisionActual = Decision.Acercarse;
            else DecisionActual = Decision.Esperar;
            return;
        }

        float golpe = 0.25f + agresividad * 0.2f;
        float patada = golpe + 0.2f;
        float cubrirse = patada + 0.15f;
        float alejarse = cubrirse + 0.12f;

        if (r < golpe) DecisionActual = Decision.Golpear;
        else if (r < patada) DecisionActual = Decision.Patear;
        else if (r < cubrirse) DecisionActual = Decision.Cubrirse;
        else if (r < alejarse) DecisionActual = Decision.Alejarse;
        else DecisionActual = Decision.Esperar;
    }
}
