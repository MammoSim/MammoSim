using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Controla el modelo del caso y las reacciones del robot; no mueve la cámara.
// Mouse: arrastrar para girar, rueda para zoom.
// Móvil: un dedo para girar, dos dedos para zoom.
public class LabSceneManager : MonoBehaviour
{
    [Serializable]
    public class ModeloCaso
    {
        public string referencia;
        public GameObject prefab;
    }

    [Header("Escena")]
    [SerializeField] Transform anclaMamografia;
    [SerializeField] RobotTutor robot;

    [Header("Modelos 3D del hallazgo")]
    [SerializeField] List<ModeloCaso> modelosLocales = new List<ModeloCaso>();
    [Tooltip("URL de Firebase Hosting que contiene los archivos .glb.")]
    [SerializeField] string urlBaseModelos = "";
    [SerializeField] float tamanoModelo = 0.6f;

    [Header("Movimiento del modelo")]
    [SerializeField] float giroAutomatico = 0f;
    [SerializeField] float flotacion = 0.03f;

    [Header("Manipulación manual del modelo")]
    [SerializeField] float sensibilidadRotacion = 0.25f;
    [SerializeField] float sensibilidadZoomTactil = 0.005f;
    [SerializeField] float sensibilidadZoomMouse = 0.15f;
    [Tooltip("Zoom mínimo y máximo para la rueda del mouse en PC.")]
    [SerializeField] Vector2 limitesZoomMouse = new Vector2(0.6f, 1.8f);
    [Tooltip("Zoom mínimo y máximo para el gesto de dos dedos en celular.")]
    [SerializeField] Vector2 limitesZoomTactil = new Vector2(0.6f, 4.0f);

    GameObject _modelo;
    Vector3 _posBase;
    Vector3 _escalaOriginal;
    float _yaw;
    float _pitchModelo;
    float _zoomModelo = 1f;
    int _version;

    int _dedosAnteriores;
    bool _gestoBloqueado;
    Vector2 _ultimaPosicion;
    float _distanciaAnterior;

    bool _arrastrandoMouse;
    Vector2 _ultimaPosicionMouse;
    readonly List<RaycastResult> _resultadosUI = new List<RaycastResult>();

    void Awake()
    {
        if (anclaMamografia == null)
        {
            Debug.LogError("LabSceneManager: falta Ancla Mamografia.");
            enabled = false;
            return;
        }

        _posBase = anclaMamografia.localPosition;
        _escalaOriginal = anclaMamografia.localScale;
    }

    void OnEnable()
    {
        SimulationEvents.OnCasoSeleccionado += CargarCaso;
        SimulationEvents.OnPreguntaMostrada += AlMostrarPregunta;
        SimulationEvents.OnRetroalimentacion += AlRetroalimentar;
        SimulationEvents.OnRecomendacionRecibida += AlRecomendar;
        SimulationEvents.OnEvaluacionDisponible += AlEvaluar;
        SimulationEvents.OnSimulacionFinalizada += AlTerminar;
    }

    void OnDisable()
    {
        SimulationEvents.OnCasoSeleccionado -= CargarCaso;
        SimulationEvents.OnPreguntaMostrada -= AlMostrarPregunta;
        SimulationEvents.OnRetroalimentacion -= AlRetroalimentar;
        SimulationEvents.OnRecomendacionRecibida -= AlRecomendar;
        SimulationEvents.OnEvaluacionDisponible -= AlEvaluar;
        SimulationEvents.OnSimulacionFinalizada -= AlTerminar;
    }

    void Update()
    {
        if (_modelo == null) return;

        LeerControlesModelo();
        _yaw += giroAutomatico * Time.deltaTime;

        anclaMamografia.localRotation = Quaternion.Euler(_pitchModelo, _yaw, 0f);
        anclaMamografia.localScale = _escalaOriginal * _zoomModelo;
        anclaMamografia.localPosition = _posBase +
            Vector3.up * (Mathf.Sin(Time.time * 1.5f) * flotacion);
    }

    // Reacciones a eventos del flujo de simulación.
    void AlMostrarPregunta(PreguntaModel p, int indice, int total)
    {
        if (robot != null) robot.Reposo();
    }

    void AlRetroalimentar(RespuestaRes r)
    {
        if (robot != null)
            robot.Reaccionar(r.correcta ? EstadoRobot.Feliz : EstadoRobot.Triste, 2.5f);
    }

