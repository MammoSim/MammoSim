
using UnityEngine;
using UnityEngine.InputSystem;

public class RotarModelo3D : MonoBehaviour
{
    [Header("Rotación")]
    [SerializeField] float sensibilidad = 0.3f;
    [SerializeField] Camera camara;

    [Header("Zoom")]
    [SerializeField] float velocidadZoom = 0.15f;
    [SerializeField] float zoomMinimo = 0.5f;
    [SerializeField] float zoomMaximo = 2.5f;

    bool arrastrando;
    Transform modelo;
    Transform modeloAnterior;

    Vector3 escalaOriginal;
    float zoomActual = 1f;

    void Start()
    {
        if (camara == null)
            camara = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null || camara == null)
            return;

        // Detectar el modelo dentro de MammoAnchor
        if (transform.childCount == 0)
        {
            modelo = null;
            modeloAnterior = null;
            arrastrando = false;
            return;
        }

        modelo = transform.GetChild(0);

        // Si cambia el caso, reiniciar el zoom
        if (modelo != modeloAnterior)
        {
            modeloAnterior = modelo;
            escalaOriginal = modelo.localScale;
            zoomActual = 1f;
            arrastrando = false;
        }

        // ---------- ZOOM ----------
        float rueda = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(rueda) > 0.01f)
        {
            float direccion = Mathf.Sign(rueda);

            zoomActual = Mathf.Clamp(
                zoomActual + direccion * velocidadZoom,
                zoomMinimo,
                zoomMaximo
            );

            modelo.localScale = escalaOriginal * zoomActual;
        }

        // Restablecer tamaño original con R
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            zoomActual = 1f;
            modelo.localScale = escalaOriginal;
        }

        // ---------- ROTACIÓN ----------
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            arrastrando = false;

            Vector2 posicionMouse =
                Mouse.current.position.ReadValue();

            Ray rayo = camara.ScreenPointToRay(posicionMouse);

            if (Physics.Raycast(rayo, out RaycastHit impacto))
            {
                if (impacto.transform == modelo ||
                    impacto.transform.IsChildOf(modelo))
                {
                    arrastrando = true;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            arrastrando = false;

        if (!arrastrando)
            return;

        Vector2 movimiento = Mouse.current.delta.ReadValue();

        modelo.Rotate(
            Vector3.up,
            -movimiento.x * sensibilidad,
            Space.World
        );

        modelo.Rotate(
            Vector3.right,
            movimiento.y * sensibilidad,
            Space.World
        );
    }
}
