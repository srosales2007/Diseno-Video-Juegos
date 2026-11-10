using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Construye todo el proyecto con un clic: Tools > Construir Tatakau.
/// Configura la importación de sprites, crea capas, materiales, prefabs, las dos escenas
/// (menú y laboratorio) con su interfaz, y registra las escenas en Build Settings.
/// Se puede volver a ejecutar cuando se quiera: regenera escenas y prefabs.
/// </summary>
public static class TatakauBuilder
{
    const string Raiz = "Assets/Tatakau";
    const string RutaSprites = Raiz + "/Sprites";
    const string RutaEscenas = Raiz + "/Scenes";
    const string RutaPrefabs = Raiz + "/Prefabs";
    const string RutaMateriales = Raiz + "/Materials";

    // Las tiras de sprites vienen normalizadas en celdas de 460 x 440 px
    const int CeldaAncho = 460;
    const int CeldaAlto = 440;
    const float PpuPersonajes = 110f;
    const float PisoY = -3.6f;

    static Font fuente;

    [MenuItem("Tools/Construir Tatakau")]
    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var carpeta in new[] { RutaEscenas, RutaPrefabs, RutaMateriales }) Directory.CreateDirectory(carpeta);
        AssetDatabase.Refresh();

        int capaSuelo = AsegurarCapa("Suelo");
        int capaLuchador = AsegurarCapa("Luchador");

        foreach (var hoja in new[] { "Erland_Idle", "Erland_Walk", "Erland_Jump", "Kenji_Idle" })
            ImportarTira($"{RutaSprites}/Personajes/{hoja}.png");
        ImportarFondo($"{RutaSprites}/Escenarios/Laboratorio.png");
        foreach (var pose in new[] { "Erland_Golpe", "Erland_Patada", "Kenji_Golpe", "Kenji_Patada" })
            ImportarPose($"{RutaSprites}/Personajes/{pose}.png");

        var sinFriccion = MaterialSinFriccion();

        var erland = CrearPrefabLuchador("Erland", true, Cargar("Erland_Idle"), Cargar("Erland_Walk"), Cargar("Erland_Jump"),
            CargarPose("Erland_Golpe"), CargarPose("Erland_Patada"), capaSuelo, capaLuchador, sinFriccion, false);
        var kenji = CrearPrefabLuchador("Kenji", false, Cargar("Kenji_Idle"), null, null,
            CargarPose("Kenji_Golpe"), CargarPose("Kenji_Patada"), capaSuelo, capaLuchador, sinFriccion, true);

        string menu = CrearMenu();
        string nivel = CrearNivel(erland, kenji, capaSuelo);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(menu, true),
            new EditorBuildSettingsScene(nivel, true),
        };

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(menu);
        EditorUtility.DisplayDialog("Tatakau", "Proyecto construido.\n\nEscena abierta: MenuPrincipal.\nPoné la vista Game en 16:9 y presioná Play.", "OK");
    }

    // ------------------------------------------------------------------ Importación

    static void ImportarTira(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("Tatakau: no se encontró " + ruta); return; }

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = PpuPersonajes;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = 8192;

        int frames = Mathf.Max(1, LeerTamanoPng(ruta).x / CeldaAncho);
        string nombre = Path.GetFileNameWithoutExtension(ruta);
        var metas = new SpriteMetaData[frames];
        for (int i = 0; i < frames; i++)
        {
            metas[i] = new SpriteMetaData
            {
                name = nombre + "_" + i,
                rect = new Rect(i * CeldaAncho, 0, CeldaAncho, CeldaAlto),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f),
            };
        }
#pragma warning disable 618
        imp.spritesheet = metas;
