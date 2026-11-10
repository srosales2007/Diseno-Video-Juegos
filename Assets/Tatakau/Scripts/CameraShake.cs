using UnityEngine;

/// <summary>Sacudida de cámara al recibir golpes (retroalimentación visual del impacto).</summary>
public class CameraShake : MonoBehaviour
{
    static CameraShake instancia;
    Vector3 posicionBase;
    float intensidad;

    void Awake()
    {
        instancia = this;
        posicionBase = transform.localPosition;
    }

    public static void Sacudir(float fuerza)
    {
        if (instancia != null) instancia.intensidad = Mathf.Max(instancia.intensidad, fuerza);
    }

    void LateUpdate()
    {
        if (intensidad > 0f)
        {
            transform.localPosition = posicionBase + (Vector3)(Random.insideUnitCircle * intensidad);
            intensidad = Mathf.MoveTowards(intensidad, 0f, Time.unscaledDeltaTime * 1.2f);
        }
        else
        {
            transform.localPosition = posicionBase;
        }
    }
}
