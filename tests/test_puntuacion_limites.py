"""Puntuación: casos límite y coherencia entre la nota de un caso y la de la sesión (RF-006.1)."""
import pytest

from app.services.puntuacion import nota_caso, puntuacion_sesion


def test_un_valor_negativo_de_preguntas_lanza_error():
    with pytest.raises(ValueError):
        nota_caso(-1, True, True)


@pytest.mark.parametrize("n", [1, 3, 7])
def test_la_sesion_perfecta_vale_10_con_cualquier_cantidad_de_casos(n):
    assert puntuacion_sesion(7 * n, n, n, n) == 10.0


@pytest.mark.parametrize("n", [1, 3, 7])
def test_la_sesion_en_blanco_vale_0(n):
    assert puntuacion_sesion(0, 0, 0, n) == 0.0


@pytest.mark.parametrize("preguntas", range(8))
@pytest.mark.parametrize("inicial", [False, True])
@pytest.mark.parametrize("final", [False, True])
def test_una_sesion_de_un_caso_vale_lo_mismo_que_ese_caso(preguntas, inicial, final):
    assert puntuacion_sesion(preguntas, int(inicial), int(final), 1) == nota_caso(preguntas, inicial, final)


def test_acertar_una_pregunta_mas_nunca_baja_la_nota():
    notas = [nota_caso(p, True, True) for p in range(8)]
    assert notas == sorted(notas) and len(set(notas)) == 8


def test_mas_aciertos_de_los_posibles_lanza_error():
    with pytest.raises(ValueError):
        puntuacion_sesion(99, 5, 5, 1)


def test_aciertos_negativos_lanzan_error():
    with pytest.raises(ValueError):
        puntuacion_sesion(-5, 0, 0, 1)