    void AlRecomendar(RecomendacionRes r)
    {
        if (robot != null)
            robot.Hablar(Mathf.Clamp(r.texto.Length * 0.05f, 2f, 8f));
    }

    void AlEvaluar(ResultadoCasoRes r)
    {
        Debug.Log("[MammoSim] Resultado final recibido: " + JsonUtility.ToJson(r));
        if (robot == null) return;

        if (r.siguio_ia && r.acierto_final)
            robot.Reaccionar(EstadoRobot.Feliz, 4f);
        else
            robot.Reposo();
    }

    void AlTerminar()
    {
        _version++;
        LimpiarModelo();
        if (robot != null) robot.Reposo();
    }

    void CargarCaso(CaseModel c)
    {
        LimpiarModelo();
        _version++;

        var entrada = modelosLocales.Find(m =>
            m.referencia == c.Modelo3dRef && m.prefab != null);

        if (entrada != null)
        {
            Colocar(Instantiate(entrada.prefab));
        }
        else if (!string.IsNullOrEmpty(urlBaseModelos))
        {
#if MAMMOSIM_GLTFAST
            CargarRemoto(c.Modelo3dRef, _version);
#else
            Debug.LogWarning("[MammoSim] Falta glTFast o MAMMOSIM_GLTFAST. Uso marcador.");
            Colocar(Marcador(c.Modelo3dRef));
#endif
        }
        else
        {
            Debug.LogWarning($"[MammoSim] No hay modelo 3D para '{c.Modelo3dRef}'. Uso marcador.");
            Colocar(Marcador(c.Modelo3dRef));
        }

        if (robot != null) robot.Saludar();
    }

#if MAMMOSIM_GLTFAST
    async void CargarRemoto(string referencia, int version)
    {
        var gltf = new GLTFast.GltfImport();
        bool ok = await gltf.Load(urlBaseModelos.TrimEnd('/') + "/" + referencia + ".glb");
        if (!ok || version != _version) return;

        var contenedor = new GameObject("Modelo_" + referencia);
        if (await gltf.InstantiateMainSceneAsync(contenedor.transform) && version == _version)
            Colocar(contenedor);
        else
            Destroy(contenedor);
    }
#endif

