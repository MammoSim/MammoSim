// COLOCAR EN: Assets/Editor/MammoSimSceneSetup.cs   (crea la carpeta "Editor" si no existe)
// Uso: menú superior  MammoSim > Configurar escena de simulación
// Requisitos en la escena: CanvasHUD, Lab y la Main Camera (con tag MainCamera).
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MammoSimSceneSetup
{
    const float AlturaRobot = 0.9f;   // metros

    static readonly Color Navy = new Color(0.043f, 0.063f, 0.20f, 0.92f);
    static readonly Color Navy2 = new Color(0.086f, 0.125f, 0.42f, 0.92f);
    static readonly Color Pink = new Color(0.91f, 0.12f, 0.55f, 1f);
    static readonly Color OptionBlue = new Color(0.12f, 0.17f, 0.47f, 1f);

    [MenuItem("MammoSim/Configurar escena de simulación")]
    static void Configurar()
    {
        var hudGo = GameObject.Find("CanvasHUD");
        var lab = GameObject.Find("Lab");
        var principal = Camera.main;
        if (hudGo == null || lab == null || principal == null)
        {
            Aviso("Necesito en la escena abierta: CanvasHUD, Lab y la Main Camera (con tag MainCamera).");
            return;
        }

        if (!EditorUtility.DisplayDialog("MammoSim",
                "Voy a:\n" +
                "• APAGAR (no borrar) el catálogo, la navegación y el panel de usuario, que pasan a React.\n" +
                "• Reconstruir SimulationPanel como overlay (preguntas + tutor) sobre el 3D.\n" +
                "• Configurar Lab, el robot y la cámara orbital en la Main Camera.\n\n¿Continuar?",
                "Sí, continuar", "Cancelar"))
            return;

        var hud = hudGo.GetComponent<RectTransform>();

        Limpiar(hud, lab, principal);
        var puente = PrepararManagers();
        ConfigurarLaboratorio(lab, principal);
        var pantalla = ConstruirOverlay(hud);
        Asignar(puente, "pantalla", pantalla);

        pantalla.gameObject.SetActive(false);   // arranca apagado: WebBridge / SimulationScreen lo encienden
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = principal.gameObject;

        Aviso("Escena configurada.\n\n" +
              "Ahora, en la Scene:\n" +
              "1) Mueve MammoAnchor a donde flotará el modelo (círculo rosa).\n" +
              "2) Mueve RobotAnchor al lado de la mesa (caja cian).\n" +
              "3) Ajusta OrbitTarget (centro de giro) y deja la Main Camera con la vista inicial que quieras.\n\n" +
              "Para ver el overlay en el editor, marca la casilla de SimulationPanel; al pulsar Play se enciende solo.\n" +
              "Guarda con Ctrl+S.");
    }

    // ============================================================
    //  Limpieza de lo anterior
    // ============================================================
    static void Limpiar(RectTransform hud, GameObject lab, Camera principal)
    {
        var viejo = hud.Find("SimulationPanel");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        foreach (Transform hijo in hud) hijo.gameObject.SetActive(false);   // catálogo / navegación / usuario -> React

        var caseCanvas = GameObject.Find("CaseCanvas");
        if (caseCanvas != null) caseCanvas.SetActive(false);

        // Restos de la idea anterior (Render Texture): cámara extra y máscara de capas.
        var camVieja = lab.transform.Find("LabCamera");
        if (camVieja != null) Undo.DestroyObjectImmediate(camVieja.gameObject);
        Undo.RecordObject(principal, "Main Camera ve todo");
        principal.cullingMask = ~0;
        EditorUtility.SetDirty(principal);
    }

    // ============================================================
    //  GameManager, ApiClient y WebBridge
    // ============================================================
    static WebBridge PrepararManagers()
    {
        var api = ObtenerOCrear<ApiClient>("ApiClient");
        var gm = ObtenerOCrear<GameManager>("GameManager");
        Asignar(gm, "api", api);
        return ObtenerOCrear<WebBridge>("WebBridge");
    }

    static T ObtenerOCrear<T>(string nombre) where T : Component
    {
        var go = GameObject.Find(nombre);
        if (go == null)
        {
            go = new GameObject(nombre);
            Undo.RegisterCreatedObjectUndo(go, nombre);
        }
        var c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    // ============================================================
    //  Laboratorio 3D: anclas, robot, coordinador y cámara orbital
    // ============================================================
    static void ConfigurarLaboratorio(GameObject lab, Camera principal)
    {
        Vector3 posMamo, posRobot;
        if (BuscarMesa(lab, out Bounds mesa))
        {
            posMamo = new Vector3(mesa.center.x, mesa.max.y + 0.45f, mesa.center.z);
            posRobot = new Vector3(mesa.max.x + 0.9f, mesa.min.y, mesa.center.z);
        }
        else
        {
            posMamo = lab.transform.TransformPoint(0f, 1.3f, 0f);
            posRobot = lab.transform.TransformPoint(1.4f, 0f, 0f);
            Debug.LogWarning("[MammoSim] No encontré un objeto 'mesa/table' dentro de Lab: puse las anclas cerca del origen. Muévelas a mano.");
        }

        var ancla = HijoVacio(lab.transform, "MammoAnchor", posMamo);
        var anclaRobot = HijoVacio(lab.transform, "RobotAnchor", posRobot);
        var centro = HijoVacio(lab.transform, "OrbitTarget", Vector3.Lerp(posMamo, posRobot + Vector3.up * 0.5f, 0.5f));

        // ---- Robot (FBX de Meshy) ----
        Transform robotT = anclaRobot.Find("Robot");
        if (robotT == null)
        {
            var guids = AssetDatabase.FindAssets("Meshy_AI_Happy_Little_Bot t:Model");
            if (guids.Length > 0)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, anclaRobot);
                Undo.RegisterCreatedObjectUndo(inst, "Robot");
                inst.name = "Robot";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                if (Limites(inst, out Bounds b) && b.size.y > 0.0001f)
                {
                    inst.transform.localScale *= AlturaRobot / b.size.y;
                    if (Limites(inst, out b)) inst.transform.position += new Vector3(0f, anclaRobot.position.y - b.min.y, 0f);
                }
                robotT = inst.transform;
            }
            else Debug.LogWarning("[MammoSim] No encontré el FBX del robot en Assets. Impórtalo y vuelve a ejecutar este menú.");
        }

        RobotTutor robot = null;
        if (robotT != null)
        {
            if (!robotT.TryGetComponent(out robot)) robot = Undo.AddComponent<RobotTutor>(robotT.gameObject);
            Asignar(robot, "objetivoMirada", ancla);
        }

        // ---- Coordinador del laboratorio ----
        if (!lab.TryGetComponent(out LabSceneManager mgr)) mgr = Undo.AddComponent<LabSceneManager>(lab);
        Asignar(mgr, "anclaMamografia", ancla);
        if (robot != null) Asignar(mgr, "robot", robot);

        // ---- Cámara orbital en la Main Camera ----
        Undo.RecordObject(principal.transform, "Vista inicial");
        principal.transform.position = centro.position + new Vector3(0f, 0.5f, -3.8f);
        principal.transform.LookAt(centro.position);

        if (!principal.TryGetComponent(out OrbitCamera orbita)) orbita = Undo.AddComponent<OrbitCamera>(principal.gameObject);
        Asignar(orbita, "objetivo", centro);
        Asignar(orbita, "anclaMamografia", ancla);
        Asignar(orbita, "robot", robotT != null ? robotT : anclaRobot);
    }

    // ============================================================
    //  Overlay de la simulación (UI sobre el 3D)
    // ============================================================
    static SimulationScreen ConstruirOverlay(RectTransform hud)
    {
        // Contenedor transparente a pantalla completa: sin Image, así no bloquea el mouse sobre el 3D.
        var panel = Nodo("SimulationPanel", hud, V(0, 0), V(1, 1), 0);
        panel.SetAsLastSibling();

        // ---- Tarjeta del caso (arriba a la izquierda) ----
        var tarjeta = Panel("CaseInfoCard", panel, V(0.02f, 0.74f), V(0.26f, 0.98f), Navy, 0).rectTransform;
        var numeroCaso = Texto("CaseNumberText", tarjeta, V(0.06f, 0.62f), V(0.94f, 0.95f), "Caso #001", 16, 30, Color.white, TextAlignmentOptions.Left, true);
        var datosPaciente = Texto("PatientText", tarjeta, V(0.06f, 0.38f), V(0.94f, 0.62f), "Edad / mama", 11, 18, Color.white, TextAlignmentOptions.TopLeft, false);
        var datosEstudio = Texto("StudyDataText", tarjeta, V(0.06f, 0.04f), V(0.94f, 0.38f), "...", 11, 18, new Color(1f, 0.48f, 0.75f), TextAlignmentOptions.TopLeft, true);

        // ---- Burbujas que CRECEN con el texto (sobre el panel de preguntas, a la derecha) ----
        var tutor = Burbuja("TutorPanel", panel, Pink, "Agente tutor", out var textoTutor);
        var retro = Burbuja("FeedbackPanel", panel, Navy2, "Retroalimentación", out var textoRetro);

        // ---- Panel de preguntas (abajo) ----
        var qp = Panel("QuestionPanel", panel, V(0.02f, 0.02f), V(0.98f, 0.30f), Navy, 0).rectTransform;
        var progreso = Texto("ProgressText", qp, V(0.02f, 0.84f), V(0.72f, 0.98f), "Pregunta 1 de 8", 12, 22, new Color(1f, 0.48f, 0.75f), TextAlignmentOptions.Left, true);
        var enunciado = Texto("QuestionText", qp, V(0.02f, 0.58f), V(0.72f, 0.84f), "Enunciado de la pregunta", 14, 28, Color.white, TextAlignmentOptions.Left, true);

        var grid = Nodo("OptionsGrid", qp, V(0.02f, 0.06f), V(0.72f, 0.56f), 0);
        Configurar(grid.gameObject.AddComponent<VerticalLayoutGroup>());
        var botones = new Button[4];
        var textos = new TMP_Text[4];
        for (int fila = 0; fila < 2; fila++)
        {
            var row = NodoLibre("Row" + (fila + 1), grid);
            Configurar(row.gameObject.AddComponent<HorizontalLayoutGroup>());
            for (int col = 0; col < 2; col++)
            {
                int i = fila * 2 + col;
                botones[i] = BotonOpcion("Option_" + "ABCD"[i], row, out var lbl);
                textos[i] = lbl;
            }
        }

        var accion = Panel("ActionButton", qp, V(0.75f, 0.10f), V(0.98f, 0.50f), Pink, 0);
        var botonAccion = accion.gameObject.AddComponent<Button>();
        botonAccion.targetGraphic = accion;
        botonAccion.transition = Selectable.Transition.None;
        var textoAccion = Texto("ActionLabel", accion.rectTransform, V(0, 0), V(1, 1), "Responder", 14, 28, Color.white, TextAlignmentOptions.Center, true, 8);

        // ---- Scripts y campos ----
        var qv = panel.gameObject.AddComponent<QuestionView>();
        Asignar(qv, "progreso", progreso);
        Asignar(qv, "enunciado", enunciado);
        AsignarArray(qv, "botonesOpcion", botones);
        AsignarArray(qv, "textosOpcion", textos);
        Asignar(qv, "botonAccion", botonAccion);
        Asignar(qv, "textoAccion", textoAccion);
        Asignar(qv, "panelTutor", tutor.gameObject);
        Asignar(qv, "textoTutor", textoTutor);
        Asignar(qv, "panelRetro", retro.gameObject);
        Asignar(qv, "textoRetro", textoRetro);

        var info = panel.gameObject.AddComponent<CaseInfoView>();
        Asignar(info, "numeroCaso", numeroCaso);
        Asignar(info, "datosPaciente", datosPaciente);
        Asignar(info, "datosEstudio", datosEstudio);

        var screen = panel.gameObject.AddComponent<SimulationScreen>();
        AsignarArray(screen, "pantallaCatalogo", new Object[0]);   // el catálogo ya no está en Unity
        return screen;
    }

    // Burbuja de texto que se ajusta al contenido: VerticalLayoutGroup + ContentSizeFitter.
    // Ancla fija abajo (pivot abajo) para que al crecer suba, sin tapar el panel de preguntas.
    static RectTransform Burbuja(string nombre, Transform padre, Color color, string titulo, out TextMeshProUGUI texto)
    {
        var img = Panel(nombre, padre, V(0.60f, 0.32f), V(0.98f, 0.32f), color, 0);
        var rt = img.rectTransform;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;

        var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 16, 18);
        vlg.spacing = 6;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextoFijo("Title", rt, titulo, 16, Color.white, true);
        texto = TextoFijo("Text", rt, "...", 22, Color.white, false);
        return rt;
    }

    // ============================================================
    //  Ayudantes
    // ============================================================
    static void Aviso(string texto) => EditorUtility.DisplayDialog("MammoSim", texto, "OK");

    static Vector2 V(float x, float y) => new Vector2(x, y);

    static Sprite Redondeado() => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static RectTransform NodoLibre(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Crear " + nombre);
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    static RectTransform Nodo(string nombre, Transform padre, Vector2 min, Vector2 max, float margen)
    {
        var rt = NodoLibre(nombre, padre);
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    static Image Panel(string nombre, Transform padre, Vector2 min, Vector2 max, Color color, float margen)
    {
        var rt = Nodo(nombre, padre, min, max, margen);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Redondeado();
        img.type = Image.Type.Sliced;
        img.color = color;
        return img;
    }

    static TextMeshProUGUI Texto(string nombre, Transform padre, Vector2 min, Vector2 max, string texto,
        float tamMin, float tamMax, Color color, TextAlignmentOptions alineacion, bool negrita, float margen = 0)
    {
        var rt = Nodo(nombre, padre, min, max, margen);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.color = color;
        t.alignment = alineacion;
        t.enableAutoSizing = true;
        t.fontSizeMin = tamMin;
        t.fontSizeMax = tamMax;
        if (negrita) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return t;
    }

    // Texto de tamaño fijo para usar dentro de un Layout Group (la burbuja calcula su alto con él).
    static TextMeshProUGUI TextoFijo(string nombre, Transform padre, string texto, float tamano, Color color, bool negrita)
    {
        var rt = NodoLibre(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.color = color;
        t.fontSize = tamano;
        t.alignment = TextAlignmentOptions.TopLeft;
        if (negrita) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return t;
    }

    static Button BotonOpcion(string nombre, Transform padre, out TextMeshProUGUI label)
    {
        var rt = NodoLibre(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Redondeado();
        img.type = Image.Type.Sliced;
        img.color = OptionBlue;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;   // el color lo pinta QuestionView
        label = Texto("Label", rt, V(0, 0), V(1, 1), "Opción", 14, 26, Color.white, TextAlignmentOptions.Left, false, 14);
        return b;
    }

    static void Configurar(HorizontalOrVerticalLayoutGroup g)
    {
        g.spacing = 10;
        g.childControlWidth = true;
        g.childControlHeight = true;
        g.childForceExpandWidth = true;
        g.childForceExpandHeight = true;
    }

    static Transform HijoVacio(Transform padre, string nombre, Vector3 posMundo)
    {
        var existente = padre.Find(nombre);
        if (existente != null) return existente;   // no mueve las anclas que ya acomodaste

        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre, true);
        go.transform.position = posMundo;
        return go.transform;
    }

    // Busca algo llamado mesa / table / desk (en el renderer o en alguno de sus padres dentro de Lab).
    static bool BuscarMesa(GameObject lab, out Bounds b)
    {
        b = default;
        bool hay = false;
        foreach (var r in lab.GetComponentsInChildren<Renderer>())
        {
            bool esMesa = false;
            for (var t = r.transform; t != null && t != lab.transform; t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("mesa") || n.Contains("table") || n.Contains("desk")) { esMesa = true; break; }
            }
            if (!esMesa) continue;
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    static bool Limites(GameObject go, out Bounds b)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        b = default;
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return true;
    }

    // Los campos de los scripts son [SerializeField] privados: se asignan por SerializedObject.
    static void Asignar(Object componente, string campo, Object valor)
    {
        var so = new SerializedObject(componente);
        so.FindProperty(campo).objectReferenceValue = valor;
        so.ApplyModifiedProperties();
    }

    static void AsignarArray(Object componente, string campo, Object[] valores)
    {
        var so = new SerializedObject(componente);
        var p = so.FindProperty(campo);
        p.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        so.ApplyModifiedProperties();
    }
}
