using System.Linq;
using NUnit.Framework;

// Pruebas unitarias de CaseDtoAdapter (Models.cs).
// El Adapter traduce el DTO que llega de la API al modelo interno que usa la interfaz.
public class CaseDtoAdapterTests
{
    static CasoDto NuevoDto() => new CasoDto
    {
        id_caso = "caso_001",
        titulo = "Caso 001",
        descripcion_breve = "Descripción",
        lado = "izquierda",
        vistas = new[] { "CC", "MLO" },
        dificultad = "facil",
        edad_simulada = 62,
        modelo_3d_ref = "mama_caso_001",
        preguntas = new[]
        {
            // Desordenadas a propósito: la 8 va antes que la 1.
            new PreguntaDto
            {
                numero = 8, tipo = "diagnostica", enunciado = "Decisión",
                opciones = new[] { new OpcionDto { id = "biopsiar", texto = "Biopsia" } }
            },
            new PreguntaDto
            {
                numero = 1, tipo = "tecnica", enunciado = "Pregunta 1",
                opciones = new[]
                {
                    new OpcionDto { id = "A", texto = "Uno" },
                    new OpcionDto { id = "B", texto = "Dos" }
                }
            }
        }
    };

    [Test]
    public void Copia_los_campos_basicos()
    {
        var m = CaseDtoAdapter.Adaptar(NuevoDto());

        Assert.AreEqual("caso_001", m.Id);
        Assert.AreEqual("Caso 001", m.Titulo);
        Assert.AreEqual("Descripción", m.Descripcion);
        Assert.AreEqual("izquierda", m.Lado);
        Assert.AreEqual("facil", m.Dificultad);
        Assert.AreEqual(62, m.EdadSimulada);
        Assert.AreEqual("mama_caso_001", m.Modelo3dRef);
    }

    [Test]
    public void Une_las_vistas_con_la_letra_y()
    {
        Assert.AreEqual("CC y MLO", CaseDtoAdapter.Adaptar(NuevoDto()).Vistas);
    }

    [Test]
    public void Una_sola_vista_no_agrega_separador()
    {
        var dto = NuevoDto();
        dto.vistas = new[] { "MLO" };
        Assert.AreEqual("MLO", CaseDtoAdapter.Adaptar(dto).Vistas);
    }

    [Test]
    public void Sin_vistas_queda_en_texto_vacio()
    {
        var dto = NuevoDto();
        dto.vistas = null;
        Assert.AreEqual("", CaseDtoAdapter.Adaptar(dto).Vistas);
    }

    [Test]
    public void Ordena_las_preguntas_por_numero()
    {
        var m = CaseDtoAdapter.Adaptar(NuevoDto());
        Assert.AreEqual(new[] { 1, 8 }, m.Preguntas.Select(p => p.Numero).ToArray());
    }

    [Test]
    public void Solo_la_de_tipo_diagnostica_se_marca_como_diagnostica()
    {
        var m = CaseDtoAdapter.Adaptar(NuevoDto());
        Assert.IsFalse(m.Preguntas.First(p => p.Numero == 1).EsDiagnostica);
        Assert.IsTrue(m.Preguntas.First(p => p.Numero == 8).EsDiagnostica);
    }

    [Test]
    public void Copia_las_opciones_con_su_id_y_su_texto()
    {
        var p1 = CaseDtoAdapter.Adaptar(NuevoDto()).Preguntas.First(p => p.Numero == 1);

        Assert.AreEqual(2, p1.Opciones.Length);
        Assert.AreEqual("A", p1.Opciones[0].Id);
        Assert.AreEqual("Uno", p1.Opciones[0].Texto);
        Assert.AreEqual("B", p1.Opciones[1].Id);
    }

    // ---- Casos borde: HOY FALLAN (Select/OrderBy sobre null lanzan ArgumentNullException).
    // Si el equipo decide que la API nunca manda estos campos nulos, se puede marcar [Ignore].

    [Test]
    public void Sin_preguntas_no_lanza_excepcion()
    {
        var dto = NuevoDto();
        dto.preguntas = null;
        Assert.DoesNotThrow(() => CaseDtoAdapter.Adaptar(dto));
    }

    [Test]
    public void Pregunta_sin_opciones_no_lanza_excepcion()
    {
        var dto = NuevoDto();
        dto.preguntas[0].opciones = null;
        Assert.DoesNotThrow(() => CaseDtoAdapter.Adaptar(dto));
    }
}
