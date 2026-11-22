import { useEffect, useState } from "react";
import { getMyProfile, type Usuario } from "../services/authServices";

export function useProfile() {
    const [usuario, setUsuario] = useState<Usuario | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");


    useEffect(() => {
        const controller = new AbortController();

        async function loadProfile() {
            try {
                const profile = await getMyProfile(controller.signal);

                if(!controller.signal.aborted){
                    setUsuario(profile);
                }
            } catch (error) {
                if(!controller.signal.aborted){
                    setError( error instanceof Error ? error.message : "No fue posible cargar el perfil", );
                }
            } finally {
                if (!controller.signal.aborted){
                    setLoading(false);
                }
            }
            
        }
        void loadProfile();

        return () => controller.abort();
    }, []);

    return {usuario, loading, error};
}