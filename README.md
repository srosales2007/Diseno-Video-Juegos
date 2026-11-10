# Tatakau (戦い)

Juego de peleas 2D 1 vs 1 — Proyecto Programado de **Diseño de Videojuegos**, Universidad Fidélitas (Grupo 2).

**Motor:** Unity 2022.3.62f3 · **Lenguaje:** C# · **IDE:** Visual Studio

## Cómo abrirlo
1. Unity Hub → **Add → Add project from disk** → seleccionar esta carpeta.
2. Abrir con **Unity 2022.3.62f3**.
3. En el menú superior: **Tools → Construir Tatakau** (genera escenas, prefabs, interfaz y Build Settings).
4. Abrir `Assets/Tatakau/Scenes/MenuPrincipal.unity`, poner la vista *Game* en **16:9** y presionar **Play**.

## Contenido del Segundo Avance
- Pantalla de inicio: 1 jugador (vs IA), 2 jugadores, controles y salir.
- Escenario 1: **Laboratorio**.
- Personajes: **Erland** (jugador) y **Kenji** (IA o jugador 2).
- Movimiento, salto, golpe, patada y cubrirse.
- Barras de salud con daño retrasado, temporizador de 99 s, sistema de rondas al mejor de 3 (K.O. y Time Out).
- IA del rival basada en decisiones (acercarse, atacar, cubrirse, retroceder, saltar).
- Pausa (Continuar / Reiniciar / Menú) y pantalla de resultado (Revancha / Menú).

## Controles
| Acción | Jugador 1 | Jugador 2 |
|---|---|---|
| Moverse | A / D | ← / → |
| Saltar | W | ↑ |
| Golpe | J | Num1 o `,` |
| Patada | K | Num2 o `.` |
| Cubrirse (mantener) | L | Num3 o `-` |
| Pausa | Esc / P | Esc / P |

## Estructura
```
Assets/Tatakau/
  Scripts/     Lógica del juego (Fighter, IA, rondas, HUD, menús)
  Editor/      TatakauBuilder: construye escenas y prefabs automáticamente
  Sprites/     Personajes/ y Escenarios/
  Prefabs/     Erland, Kenji (generados)
  Scenes/      MenuPrincipal, Escenario01_Laboratorio (generadas)
  Materials/   Material físico sin fricción (generado)
  Audio/       (pendiente)
```
