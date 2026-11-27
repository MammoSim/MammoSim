import { AlertTriangleIcon, X } from "lucide-react";
import { useEffect, useRef } from "react";
import '../../styles/AlertDialog.css';


type AlertDialogProps = {
  isOpen: boolean;
  onClose: () => void;
};

export default function AlertDialog({ isOpen, onClose }: AlertDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
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
      aria-labelledby="academy-alert-title"
      className="academy_alert_dialog"
    >
      <div className="academy_alert_close">
        <button type="button" onClick={onClose} aria-label="Cerra alerta" className="button_close">
          <X aria-hidden="true"/>
        </button>
      </div>
      <div>
        <AlertTriangleIcon className="icon_academy_alert"/>
        <h3 id="academy-alert-title" style={{fontSize: 26}}>
          MammoSim es un simulador web únicamente de carácter académico y experimental.
        </h3>
        <p style={{fontSize: 16}}>No busca reemplazar el criterio médico. No es un dispositivo de detección de cáncer de mama.</p>
      </div>
    </dialog>
  );
}
