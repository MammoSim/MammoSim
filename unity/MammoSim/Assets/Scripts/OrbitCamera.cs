using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Va en la Main Camera. Arrastrar con el mouse = orbitar; rueda = acercar/alejar.
// No orbita si el clic empieza sobre la UI (botones, panel de preguntas, burbujas).
// La vista inicial es la que dejes en la Scene: el script la toma al arrancar.
public class OrbitCamera : MonoBehaviour
{
    [Header("Qué mira")]
    [SerializeField] Transform objetivo;          // OrbitTarget: punto entre la mesa y el robot
    [SerializeField] Transform anclaMamografia;   // para EnfocarMamografia()
    [SerializeField] Transform robot;             // para EnfocarRobot()

    [Header("Sensibilidad y límites")]
    [SerializeField] float sensibilidad = 0.25f;                          // grados por píxel
    [SerializeField] float pasoZoom = 0.4f;                               // metros por "clic" de rueda
    [SerializeField] Vector2 rangoDistancia = new Vector2(0.8f, 8f);
    [SerializeField] Vector2 rangoInclinacion = new Vector2(-10f, 80f);   // grados: negativo = desde abajo
    [SerializeField] float suavizado = 10f;

    float _yaw, _pitch, _dist;
    float _yawS, _pitchS, _distS;
    float _distInicial;
    Transform _seguir;
    Vector3 _desfase;
    Vector3 _centroS;
    bool _arrastrando;
    Vector2 _ultimaPos;

    void Awake()
    {
        _seguir = objetivo;
        _centroS = objetivo.position;

        Vector3 d = transform.position - objetivo.position;
        _dist = _distS = _distInicial = Mathf.Clamp(d.magnitude, rangoDistancia.x, rangoDistancia.y);
        Vector3 e = Quaternion.LookRotation(-d).eulerAngles;
        _yaw = _yawS = e.y;
        _pitch = _pitchS = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, rangoInclinacion.x, rangoInclinacion.y);
    }

    // Se pueden conectar a botones de la UI más adelante.
    public void EnfocarEscena() => Enfocar(objetivo, Vector3.zero, _distInicial);
    public void EnfocarMamografia() => Enfocar(anclaMamografia, Vector3.zero, 1.6f);
    public void EnfocarRobot() => Enfocar(robot, Vector3.up * 0.45f, 2f);

    void Enfocar(Transform t, Vector3 desfase, float distancia)
    {
        if (t == null) return;
        _seguir = t;
        _desfase = desfase;
        _dist = Mathf.Clamp(distancia, rangoDistancia.x, rangoDistancia.y);
    }

    void LateUpdate()
    {
        LeerEntrada();

        float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        _yawS = Mathf.LerpAngle(_yawS, _yaw, k);
        _pitchS = Mathf.Lerp(_pitchS, _pitch, k);
        _distS = Mathf.Lerp(_distS, _dist, k);
        _centroS = Vector3.Lerp(_centroS, _seguir.position + _desfase, k);

        Quaternion rot = Quaternion.Euler(_pitchS, _yawS, 0f);
        transform.position = _centroS + rot * new Vector3(0f, 0f, -_distS);
        transform.rotation = rot;
    }

    void LeerEntrada()
    {
        bool abajo, presionado;
        Vector2 pos;
        float rueda;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return;
        abajo = m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame;
        presionado = m.leftButton.isPressed || m.rightButton.isPressed;
        pos = m.position.ReadValue();
        rueda = m.scroll.ReadValue().y;
#else
        abajo = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
        presionado = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        pos = Input.mousePosition;
        rueda = Input.mouseScrollDelta.y;
#endif
        bool sobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (abajo) _arrastrando = !sobreUI;
        if (!presionado) _arrastrando = false;

        if (_arrastrando && !abajo)
        {
            Vector2 delta = pos - _ultimaPos;
            _yaw += delta.x * sensibilidad;
            _pitch = Mathf.Clamp(_pitch - delta.y * sensibilidad, rangoInclinacion.x, rangoInclinacion.y);
        }

        if (!sobreUI && Mathf.Abs(rueda) > 0.01f)
            _dist = Mathf.Clamp(_dist - Mathf.Sign(rueda) * pasoZoom, rangoDistancia.x, rangoDistancia.y);

        _ultimaPos = pos;
    }
}
