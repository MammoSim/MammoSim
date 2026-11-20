import { UserGroupIcon, UserIcon } from "lucide-react";
import { useEffect, useRef, useState, type FormEvent } from "react";
import "../../styles/ModeDialog.css";
import Logo from "../../assets/Logo-bg.png";
import { useNavigate } from "react-router-dom";
import { joinCourse } from "../../services/coursesServices";
import { useSimulationMode } from "../../hooks/useSimulationMode";

type SimulationModeDialogProps = {
  isOpen: boolean;
  onClose: () => void;
};

export default function SimulationModeDialog({
  isOpen,
  onClose,
}: SimulationModeDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const navigate = useNavigate();
  const [code, setCode] = useState("");
  const [joining, setJoining] = useState(false);
  const [joinError, setJoinError] = useState("");
  const { selectCourse, selectIndividual } = useSimulationMode();

  async function handleJoin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (joining) return;

    const codigo = code.trim();

    if (!codigo) {
      setJoinError("Ingresa el código de la clase a la que quiere unirte");
      return;
    }
    setJoining(true);
    setJoinError("");

    try {
      const course = await joinCourse(codigo);

      selectCourse(course.id_curso);
      onClose();
      navigate("/cases", { replace: true });
    } catch (error) {
      setJoinError(
        error instanceof Error
          ? error.message
          : "No fue posible ingresar a la clase",
      );
    } finally {
      setJoining(false);
    }
  }

  useEffect(() => {
    const dialog = dialogRef.current;

    if (!dialog) return;

    if (isOpen && !dialog.open) dialog.showModal();
    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);
  return (
    <dialog
      ref={dialogRef}
      onClose={onClose}
      aria-labelledby="simulation-mode-title"
      className="dialog_container"
    >
      <div className="dialog_left">
        <img src={Logo} className="logo" />
        <p>
          Antes de iniciar tu simulación, selecciona cómo deseas ingresar a la
          plataforma.
        </p>
      </div>
      <section className="mode_container">
        <h2 id="simulation-mode-title">
          Selecciona una modalidad de simulación
        </h2>
        <p>
          Elige si vas a partcipar en una clase dirigida por tu docente o si
          prefieres realizar prácticas de manera independiente.
        </p>

        <div className="class_mode">
          <UserGroupIcon className="icon" />
          <div className="class_mode_content">
            <h3>Unirme a una clase</h3>
            <p>
              Ingresa el código proporcionado por tu docente para que pueda
              realizar el siguimiento y evaluar tu historial de simulación.
            </p>
            <form className="code_container" onSubmit={handleJoin}>
              <label htmlFor="class-code">Código de la clase</label>
              <input
                id="class-code"
                name="codigo"
                value={code}
                onChange={(event) => setCode(event.target.value)}
                placeholder="Ingresa el código de clase"
                disabled={joining}
                required
              />
              <button type="submit" disabled={joining}>
                {joining ? "Ingresando..." : "Ingresar"}
              </button>
            </form>
            {joinError && <p role="alert">{joinError}</p>}
          </div>
        </div>

        <div className="no_mode">
          <UserIcon className="icon" />
          <div>
            <h3>Practicar solo</h3>
            <p>Accede al banco de casos clínicos de forma libre.</p>
            <p>
              Importante: Bajo esta modalidad, ningún docente podrá visualizar
              tu historial ni resultados.
            </p>
          </div>

          <div className="no_code_button_container">
            <button
              type="button"
              disabled={joining}
              onClick={() => {
                selectIndividual(); 
                onClose();
                navigate("/cases", {
                  replace: true,
                });
              }}
            >
              Iniciar
            </button>
          </div>
        </div>
      </section>
    </dialog>
  );
}