#pragma warning restore 618
        imp.SaveAndReimport();
    }

    static void ImportarFondo(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("Tatakau: no se encontró " + ruta); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = LeerTamanoPng(ruta).y / 10f; // alto de la imagen = alto de la cámara (10 unidades)
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 4096;
        imp.SaveAndReimport();
    }

    /// <summary>Pose de ataque: un solo sprite con el pivote en los pies.</summary>
    static void ImportarPose(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogWarning("Tatakau: falta la pose " + ruta + " (se usará la animación procedural)"); return; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = PpuPersonajes;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        var ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        ajustes.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
    }

    static Sprite CargarPose(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>($"{RutaSprites}/Personajes/{nombre}.png");

    static Vector2Int LeerTamanoPng(string ruta)
    {
        byte[] b = File.ReadAllBytes(ruta);
        int ancho = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int alto = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        return new Vector2Int(ancho, alto);
    }

    static Sprite[] Cargar(string hoja)
    {
        return AssetDatabase.LoadAllAssetsAtPath($"{RutaSprites}/Personajes/{hoja}.png")
            .OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    static Sprite CargarFondo() => AssetDatabase.LoadAssetAtPath<Sprite>($"{RutaSprites}/Escenarios/Laboratorio.png");

    static int AsegurarCapa(string nombre)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0) return existente;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var capas = tagManager.FindProperty("layers");
        for (int i = 8; i < capas.arraySize; i++)
        {
            var capa = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(capa.stringValue))
            {
                capa.stringValue = nombre;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        Debug.LogError("Tatakau: no hay capas libres para " + nombre);
        return 0;
    }

    static PhysicsMaterial2D MaterialSinFriccion()
    {
        string ruta = RutaMateriales + "/SinFriccion.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ruta);
        if (mat != null) return mat;
        mat = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // ------------------------------------------------------------------ Prefabs

    static GameObject CrearPrefabLuchador(string nombre, bool miraDerecha, Sprite[] quieto, Sprite[] caminar, Sprite[] salto,
        Sprite golpe, Sprite patada, int capaSuelo, int capaLuchador, PhysicsMaterial2D material, bool esRival)
    {
        var raiz = new GameObject(nombre) { layer = capaLuchador };

        var rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = raiz.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.6f, 3.0f);
        col.offset = new Vector2(0f, 1.5f);
        col.sharedMaterial = material;

        var visual = new GameObject("Sprite") { layer = capaLuchador };
        visual.transform.SetParent(raiz.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = quieto.Length > 0 ? quieto[0] : null;
        sr.sortingOrder = esRival ? 9 : 10;
        var anim = visual.AddComponent<SpriteAnimator>();
        anim.quieto = quieto;
        anim.caminar = caminar ?? new Sprite[0];
        anim.salto = salto ?? new Sprite[0];
        anim.cuerpo = rb;
        anim.golpe = golpe;
        anim.patada = patada;

        var pies = new GameObject("Pies").transform;
        pies.SetParent(raiz.transform, false);
        pies.localPosition = new Vector3(0f, 0.02f, 0f);

        var teclado = raiz.AddComponent<PlayerControls>();
        FighterInput entrada = teclado;
        if (esRival)
        {
            teclado.ConfigurarJugador2();
            entrada = raiz.AddComponent<AIController>();
        }

        var f = raiz.AddComponent<Fighter>();
        f.nombre = nombre;
        f.spriteMiraDerecha = miraDerecha;
        f.sprite = sr;
        f.animador = anim;
        f.entrada = entrada;
        f.pies = pies;
        f.capaSuelo = 1 << capaSuelo;
        if (esRival)
        {
            // Kenji: un poco más lento; el corte de katana tiene más alcance que un puño
            f.velocidad = 4.2f;
            f.alcanceGolpe = 2.0f;
            f.danoPatada = 11f;
            f.alcancePatada = 2.0f;
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, $"{RutaPrefabs}/{nombre}.prefab");
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ------------------------------------------------------------------ Escena: menú

    static string CrearMenu()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearCamara();
        CrearFondo(new Color(0.42f, 0.45f, 0.5f));

        // Personajes decorativos a los lados
        Decorativo("Erland", Cargar("Erland_Idle"), new Vector3(-5.6f, PisoY, 0f), false);
        Decorativo("Kenji", Cargar("Kenji_Idle"), new Vector3(5.6f, PisoY, 0f), false);

        var canvas = CrearCanvas("Canvas");
        var eventos = CrearEventSystem();
        var menu = canvas.gameObject.AddComponent<MenuController>();

        var principal = Contenedor("PanelPrincipal", canvas.transform);
        Texto("Titulo", principal, "TATAKAU", 190, Centro, new Vector2(0, 330), new Vector2(1400, 230), TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.22f), 6);
        Texto("Subtitulo", principal, "El torneo del más fuerte", 42, Centro, new Vector2(0, 205), new Vector2(1200, 60), TextAnchor.MiddleCenter, Color.white, 3);
        var b1 = Boton("Boton1Jugador", principal, "1 JUGADOR  (vs IA)", new Vector2(0, 70), menu.UnJugador);
        Boton("Boton2Jugadores", principal, "2 JUGADORES", new Vector2(0, -40), menu.DosJugadores);
        Boton("BotonControles", principal, "CONTROLES", new Vector2(0, -150), menu.AbrirControles);
        Boton("BotonSalir", principal, "SALIR", new Vector2(0, -260), menu.Salir);
        Texto("Creditos", principal, "Diseño de Videojuegos · Universidad Fidélitas · Grupo 2", 26, new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(1600, 40), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.75f), 2);

        var controles = Panel("PanelControles", canvas.transform, Centro, Vector2.zero, new Vector2(1250, 780), new Color(0.04f, 0.05f, 0.08f, 0.93f));
        Texto("TituloControles", controles.transform, "CONTROLES", 70, Centro, new Vector2(0, 310), new Vector2(1100, 90), TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.22f), 4);
        Texto("ListaControles", controles.transform,
            "<b>JUGADOR 1 — ERLAND</b>\n" +
            "A / D  moverse        W  saltar\n" +
            "J  golpe        K  patada        L  cubrirse (mantener)\n\n" +
            "<b>JUGADOR 2 — KENJI</b>  (modo 2 jugadores)\n" +
            "Flechas  moverse / saltar\n" +
            "Num1 o ,  golpe      Num2 o .  patada      Num3 o -  cubrirse\n\n" +
            "Esc / P  pausa\n\n" +
            "<size=28>Al mejor de 3 rondas · 99 segundos por ronda · si se acaba el tiempo gana quien tenga más vida</size>",
            34, Centro, new Vector2(0, 10), new Vector2(1150, 480), TextAnchor.MiddleCenter, Color.white, 2);
        var volver = Boton("BotonVolver", controles.transform, "VOLVER", new Vector2(0, -310), menu.CerrarControles);

        menu.panelPrincipal = principal.gameObject;
        menu.panelControles = controles.gameObject;
        menu.primerBoton = b1;
        menu.botonVolver = volver;
        eventos.firstSelectedGameObject = b1.gameObject;

        string ruta = RutaEscenas + "/MenuPrincipal.unity";
        EditorSceneManager.SaveScene(escena, ruta);
        return ruta;
    }

    // ------------------------------------------------------------------ Escena: laboratorio

    static string CrearNivel(GameObject prefabErland, GameObject prefabKenji, int capaSuelo)
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = CrearCamara();
        cam.gameObject.AddComponent<CameraShake>();
        CrearFondo(Color.white);

        var suelo = new GameObject("Suelo") { layer = capaSuelo };
        suelo.transform.position = new Vector3(0f, PisoY - 1f, 0f);
        suelo.AddComponent<BoxCollider2D>().size = new Vector2(40f, 2f);

        // Límites del escenario (el GDD pide poder arrinconar al rival)
        Pared("ParedIzquierda", -9.1f);
        Pared("ParedDerecha", 9.1f);

        var inicio1 = new GameObject("Inicio_Jugador1").transform;
        inicio1.position = new Vector3(-4f, PisoY + 0.05f, 0f);
        var inicio2 = new GameObject("Inicio_Jugador2").transform;
        inicio2.position = new Vector3(4f, PisoY + 0.05f, 0f);

        var erland = (GameObject)PrefabUtility.InstantiatePrefab(prefabErland);
        erland.transform.position = inicio1.position;
        var kenji = (GameObject)PrefabUtility.InstantiatePrefab(prefabKenji);
        kenji.transform.position = inicio2.position;
        var f1 = erland.GetComponent<Fighter>();
        var f2 = kenji.GetComponent<Fighter>();
        f1.rival = f2;
        f2.rival = f1;

        // ---------- HUD
        var canvas = CrearCanvas("HUD");
        CrearEventSystem();
        var hud = canvas.gameObject.AddComponent<HUD>();
        hud.jugador1 = f1;
        hud.jugador2 = f2;

        BarraVida("BarraJ1", canvas.transform, new Vector2(0f, 1f), new Vector2(450f, -75f), out hud.vida1, out hud.retraso1);
        BarraVida("BarraJ2", canvas.transform, new Vector2(1f, 1f), new Vector2(-450f, -75f), out hud.vida2, out hud.retraso2);
        hud.nombre1 = Texto("NombreJ1", canvas.transform, "ERLAND", 38, new Vector2(0f, 1f), new Vector2(450f, -128f), new Vector2(780f, 50f), TextAnchor.MiddleLeft, Color.white, 3);
        hud.nombre2 = Texto("NombreJ2", canvas.transform, "KENJI", 38, new Vector2(1f, 1f), new Vector2(-450f, -128f), new Vector2(780f, 50f), TextAnchor.MiddleRight, Color.white, 3);
        hud.rondas1 = new[]
        {
            Panel("RondaJ1_1", canvas.transform, new Vector2(0f, 1f), new Vector2(822f, -128f), new Vector2(26f, 26f), Color.gray),
            Panel("RondaJ1_2", canvas.transform, new Vector2(0f, 1f), new Vector2(786f, -128f), new Vector2(26f, 26f), Color.gray),
        };
        hud.rondas2 = new[]
        {
            Panel("RondaJ2_1", canvas.transform, new Vector2(1f, 1f), new Vector2(-822f, -128f), new Vector2(26f, 26f), Color.gray),
            Panel("RondaJ2_2", canvas.transform, new Vector2(1f, 1f), new Vector2(-786f, -128f), new Vector2(26f, 26f), Color.gray),
        };
        var marcoTiempo = Panel("MarcoTiempo", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(150f, 112f), new Color(0f, 0f, 0f, 0.85f));
        hud.textoTiempo = Texto("Tiempo", marcoTiempo.transform, "99", 82, Centro, Vector2.zero, new Vector2(150f, 112f), TextAnchor.MiddleCenter, Color.white, 3);
        hud.textoMensaje = Texto("Mensaje", canvas.transform, "", 140, Centro, new Vector2(0f, 120f), new Vector2(1800f, 220f), TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.25f), 7);
        hud.textoAyuda = Texto("Ayuda", canvas.transform, "", 26, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1900f, 40f), TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f), 2);

        // ---------- Lógica
        var gm = new GameObject("GameManager");
        var rondas = gm.AddComponent<RoundManager>();
        rondas.jugador1 = f1;
        rondas.jugador2 = f2;
        rondas.inicio1 = inicio1;
        rondas.inicio2 = inicio2;
        rondas.hud = hud;
        var pausa = gm.AddComponent<PauseMenu>();
        pausa.partida = rondas;

        // ---------- Pausa
        var panelPausa = Panel("PanelPausa", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        Estirar(panelPausa.rectTransform);
        Texto("TituloPausa", panelPausa.transform, "PAUSA", 120, Centro, new Vector2(0f, 230f), new Vector2(1000f, 160f), TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.22f), 6);
        Boton("BotonContinuar", panelPausa.transform, "CONTINUAR", new Vector2(0f, 50f), pausa.Continuar);
        Boton("BotonReiniciar", panelPausa.transform, "REINICIAR PELEA", new Vector2(0f, -60f), pausa.Reiniciar);
        Boton("BotonMenuPausa", panelPausa.transform, "MENÚ PRINCIPAL", new Vector2(0f, -170f), pausa.IrAlMenu);
        pausa.panel = panelPausa.gameObject;

        // ---------- Resultado
        var panelResultado = Panel("PanelResultado", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.78f));
        Estirar(panelResultado.rectTransform);
        rondas.textoResultado = Texto("TextoResultado", panelResultado.transform, "¡VICTORIA!", 120, Centro, new Vector2(0f, 190f), new Vector2(1600f, 300f), TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.22f), 6);
        Boton("BotonRevancha", panelResultado.transform, "REVANCHA", new Vector2(0f, -60f), rondas.Revancha);
        Boton("BotonMenuResultado", panelResultado.transform, "MENÚ PRINCIPAL", new Vector2(0f, -170f), rondas.VolverAlMenu);
        rondas.panelResultado = panelResultado.gameObject;

        string ruta = RutaEscenas + "/Escenario01_Laboratorio.unity";
        EditorSceneManager.SaveScene(escena, ruta);
        return ruta;
    }

    // ------------------------------------------------------------------ Utilidades de escena

    static Camera CrearCamara()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
        return cam;
    }

    static void CrearFondo(Color tinte)
    {
        var go = new GameObject("Fondo_Laboratorio");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CargarFondo();
        sr.color = tinte;
        sr.sortingOrder = -10;
    }

    static void Decorativo(string nombre, Sprite[] frames, Vector3 posicion, bool voltear)
    {
        var go = new GameObject(nombre);
        go.transform.position = posicion;
        go.transform.localScale = Vector3.one * 1.15f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames.Length > 0 ? frames[0] : null;
        sr.flipX = voltear;
        var anim = go.AddComponent<SpriteAnimator>();
        anim.quieto = frames;
    }

    static void Pared(string nombre, float x)
    {
        var go = new GameObject(nombre);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 30f);
    }

    // ------------------------------------------------------------------ Utilidades de UI

    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);

    static Canvas CrearCanvas(string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static EventSystem CrearEventSystem()
    {
        var go = new GameObject("EventSystem");
        var eventos = go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        return eventos;
    }

    static RectTransform Rect(string nombre, Transform padre, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = Centro;
        r.anchoredPosition = posicion;
        r.sizeDelta = tamano;
        return r;
    }

    static void Estirar(RectTransform r, float margen = 0f)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(margen, margen);
        r.offsetMax = new Vector2(-margen, -margen);
    }

    static RectTransform Contenedor(string nombre, Transform padre)
    {
        var r = Rect(nombre, padre, Centro, Vector2.zero, Vector2.zero);
        Estirar(r);
        return r;
    }

    static Image Panel(string nombre, Transform padre, Vector2 ancla, Vector2 posicion, Vector2 tamano, Color color)
    {
        var img = Rect(nombre, padre, ancla, posicion, tamano).gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Text Texto(string nombre, Transform padre, string contenido, int tamano, Vector2 ancla, Vector2 posicion, Vector2 caja,
        TextAnchor alineacion, Color color, float sombra)
    {
        var r = Rect(nombre, padre, ancla, posicion, caja);
        var t = r.gameObject.AddComponent<Text>();
        t.font = fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.fontStyle = FontStyle.Bold;
        t.alignment = alineacion;
        t.color = color;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        if (sombra > 0f)
        {
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(sombra, -sombra);
        }
        return t;
    }

    static Button Boton(string nombre, Transform padre, string texto, Vector2 posicion, UnityAction accion)
    {
        var img = Panel(nombre, padre, Centro, posicion, new Vector2(580f, 90f), Color.white);
        var borde = img.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(1f, 0.65f, 0.2f, 0.8f);
        borde.effectDistance = new Vector2(3f, 3f);

        var b = img.gameObject.AddComponent<Button>();
        var colores = b.colors;
        colores.normalColor = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        colores.highlightedColor = new Color(0.85f, 0.45f, 0.1f, 1f);
        colores.selectedColor = new Color(0.85f, 0.45f, 0.1f, 1f);
        colores.pressedColor = new Color(1f, 0.62f, 0.2f, 1f);
        colores.fadeDuration = 0.08f;
        b.colors = colores;

        var t = Texto("Texto", img.transform, texto, 40, Centro, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, Color.white, 2);
        Estirar(t.rectTransform);

        UnityEventTools.AddPersistentListener(b.onClick, accion);
        return b;
    }

    static void BarraVida(string nombre, Transform padre, Vector2 ancla, Vector2 posicion, out RectTransform vida, out RectTransform retraso)
    {
        var marco = Panel(nombre, padre, ancla, posicion, new Vector2(790f, 54f), new Color(0f, 0f, 0f, 0.85f));
        var fondo = Panel("Fondo", marco.transform, Centro, Vector2.zero, Vector2.zero, new Color(0.3f, 0.05f, 0.05f, 1f));
        Estirar(fondo.rectTransform, 5f);
        var r = Panel("Retraso", fondo.transform, Centro, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.9f));
        Estirar(r.rectTransform);
        var v = Panel("Vida", fondo.transform, Centro, Vector2.zero, Vector2.zero, new Color(0.25f, 0.85f, 0.35f));
        Estirar(v.rectTransform);
        vida = v.rectTransform;
        retraso = r.rectTransform;
    }
}
