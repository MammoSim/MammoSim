import { createElement } from "react";
import '@google/model-viewer';


type ModelsPreviewProps = {
    caseId: string; 
    title?: string; 
}
export default function ModelsPreview({caseId, title}: ModelsPreviewProps){
    return createElement("model-viewer", {
        key: caseId,
        src: `/modelos/mama_${encodeURIComponent(caseId)}.glb`,
        alt: title ?? `Modelo del caso ${caseId}`,
        class: "model_preview",
        "camera-controls": "",
        "camera-orbit":"180deg 75px 75px",
        "auto-rotate": "",
        "disabled-zoom": "",
        "touch-action": "pan-y",
        loading: "lazy",
    });
}