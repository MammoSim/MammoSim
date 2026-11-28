
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class OrbitCamera : MonoBehaviour
{
    [Header("Qué mira")]
    [SerializeField] Transform objetivo;
    [SerializeField] Transform anclaMamografia;
    [SerializeField] Transform robot;

    [Header("Sensibilidad y límites")]
    [SerializeField] float sensibilidad = 0.25f;
    [SerializeField] float pasoZoom = 0.4f;
    [SerializeField] Vector2 rangoDistancia = new Vector2(0.8f, 8f);
    [SerializeField] Vector2 rangoInclinacion = new Vector2(-10f, 80f);
    [SerializeField] float suavizado = 10f;

    [Header("Controles táctiles")]
    [SerializeField] float sensibilidadZoomTactil = 0.005f;

    float _yaw, _pitch, _dist;
    float _yawS, _pitchS, _distS;
    float _distInicial;

    Transform _seguir;
    Vector3 _desfase;
    Vector3 _centroS;

    bool _arrastrando;
    Vector2 _ultimaPos;

    int _dedosAnteriores;
    bool _gestoBloqueado;
    Vector2 _ultimaPosTactil;
    float _ultimaDistanciaPinza;

    readonly List<RaycastResult> _resultadosUI =
        new List<RaycastResult>();

    void Awake()
    {
        if (objetivo == null)
        {
            Debug.LogError("OrbitCamera: falta asignar Objetivo.");
            enabled = false;
            return;
        }

        _seguir = objetivo;
        _centroS = objetivo.position;

        Vector3 d = transform.position - objetivo.position;

        _dist = _distS = _distInicial =
            Mathf.Clamp(
                d.magnitude,
                rangoDistancia.x,
                rangoDistancia.y
            );

        Vector3 e = Quaternion.LookRotation(-d).eulerAngles;

        _yaw = _yawS = e.y;

        _pitch = _pitchS = Mathf.Clamp(
            e.x > 180f ? e.x - 360f : e.x,
            rangoInclinacion.x,
            rangoInclinacion.y
        );
    }

    public void EnfocarEscena()
    {
        Enfocar(objetivo, Vector3.zero, _distInicial);
    }

    public void EnfocarMamografia()
    {
        Enfocar(anclaMamografia, Vector3.zero, 1.6f);
    }

    public void EnfocarRobot()
    {
        Enfocar(robot, Vector3.up * 0.45f, 2f);
    }

    void Enfocar(Transform t, Vector3 desfase, float distancia)
    {
        if (t == null) return;

        _seguir = t;
        _desfase = desfase;

        _dist = Mathf.Clamp(
            distancia,
            rangoDistancia.x,
            rangoDistancia.y
        );
    }

    void LateUpdate()
{
    // La cámara permanece fija salvo cuando se usan
    // los métodos EnfocarEscena, EnfocarMamografia y EnfocarRobot.
    // LeerEntrada();

    float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);

        _yawS = Mathf.LerpAngle(_yawS, _yaw, k);
        _pitchS = Mathf.Lerp(_pitchS, _pitch, k);
        _distS = Mathf.Lerp(_distS, _dist, k);

        _centroS = Vector3.Lerp(
            _centroS,
            _seguir.position + _desfase,
            k
        );

        Quaternion rot = Quaternion.Euler(
            _pitchS,
            _yawS,
            0f
        );

        transform.position =
            _centroS + rot * new Vector3(0f, 0f, -_distS);

        transform.rotation = rot;
    }

    void LeerEntrada()
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

                if (cantidad == 0)
                    dedo1 = toque.position.ReadValue();
                else if (cantidad == 1)
                    dedo2 = toque.position.ReadValue();

                cantidad++;

                if (cantidad >= 3) break;
            }
        }
#else
        cantidad = Input.touchCount;

        if (cantidad > 0)
            dedo1 = Input.GetTouch(0).position;

        if (cantidad > 1)
            dedo2 = Input.GetTouch(1).position;
#endif

        if (cantidad > 0)
        {
            _arrastrando = false;
            LeerTactil(cantidad, dedo1, dedo2);
            return;
        }

        _dedosAnteriores = 0;
        _gestoBloqueado = false;

        LeerMouse();
    }

    void LeerTactil(int cantidad, Vector2 dedo1, Vector2 dedo2)
    {
        if (cantidad != _dedosAnteriores)
{
    // Prueba temporal: desactivar el bloqueo de la UI.
    _gestoBloqueado = false;

            _ultimaPosTactil = dedo1;

            if (cantidad >= 2)
            {
                _ultimaDistanciaPinza =
                    Vector2.Distance(dedo1, dedo2);
            }

            _dedosAnteriores = cantidad;
            return;
        }

        if (_gestoBloqueado) return;

        if (cantidad == 1)
        {
            Vector2 delta = dedo1 - _ultimaPosTactil;

            _yaw += delta.x * sensibilidad;

            _pitch = Mathf.Clamp(
                _pitch - delta.y * sensibilidad,
                rangoInclinacion.x,
                rangoInclinacion.y
            );

            _ultimaPosTactil = dedo1;
        }
        else if (cantidad == 2)
        {
            float distanciaActual =
                Vector2.Distance(dedo1, dedo2);

            float cambio =
                distanciaActual - _ultimaDistanciaPinza;

            _dist = Mathf.Clamp(
                _dist - cambio * sensibilidadZoomTactil,
                rangoDistancia.x,
                rangoDistancia.y
            );

            _ultimaDistanciaPinza = distanciaActual;
        }
    }

    bool EstaSobreUI(Vector2 posicion)
    {
        if (EventSystem.current == null)
            return false;

        var datos = new PointerEventData(EventSystem.current)
        {
            position = posicion
        };

        _resultadosUI.Clear();

        EventSystem.current.RaycastAll(
            datos,
            _resultadosUI
        );

        foreach (var resultado in _resultadosUI)
        {
            if (resultado.gameObject.GetComponentInParent<Canvas>()
                != null)
            {
                return true;
            }
        }

        return false;
    }

    void LeerMouse()
    {
        bool abajo;
        bool presionado;

        Vector2 pos;
        float rueda;

#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;

        if (m == null) return;

        abajo =
            m.leftButton.wasPressedThisFrame ||
            m.rightButton.wasPressedThisFrame;

        presionado =
            m.leftButton.isPressed ||
            m.rightButton.isPressed;

        pos = m.position.ReadValue();
        rueda = m.scroll.ReadValue().y;
#else
        abajo =
            Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1);

        presionado =
            Input.GetMouseButton(0) ||
            Input.GetMouseButton(1);

        pos = Input.mousePosition;
        rueda = Input.mouseScrollDelta.y;
#endif

        bool sobreUI =
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();

        if (abajo)
            _arrastrando = !sobreUI;

        if (!presionado)
            _arrastrando = false;

        if (_arrastrando && !abajo)
        {
            Vector2 delta = pos - _ultimaPos;

            _yaw += delta.x * sensibilidad;

            _pitch = Mathf.Clamp(
                _pitch - delta.y * sensibilidad,
                rangoInclinacion.x,
                rangoInclinacion.y
            );
        }

        if (!sobreUI && Mathf.Abs(rueda) > 0.01f)
        {
            _dist = Mathf.Clamp(
                _dist - Mathf.Sign(rueda) * pasoZoom,
                rangoDistancia.x,
                rangoDistancia.y
            );
        }

        _ultimaPos = pos;
    }
}