    static GameObject Marcador(string referencia)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Marcador_" + referencia;
        go.transform.localScale = new Vector3(1f, 0.7f, 1f);
        return go;
    }

    void LimpiarModelo()
    {
        if (_modelo != null) Destroy(_modelo);
        _modelo = null;
        _dedosAnteriores = 0;
        _gestoBloqueado = false;
        _arrastrandoMouse = false;
    }

    void Colocar(GameObject go)
    {
        _yaw = 0f;
        _pitchModelo = 0f;
        _zoomModelo = 1f;
        _dedosAnteriores = 0;
        _gestoBloqueado = false;
        _arrastrandoMouse = false;

        anclaMamografia.localRotation = Quaternion.identity;
        anclaMamografia.localPosition = _posBase;
        anclaMamografia.localScale = _escalaOriginal;

        var t = go.transform;
        t.SetParent(anclaMamografia, false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        if (Limites(go, out Bounds b))
        {
            float mayor = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (mayor > 0.0001f) t.localScale *= tamanoModelo / mayor;
            if (Limites(go, out b)) t.position += anclaMamografia.position - b.center;
        }

        // Conserva el collider para futuras interacciones sobre el modelo.
        if (go.GetComponentInChildren<Collider>() == null && Limites(go, out Bounds limites))
        {
            BoxCollider collider = go.AddComponent<BoxCollider>();
            collider.center = go.transform.InverseTransformPoint(limites.center);
            Vector3 size = go.transform.InverseTransformVector(limites.size);
            collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        _modelo = go;
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

    void LeerControlesModelo()
    {
        int cantidad = 0;
        Vector2 dedo1 = Vector2.zero;
        Vector2 dedo2 = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        var pantalla = Touchscreen.current;
        if (pantalla != null)
        {
            foreach (var toque in pantalla.touches)
            {
                if (!toque.press.isPressed) continue;
                if (cantidad == 0) dedo1 = toque.position.ReadValue();
                else if (cantidad == 1) dedo2 = toque.position.ReadValue();
                cantidad++;
                if (cantidad >= 3) break;
            }
        }
#else
        cantidad = Input.touchCount;
        if (cantidad > 0) dedo1 = Input.GetTouch(0).position;
        if (cantidad > 1) dedo2 = Input.GetTouch(1).position;
#endif

        // En móvil no usar el mouse simulado por el navegador.
        if (cantidad > 0 || Application.isMobilePlatform)
        {
            _arrastrandoMouse = false;
            LeerGestos(cantidad, dedo1, dedo2);
            return;
        }

        _dedosAnteriores = 0;
        _gestoBloqueado = false;
        LeerControlMouse();
    }

    void LeerGestos(int cantidad, Vector2 dedo1, Vector2 dedo2)
    {
        if (cantidad != _dedosAnteriores)
        {
            if (cantidad == 0)
            {
                _gestoBloqueado = false;
                _dedosAnteriores = 0;
                return;
            }

            // Solo bloquear controles interactivos: los paneles transparentes
            // no deben impedir la manipulación del modelo.
            if (_dedosAnteriores == 0)
                _gestoBloqueado = EstaSobreControlUI(dedo1);
            if (cantidad >= 2)
                _gestoBloqueado |= EstaSobreControlUI(dedo2);

            _ultimaPosicion = dedo1;
            if (cantidad >= 2)
                _distanciaAnterior = Vector2.Distance(dedo1, dedo2);

            _dedosAnteriores = cantidad;
            return;
        }

        if (_gestoBloqueado) return;

        if (cantidad == 1)
        {
            Vector2 delta = dedo1 - _ultimaPosicion;
            _yaw -= delta.x * sensibilidadRotacion;
            _pitchModelo = Mathf.Clamp(_pitchModelo + delta.y * sensibilidadRotacion, -70f, 70f);
            _ultimaPosicion = dedo1;
        }
        else if (cantidad == 2)
        {
            float distanciaActual = Vector2.Distance(dedo1, dedo2);
            _zoomModelo = Mathf.Clamp(
                _zoomModelo + (distanciaActual - _distanciaAnterior) * sensibilidadZoomTactil,
                limitesZoomTactil.x, limitesZoomTactil.y);
            _distanciaAnterior = distanciaActual;
        }
    }

    void LeerControlMouse()
    {
        bool abajo, presionado;
        Vector2 posicion;
        float rueda;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse == null) return;
        abajo = mouse.leftButton.wasPressedThisFrame;
        presionado = mouse.leftButton.isPressed;
        posicion = mouse.position.ReadValue();
        rueda = mouse.scroll.ReadValue().y;
#else
        abajo = Input.GetMouseButtonDown(0);
        presionado = Input.GetMouseButton(0);
        posicion = Input.mousePosition;
        rueda = Input.mouseScrollDelta.y;
#endif

        if (abajo)
        {
            _arrastrandoMouse = !EstaSobreControlUI(posicion);
            _ultimaPosicionMouse = posicion;
        }
        if (!presionado) _arrastrandoMouse = false;

        if (_arrastrandoMouse && !abajo)
        {
            Vector2 delta = posicion - _ultimaPosicionMouse;
            _yaw -= delta.x * sensibilidadRotacion;
            _pitchModelo = Mathf.Clamp(_pitchModelo + delta.y * sensibilidadRotacion, -70f, 70f);
        }
        _ultimaPosicionMouse = posicion;

        if (!EstaSobreControlUI(posicion) && Mathf.Abs(rueda) > 0.01f)
            _zoomModelo = Mathf.Clamp(_zoomModelo + Mathf.Sign(rueda) * sensibilidadZoomMouse,
                limitesZoomMouse.x, limitesZoomMouse.y);
    }

    bool EstaSobreControlUI(Vector2 posicion)
    {
        if (EventSystem.current == null) return false;

        var datos = new PointerEventData(EventSystem.current) { position = posicion };
        _resultadosUI.Clear();
        EventSystem.current.RaycastAll(datos, _resultadosUI);

        foreach (var resultado in _resultadosUI)
        {
            var objeto = resultado.gameObject;
            // Un Canvas o un fondo translúcido no bloquea la interacción.
            // Sí la bloquean los controles que reciben clics o arrastres.
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(objeto) != null ||
                ExecuteEvents.GetEventHandler<IDragHandler>(objeto) != null ||
                ExecuteEvents.GetEventHandler<IScrollHandler>(objeto) != null)
                return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (anclaMamografia != null)
        {
            Gizmos.color = new Color(0.91f, 0.12f, 0.55f);
            Gizmos.DrawWireSphere(anclaMamografia.position, tamanoModelo * 0.5f);
        }
        if (robot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(robot.transform.position + Vector3.up * 0.45f,
                new Vector3(0.5f, 0.9f, 0.5f));
        }
    }
}
