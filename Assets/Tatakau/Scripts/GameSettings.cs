/// <summary>
/// Configuración compartida entre escenas (se mantiene al cambiar de escena por ser estática).
/// </summary>
public static class GameSettings
{
    public const string EscenaMenu = "MenuPrincipal";
    public const string EscenaPelea = "Escenario01_Laboratorio";

    /// <summary>false = Erland vs Kenji controlado por la IA. true = dos jugadores en el mismo teclado.</summary>
    public static bool DosJugadores = false;
}
