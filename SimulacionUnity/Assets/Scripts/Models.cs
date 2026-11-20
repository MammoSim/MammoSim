
using System;
using System.Linq;

// =====================================================
// DTOs: datos que intercambiamos con FastAPI
// =====================================================

[Serializable]
public class OpcionDto
{
    public string id;
    public string texto;
}

[Serializable]
public class PreguntaDto
{
    public int numero;
    public string tipo;       // tecnica | diagnostica
    public string enunciado;
    public OpcionDto[] opciones;
}

[Serializable]
public class CasoDto
{
    public string id_caso;
    public string titulo;
    public string descripcion_breve;
    public string lado;
    public string[] vistas;
    public string dificultad;
    public int edad_simulada;
    public string modelo_3d_ref;
    public PreguntaDto[] preguntas;
}

// =====================================================
// SESIONES
// POST /sesiones
// =====================================================

[Serializable]
public class VacioReq
{
    // Se conserva por compatibilidad.
}

[Serializable]
public class CrearSesionReq
{
    public string id_curso;   // null = práctica libre
    public string[] casos;
}

[Serializable]
public class SesionRes
{
    public string id_sesion;
    public string[] casos_asignados;
    public string estado;
}

// GET /sesiones/activa
[Serializable]
public class SesionActivaRes
{
    public string id_sesion;
    public string[] casos_asignados;
    public string[] casos_terminados;
    public string caso_actual;
    public string estado;
}

// =====================================================
// RESPUESTAS TÉCNICAS (PREGUNTAS 1 A 7)
// POST /sesiones/{id_sesion}/casos/{id_caso}/respuestas
// =====================================================

[Serializable]
public class RespuestaReq
{
    public int numero_pregunta;
    public string opcion_elegida;
}

[Serializable]
public class RespuestaRes
{
    // Campos reales del backend.
    public bool correcta;
    public string respuesta_correcta;
    public string explicacion;
    public string mensaje;

    // Campos antiguos, utilizados por la interfaz Unity.
    // GameManager los completará después de recibir el JSON.
    public string opcion_correcta;
    public string texto_correcto;
}

// =====================================================
// DECISIONES CLÍNICAS
// =====================================================

[Serializable]
public class DecisionReq
{
    // biopsiar | seguimiento | ignorar | estudio_adicional
    public string decision;
}

// POST /decision-inicial y POST /decision-final
[Serializable]
public class GuardadaRes
{
    public bool guardada;
}

// GET /recomendacion-ia
[Serializable]
public class RecomendacionRes
{
    // Campos reales del backend.
    public string opcion_ia;
    public string texto;

    // Compatibilidad con la interfaz anterior.
    public string conducta;
}

// =====================================================
// RETROALIMENTACIÓN FINAL
// GET /retroalimentacion
// =====================================================

[Serializable]
public class PreguntaResultadoRes
{
    public int numero;
    public bool correcta;
}

[Serializable]
public class RetroalimentacionRes
{
    public string hallazgos;
    public string retroalimentacion;
    public string respuesta_correcta;
    public PreguntaResultadoRes[] preguntas;
    public bool ia_correcta;
    public bool siguio_ia;
    public float nota_caso;
}

// Modelo que utiliza actualmente la interfaz de Unity.
[Serializable]
public class ResultadoCasoRes
{
    public bool acierto_inicial;
    public bool acierto_final;
    public bool siguio_ia;
    public string hallazgos;
    public string retroalimentacion;
}

// =====================================================
// RESULTADO GENERAL DE LA SESIÓN
// POST /sesiones/{id_sesion}/cerrar
// GET /sesiones/{id_sesion}/resultado
// =====================================================

[Serializable]
public class ResultadoSesionRes
{
    public string id_usuario;
    public string id_curso;
    public int total_casos;
    public int aciertos_iniciales;
    public int aciertos_finales;
    public int preguntas_acertadas;
    public int veces_siguio_ia;
    public int siguio_ia_correcta;
    public int siguio_ia_incorrecta;
    public float puntuacion;
}

// =====================================================
// MODELOS INTERNOS DE UNITY
// Son los que utiliza el HUD y la simulación.
// =====================================================

public class OpcionModel
{
    public string Id;
    public string Texto;
}

public class PreguntaModel
{
    public int Numero;
    public bool EsDiagnostica;
    public string Enunciado;
    public OpcionModel[] Opciones;
}

public class CaseModel
{
    public string Id;
    public string Titulo;
    public string Descripcion;
    public string Lado;
    public string Vistas;
    public string Dificultad;
    public int EdadSimulada;
    public string Modelo3dRef;
    public PreguntaModel[] Preguntas;
}

// =====================================================
// ADAPTADOR: FastAPI -> Modelo interno de Unity
// =====================================================

public static class CaseDtoAdapter
{
    public static CaseModel Adaptar(CasoDto d) => new CaseModel
    {
        Id = d.id_caso,
        Titulo = d.titulo,
        Descripcion = d.descripcion_breve,
        Lado = d.lado,
        Vistas = d.vistas == null
            ? ""
            : string.Join(" y ", d.vistas),
        Dificultad = d.dificultad,
        EdadSimulada = d.edad_simulada,
        Modelo3dRef = d.modelo_3d_ref,

        Preguntas = d.preguntas
            .OrderBy(p => p.numero)
            .Select(p => new PreguntaModel
            {
                Numero = p.numero,
                EsDiagnostica = p.tipo == "diagnostica",
                Enunciado = p.enunciado,

                Opciones = p.opciones
                    .Select(o => new OpcionModel
                    {
                        Id = o.id,
                        Texto = o.texto
                    })
                    .ToArray()
            })
            .ToArray()
    };
}

// =====================================================
// ESTADOS DE LA SIMULACIÓN
// =====================================================

public enum FaseSimulacion
{
    Cargando,

    Pregunta,           // Preguntas técnicas 1 a 7

    Retroalimentacion,  // Después de responder

    DecisionInicial,    // Pregunta diagnóstica 8

    DecisionFinal,      // Después de recibir la IA

    Resultado,          // Evaluación del caso

    Error
}

[Serializable]
public class ProgresoCasoRes
{
    public string id_caso;

    public int[] preguntas_respondidas;

    // Unity JsonUtility convierte null numérico
    // en su valor predeterminado (0).
    public int siguiente_pregunta;

    public bool decision_inicial_guardada;
    public bool decision_final_guardada;

    public string decision_inicial;
    public string decision_final;
}
