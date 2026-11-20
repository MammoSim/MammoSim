
using System;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;

[Serializable]
public class DatosModelo
{
    public string casoId;
    public string modeloUrl;
}

public class ModelLoader : MonoBehaviour
{
    private GameObject modeloActual;
    private GltfImport importadorActual;
    private int versionSolicitud = 0;

    public async void CargarModelo(string json)
    {
        DatosModelo datos;

        try
        {
            datos = JsonUtility.FromJson<DatosModelo>(json);
        }
        catch (Exception e)
        {
            Debug.LogError("JSON inválido: " + e.Message);
            return;
        }

        if (datos == null ||
            string.IsNullOrWhiteSpace(datos.casoId) ||
            !Uri.TryCreate(datos.modeloUrl,
                UriKind.Absolute, out Uri uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            Debug.LogError("Datos del modelo inválidos");
            return;
        }

        // Evita solicitudes de dominios no autorizados.
        if (uri.Host != "TU-PROYECTO.web.app")
        {
            Debug.LogError("Origen del modelo no autorizado");
            return;
        }

        int solicitud = ++versionSolicitud;

        await CargarDesdeUrl(datos, solicitud);
    }

    private async Task CargarDesdeUrl(
        DatosModelo datos,
        int solicitud)
    {
        var importador = new GltfImport();
        GameObject contenedor = null;

        try
        {
            bool descargado =
                await importador.Load(datos.modeloUrl);

            if (solicitud != versionSolicitud)
                return;

            if (!descargado)
            {
                Debug.LogError("No se pudo descargar el GLB");
                return;
            }

            contenedor = new GameObject(
                "Modelo_" + datos.casoId
            );

            contenedor.transform.SetParent(
                transform, false
            );

            // Mantener oculto hasta completar la carga.
            contenedor.SetActive(false);

            bool instanciado =
                await importador.InstantiateMainSceneAsync(
                    contenedor.transform
                );

            if (solicitud != versionSolicitud)
                return;

            if (!instanciado)
            {
                Debug.LogError("No se pudo instanciar");
                return;
            }

            // Sustituir únicamente cuando el nuevo
            // modelo está completamente preparado.
            if (modeloActual != null)
                Destroy(modeloActual);

            if (importadorActual != null)
                importadorActual.Dispose();

            modeloActual = contenedor;
            importadorActual = importador;
            contenedor = null;
            importador = null;

            modeloActual.SetActive(true);

            Debug.Log(
                "Modelo cargado: " + datos.casoId
            );
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
        finally
        {
            if (contenedor != null)
                Destroy(contenedor);

            if (importador != null)
                importador.Dispose();
        }
    }

    private void OnDestroy()
    {
        versionSolicitud++;

        if (importadorActual != null)
            importadorActual.Dispose();
    }
}
