import type {
  CasoRevision,
  StudentSessionDetail,
} from "../../types/StudentHistorial";

type AnalysisSummaryProps = {
  caso: CasoRevision;
  sesion: Pick<
    StudentSessionDetail,
    "estado" | "fecha_inicio" | "fecha_fin" | "puntuacion"
  >;
};

export default function AnalysisSummary({
  caso,
  sesion,
}: AnalysisSummaryProps) {
  return (
    <div className="summary_container">
      <h3>Resumen del análisis</h3>
      <div className="table_wrap">
        <table className="summary_table">
          <tbody>
            <tr>
              <th scope="row">Caso analizado</th>
              <td>{caso.titulo ?? caso.id_caso}</td>
            </tr>
            <tr>
              <th scope="row">Resultado</th>
              <td>{caso.retroalimentacion}</td>
            </tr>
            <tr>
              <th scope="row">Puntuación</th>
              <td>{sesion.puntuacion}/10</td>
            </tr>
            <tr>
              <th scope="row">Estado</th>
              <td>
                {sesion.estado === "completada" ? "Completado" : "En curso"}
              </td>
            </tr>
            <tr>
              <th scope="row">Fecha inicio</th>
              <td>{new Date(sesion.fecha_inicio).toLocaleString("es-CO")}</td>
            </tr>
            <tr>
              <th scope="row">Fecha fin</th>
              <td>
                {sesion.fecha_fin
                  ? new Date(sesion.fecha_fin).toLocaleString("es-CO")
                  : "Sin finalizar"}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
