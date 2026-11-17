import { UserGroupIcon, UserIcon } from "lucide-react";
import { useEffect, useRef } from "react";
import "../../styles/ModeDialog.css";
import Logo from "../../assets/Logo-bg.png";

type SimulationModeDialogProps = {
  isOpen: boolean;
  onClose: () => void;
};

export default function SimulationModeDialog({
  isOpen,
  onClose,
}: SimulationModeDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;

    if (!dialog) return;

    if (isOpen && !dialog.open) dialog.showModal();
    if (!isOpen && dialog.open) dialog.close();
  });
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
            <div className="code_container">
              <input placeholder="Ingresa el código de clase (Token)" />
              <button type="submit">Ingresar</button>
            </div>
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
            <button type="button">Iniciar</button>
          </div>
        </div>
      </section>
    </dialog>
  );
}
