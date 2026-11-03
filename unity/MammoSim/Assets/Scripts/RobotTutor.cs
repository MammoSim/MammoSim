using UnityEngine;

public enum EstadoRobot { Reposo, Hablando, Feliz, Triste }

// Va en el robot (Robot, hijo de RobotAnchor). El FBX de Meshy normalmente trae solo la malla (sin rig ni animaciones),
// así que se anima por código: flota siempre, "habla" con un pulso, salta si el estudiante acierta y niega con la cabeza si falla.
public class RobotTutor : MonoBehaviour
{
    [Tooltip("A qué mira el robot (normalmente MammoAnchor). Solo gira en Y.")]
    [SerializeField] Transform objetivoMirada;
    [Tooltip("Si el robot queda de espaldas, prueba 180. Para verlo de 3/4 hacia la cámara, prueba 30 o -30.")]
    [SerializeField] float correccionYaw = 0f;
    [SerializeField] float alturaFlotacion = 0.05f;

    Vector3 _posBase;
    Quaternion _rotBase;
    Vector3 _escalaBase;
    EstadoRobot _estado = EstadoRobot.Reposo;
    float _hasta;


    void Awake()
{
    // Conserva la orientación configurada manualmente en Unity.
    // No modifica la rotación para mirar hacia MammoAnchor.
    _posBase = transform.localPosition;
    _rotBase = transform.localRotation;
    _escalaBase = transform.localScale;
}

    public void Reposo() => _estado = EstadoRobot.Reposo;
    public void Saludar() => Hablar(2f);
    public void Hablar(float segundos) => Reaccionar(EstadoRobot.Hablando, segundos);

    public void Reaccionar(EstadoRobot estado, float segundos)
    {
        _estado = estado;
        _hasta = Time.time + segundos;
    }

    void Update()
    {
        if (_estado != EstadoRobot.Reposo && Time.time > _hasta) _estado = EstadoRobot.Reposo;

        float t = Time.time;
        Vector3 pos = _posBase;
        Quaternion rot = _rotBase;
        Vector3 esc = _escalaBase;

        pos.y += Mathf.Sin(t * 1.6f) * alturaFlotacion;   // flota siempre

        switch (_estado)
        {
            case EstadoRobot.Hablando:
                esc *= 1f + Mathf.Abs(Mathf.Sin(t * 9f)) * 0.04f;                    // pulso mientras "habla"
                break;
            case EstadoRobot.Feliz:
                pos.y += Mathf.Abs(Mathf.Sin(t * 6f)) * 0.18f;                       // saltitos
                rot *= Quaternion.Euler(0f, Mathf.Sin(t * 6f) * 12f, 0f);
                break;
            case EstadoRobot.Triste:
                rot *= Quaternion.Euler(0f, Mathf.Sin(t * 10f) * 15f, 0f);           // niega con la cabeza
                break;
        }

        transform.localPosition = pos;
        transform.localRotation = rot;
        transform.localScale = esc;
    }
}
