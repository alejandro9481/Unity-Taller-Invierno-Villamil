using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Guarda en Capturas/ una imagen de lo que ve Main Camera.
// Uso en batch: -executeMethod Capturador.Capturar -captura 02_terreno_y_coche
// Opcional: -simular 5 avanza las partículas esos segundos antes de la foto.
public static class Capturador
{
    [MenuItem("Taller/Capturar imagen de la cámara")]
    public static void Capturar()
    {
        string nombre = Argumento("-captura") ?? "captura_" + DateTime.Now.ToString("HHmmss");
        float segundos = float.TryParse(Argumento("-simular"), out var s) ? s : 0f;

        EditorSceneManager.OpenScene(ConstructorEscena.RutaEscena, OpenSceneMode.Single);
        if (segundos > 0f)
            foreach (var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>())
                ps.Simulate(segundos, true, true);

        var camara = Camera.main;
        if (camara == null) throw new Exception("No hay Main Camera en la escena.");

        const int ancho = 1920, alto = 1080;
        var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            antiAliasing = 4
        };
        var anterior = camara.targetTexture;
        camara.targetTexture = rt;
        // Cuadros de calentamiento: en modo batch el primer render puede salir con los
        // datos de un solo material (el SRP Batcher todavía no cargó los demás).
        for (int i = 0; i < 3; i++) camara.Render();
        camara.targetTexture = anterior;

        var activa = RenderTexture.active;
        RenderTexture.active = rt;
        var imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        imagen.Apply();
        RenderTexture.active = activa;

        Directory.CreateDirectory("Capturas");
        string ruta = Path.Combine("Capturas", nombre + ".png");
        File.WriteAllBytes(ruta, imagen.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(imagen);
        rt.Release();
        Debug.Log("[Taller] Captura guardada: " + ruta);
    }

    static string Argumento(string clave)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, clave);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
