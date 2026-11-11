using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Construye la escena invernal por etapas, una por commit del taller.
// Cada etapa abre la escena, agrega solo lo suyo (sin duplicar) y guarda.
// Se puede usar desde el menú Taller o en modo batch con -executeMethod.
public static class ConstructorEscena
{
    public const string RutaEscena = "Assets/Scenes/EscenaInvernal.unity";

    // ------------------------------------------------------------------ Etapa 1
    [MenuItem("Taller/1. Escena vacía (cámara y luz de luna)")]
    public static void Etapa1_EscenaVacia()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Main Camera: encuadre diagonal hacia el coche.
        var camaraGO = new GameObject("Main Camera");
        camaraGO.tag = "MainCamera";
        var camara = camaraGO.AddComponent<Camera>();
        camaraGO.AddComponent<AudioListener>();
        camara.fieldOfView = 50f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.03f, 0.05f, 0.09f);
        camara.nearClipPlane = 0.1f;
        camara.farClipPlane = 200f;
        camaraGO.transform.position = new Vector3(6.5f, 2.6f, -7.5f);
        camaraGO.transform.LookAt(new Vector3(0f, 0.6f, 0f));

        // 2. Moon_Light: luz general azul muy pálido y de baja intensidad.
        var lunaGO = new GameObject("Moon_Light");
        var luna = lunaGO.AddComponent<Light>();
        luna.type = LightType.Directional;
        luna.color = new Color(0.75f, 0.85f, 1.0f);
        luna.intensity = 0.35f;
        luna.shadows = LightShadows.Soft;
        lunaGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        AplicarAmbiente();

        EditorSceneManager.SaveScene(escena, RutaEscena);
        RegistrarEnBuildSettings();
        Debug.Log("[Taller] Etapa 1 lista: " + RutaEscena);
    }

    // Noche de tormenta: niebla azulada y luz ambiente tenue. No son objetos de la jerarquía.
    static void AplicarAmbiente()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.09f, 0.11f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.20f);
        RenderSettings.fogDensity = 0.035f;
    }

    static void RegistrarEnBuildSettings()
    {
        var escenas = EditorBuildSettings.scenes.ToList();
        if (escenas.All(e => e.path != RutaEscena))
        {
            escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }

    // ------------------------------------------------------------------ utilidades
    // Abre la escena de la etapa anterior; las etapas 2 a 5 parten de ella.
    static Scene AbrirEscena()
    {
        return EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
    }

    // Busca un objeto por nombre en toda la escena, incluidos los hijos.
    static GameObject Buscar(Scene escena, string nombre)
    {
        foreach (var raiz in escena.GetRootGameObjects())
        {
            if (raiz.name == nombre) return raiz;
            var hijo = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == nombre);
            if (hijo != null) return hijo.gameObject;
        }
        return null;
    }
}
