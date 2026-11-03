// COLOCAR EN: Assets/Plugins/WebGL/MammoSimBridge.jslib   (crea las carpetas Plugins/WebGL si no existen)
// Unity llama a MammoSim_Evento y esto lo convierte en un evento del navegador: "mammosim:<nombre>".
mergeInto(LibraryManager.library, {
  MammoSim_Evento: function (nombre, datos) {
    var n = UTF8ToString(nombre);
    var d = UTF8ToString(datos);
    window.dispatchEvent(new CustomEvent("mammosim:" + n, { detail: d }));
  }
});
