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

    // ------------------------------------------------------------------ Etapa 2
    const string CarpetaMateriales = "Assets/Materials";
    const string RutaMallaCoche = "Assets/Models/PoliceCar.asset";

    [MenuItem("Taller/2. Terreno nevado y coche enterrado")]
    public static void Etapa2_TerrenoYCoche()
    {
        var escena = AbrirEscena();
        var nieve = Mat("Nieve", new Color(0.92f, 0.95f, 1.0f), 0.2f);

        // 3. Snow_Terrain: plano de 100 x 100 m en y = 0.
        var terreno = Buscar(escena, "Snow_Terrain");
        if (terreno == null)
        {
            terreno = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terreno.name = "Snow_Terrain";
        }
        terreno.transform.position = Vector3.zero;
        terreno.transform.localScale = new Vector3(10f, 1f, 10f);
        terreno.GetComponent<MeshRenderer>().sharedMaterial = nieve;

        // 4. Police_Car: objeto padre vacío, hundido en y = -0.5 para que parezca enterrado.
        var coche = Buscar(escena, "Police_Car") ?? new GameObject("Police_Car");
        coche.transform.position = new Vector3(0f, -0.5f, 0f);
        coche.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

        // 5. Car_Mesh: la carrocería, un solo objeto con una malla de varias piezas.
        var carroceria = Buscar(escena, "Car_Mesh") ?? new GameObject("Car_Mesh");
        carroceria.transform.SetParent(coche.transform, false);
        carroceria.transform.localPosition = Vector3.zero;
        carroceria.transform.localRotation = Quaternion.identity;
        var filtro = Componente<MeshFilter>(carroceria);
        var render = Componente<MeshRenderer>(carroceria);
        Material[] materiales;
        filtro.sharedMesh = CrearMallaCoche(nieve, out materiales);
        render.sharedMaterials = materiales;

        EncuadrarCamara(escena);

        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] Etapa 2 lista: terreno y coche enterrado");
    }

    // Encuadre diagonal desde atrás a la derecha: se ven el costado, el techo y el escape.
    // El fondo toma el color de la niebla para que el borde del terreno se pierda en la tormenta.
    static void EncuadrarCamara(Scene escena)
    {
        var camaraGO = Buscar(escena, "Main Camera");
        camaraGO.transform.position = new Vector3(4.6f, 2.1f, -5.6f);
        camaraGO.transform.LookAt(new Vector3(0f, 0.35f, 0f));
        camaraGO.GetComponent<Camera>().backgroundColor = RenderSettings.fogColor;
    }

    // Arma la patrulla con cajas y la guarda como una sola malla (un submesh por material).
    // Medidas en metros, en el espacio local de Police_Car (que está en y = -0.5):
    // la carrocería va de y = 0.3 a 1.0, así que la nieve le llega a media puerta.
    static Mesh CrearMallaCoche(Material nieve, out Material[] materiales)
    {
        var pintura = Mat("Patrulla", new Color(0.05f, 0.07f, 0.15f), 0.6f);
        var puertas = Mat("Patrulla_Blanco", new Color(0.88f, 0.90f, 0.93f), 0.55f);
        var vidrio = Mat("Vidrio", new Color(0.03f, 0.04f, 0.06f), 0.9f);
        var barra = Mat("Barra_Sirena", new Color(0.18f, 0.18f, 0.20f), 0.4f);
        var rojo = Mat("Sirena_Roja", new Color(0.6f, 0.02f, 0.02f), 0.8f, new Color(2.5f, 0.05f, 0.05f));
        var azul = Mat("Sirena_Azul", new Color(0.02f, 0.06f, 0.6f), 0.8f, new Color(0.05f, 0.25f, 2.5f));
        var traseras = Mat("Luz_Trasera", new Color(0.5f, 0.02f, 0.02f), 0.7f, new Color(1.2f, 0.02f, 0.02f));

        var piezas = new (Material mat, Vector3 centro, Vector3 tamano)[]
        {
            (pintura,  new Vector3(0f, 0.65f, 0f),      new Vector3(1.9f, 0.7f, 4.4f)),    // carrocería
            (puertas,  new Vector3(0f, 0.75f, -0.1f),   new Vector3(1.92f, 0.3f, 1.6f)),   // franja blanca de las puertas
            (vidrio,   new Vector3(0f, 1.275f, -0.2f),  new Vector3(1.7f, 0.55f, 2.2f)),   // cabina
            (nieve,    new Vector3(0f, 1.58f, -0.2f),   new Vector3(1.74f, 0.07f, 2.1f)),  // nieve sobre el techo
            (nieve,    new Vector3(0f, 1.04f, 1.6f),    new Vector3(1.86f, 0.08f, 1.1f)),  // nieve sobre el capó
            (nieve,    new Vector3(0f, 1.04f, -1.75f),  new Vector3(1.86f, 0.08f, 0.8f)),  // nieve sobre el maletero
            (barra,    new Vector3(0f, 1.64f, -0.2f),   new Vector3(1.3f, 0.06f, 0.34f)),  // base de la barra de luces
            (rojo,     new Vector3(0.32f, 1.72f, -0.2f),  new Vector3(0.6f, 0.12f, 0.3f)), // cúpula roja
            (azul,     new Vector3(-0.32f, 1.72f, -0.2f), new Vector3(0.6f, 0.12f, 0.3f)), // cúpula azul
            (traseras, new Vector3(0.68f, 0.85f, -2.21f),  new Vector3(0.36f, 0.12f, 0.04f)), // luces traseras
            (traseras, new Vector3(-0.68f, 0.85f, -2.21f), new Vector3(0.36f, 0.12f, 0.04f)),
        };

        var cubo = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var porMaterial = piezas.GroupBy(p => p.mat).ToList();
        var grupos = new CombineInstance[porMaterial.Count];
        for (int i = 0; i < porMaterial.Count; i++)
        {
            var partes = porMaterial[i].Select(p => new CombineInstance
            {
                mesh = cubo,
                transform = Matrix4x4.TRS(p.centro, Quaternion.identity, p.tamano)
            }).ToArray();
            var malla = new Mesh();
            malla.CombineMeshes(partes, true, true);
            grupos[i] = new CombineInstance { mesh = malla, transform = Matrix4x4.identity };
        }

        var final = new Mesh { name = "PoliceCar" };
        final.CombineMeshes(grupos, false, true);   // false: un submesh por material
        final.RecalculateBounds();
        materiales = porMaterial.Select(g => g.Key).ToArray();

        System.IO.Directory.CreateDirectory("Assets/Models");
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMallaCoche);
        if (existente != null)
        {
            EditorUtility.CopySerialized(final, existente);
            AssetDatabase.SaveAssets();
            return existente;
        }
        AssetDatabase.CreateAsset(final, RutaMallaCoche);
        return final;
    }

    // Material URP/Lit (el shader Standard se vería magenta en URP).
    static Material Mat(string nombre, Color color, float suavidad, Color? emision = null)
    {
        System.IO.Directory.CreateDirectory(CarpetaMateriales);
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = mat == null;
        if (nuevo) mat = new Material(shader);
        mat.shader = shader;
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", suavidad);
        if (emision.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emision.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (nuevo) AssetDatabase.CreateAsset(mat, ruta);
        else EditorUtility.SetDirty(mat);
        return mat;
    }

    // ------------------------------------------------------------------ Etapa 3
    [MenuItem("Taller/3. Luces y sirenas de emergencia")]
    public static void Etapa3_Sirenas()
    {
        var escena = AbrirEscena();
        var coche = Buscar(escena, "Police_Car");

        // 6. Siren_Pivot: vacío en el centro de la barra de luces del techo. El script se agrega en la etapa 4.
        var pivote = Buscar(escena, "Siren_Pivot") ?? new GameObject("Siren_Pivot");
        pivote.transform.SetParent(coche.transform, false);
        pivote.transform.localPosition = new Vector3(0f, 1.72f, -0.2f);
        pivote.transform.localRotation = Quaternion.identity;

        // 7 y 8. Red_Light y Blue_Light: spots hijos del pivote, apuntando a lados opuestos
        // e inclinados un poco hacia abajo para que el haz barra la nieve al girar.
        CrearSirena(escena, pivote.transform, "Red_Light", new Color(1f, 0.05f, 0.05f), +1f);
        CrearSirena(escena, pivote.transform, "Blue_Light", new Color(0.05f, 0.2f, 1f), -1f);

        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] Etapa 3 lista: pivote y luces de las sirenas");
    }

    static void CrearSirena(Scene escena, Transform pivote, string nombre, Color color, float lado)
    {
        var go = Buscar(escena, nombre) ?? new GameObject(nombre);
        go.transform.SetParent(pivote, false);
        go.transform.localPosition = new Vector3(0.35f * lado, 0f, 0f);
        go.transform.localRotation = Quaternion.Euler(15f, 90f * lado, 0f);
        var luz = Componente<Light>(go);
        luz.type = LightType.Spot;
        luz.color = color;
        luz.intensity = 25f;
        luz.range = 25f;
        luz.spotAngle = 70f;
        luz.innerSpotAngle = 30f;
        luz.shadows = LightShadows.None;   // sin sombras: la barra de luces no tapa el haz
    }

    // ------------------------------------------------------------------ Etapa 4
    [MenuItem("Taller/4. Asignar SirenRotation al pivote")]
    public static void Etapa4_Script()
    {
        var escena = AbrirEscena();
        var pivote = Buscar(escena, "Siren_Pivot");
        // El script va únicamente en Siren_Pivot: las luces giran por ser sus hijas.
        var giro = Componente<SirenRotation>(pivote);
        giro.rotationSpeed = 180f;   // media vuelta por segundo
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] Etapa 4 lista: SirenRotation en Siren_Pivot");
    }

    // ------------------------------------------------------------------ Etapa 5
    // Material de partículas por defecto de URP (Particles/Unlit, transparente, textura suave).
    const string GuidParticulaURP = "e823cd5b5d27c0f4b8256e7c12ee3e6d";

    [MenuItem("Taller/5. Partículas de nieve y humo")]
    public static void Etapa5_Particulas()
    {
        var escena = AbrirEscena();

        // 9. Exhaust_Smoke: hijo de Police_Car, en el escape trasero, apenas sobre la nieve.
        var coche = Buscar(escena, "Police_Car");
        var humoGO = Buscar(escena, "Exhaust_Smoke") ?? new GameObject("Exhaust_Smoke");
        humoGO.transform.SetParent(coche.transform, false);
        humoGO.transform.localPosition = new Vector3(-0.55f, 0.65f, -2.25f);
        humoGO.transform.localRotation = Quaternion.Euler(-25f, 180f, 0f);   // hacia atrás y un poco arriba
        ConfigurarHumo(Componente<ParticleSystem>(humoGO), MatParticula("Humo_Particula", Color.white));

        // 10. Global_Blizzard: independiente, a 14 m de altura, emitiendo hacia abajo.
        var nieveGO = Buscar(escena, "Global_Blizzard") ?? new GameObject("Global_Blizzard");
        nieveGO.transform.SetParent(null);
        nieveGO.transform.position = new Vector3(-3f, 12f, 4f);   // corrida contra el viento
        nieveGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ConfigurarNevada(Componente<ParticleSystem>(nieveGO), MatParticula("Nieve_Particula", Color.white));

        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] Etapa 5 lista: nevada y humo del escape");
    }

    static void ConfigurarNevada(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.prewarm = true;                                             // al dar Play ya está nevando
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);      // velocidad inicial variable
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startColor = new Color(0.85f, 0.93f, 1f, 0.9f);              // blanco con tono azul claro
        main.gravityModifier = 0.1f;                                     // gravedad moderada
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20000;

        var emision = ps.emission;
        emision.enabled = true;
        emision.rateOverTime = 3500f;                                    // tormenta densa

        // Caja de 30 x 30 m: con la rotación X = 90 queda horizontal y emite hacia abajo.
        var forma = ps.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(30f, 30f, 1f);

        // Viento lateral: la nieve cae en diagonal, como en una ventisca.
        var velocidad = ps.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.space = ParticleSystemSimulationSpace.World;
        velocidad.x = new ParticleSystem.MinMaxCurve(2.5f);
        velocidad.y = new ParticleSystem.MinMaxCurve(0f);
        velocidad.z = new ParticleSystem.MinMaxCurve(0f);

        // Remolinos suaves para que los copos no caigan en línea recta.
        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.6f;
        ruido.frequency = 0.3f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sharedMaterial = material;
        render.shadowCastingMode = ShadowCastingMode.Off;
        render.receiveShadows = false;
    }

    static void ConfigurarHumo(ParticleSystem ps, Material material)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startColor = Color.white;                                   // el gris lo pone Color over Lifetime
        main.gravityModifier = -0.05f;                                   // el humo caliente sube
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        // Emisión constante más ráfagas irregulares: el motor "tose" por el frío.
        var emision = ps.emission;
        emision.enabled = true;
        emision.rateOverTime = 12f;
        emision.SetBursts(new[]
        {
            new ParticleSystem.Burst(0.0f, 6, 10),
            new ParticleSystem.Burst(1.3f, 6, 10),
            new ParticleSystem.Burst(2.1f, 6, 10),
            new ParticleSystem.Burst(3.9f, 6, 10),
        });

        // Cono muy estrecho: sale del tubo de escape.
        var forma = ps.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 6f;
        forma.radius = 0.04f;

        // De gris oscuro a transparente.
        var colorVida = ps.colorOverLifetime;
        colorVida.enabled = true;
        var degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(new Color(0.3f, 0.3f, 0.32f), 0f), new GradientColorKey(new Color(0.62f, 0.63f, 0.66f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.1f), new GradientAlphaKey(0f, 1f) });
        colorVida.color = new ParticleSystem.MinMaxGradient(degradado);

        // Tamaño creciente: la bocanada se expande al enfriarse (0.5x a 2.5x).
        var tamanoVida = ps.sizeOverLifetime;
        tamanoVida.enabled = true;
        tamanoVida.size = new ParticleSystem.MinMaxCurve(2.5f, AnimationCurve.Linear(0f, 0.2f, 1f, 1f));

        // El mismo viento que la nieve, más suave.
        var velocidad = ps.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.space = ParticleSystemSimulationSpace.World;
        velocidad.x = new ParticleSystem.MinMaxCurve(1.2f);
        velocidad.y = new ParticleSystem.MinMaxCurve(0f);
        velocidad.z = new ParticleSystem.MinMaxCurve(0f);

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.3f;
        ruido.frequency = 0.5f;

        var render = ps.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sharedMaterial = material;
        render.shadowCastingMode = ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.sortingFudge = -10f;   // se dibuja delante de la nieve cercana
    }

    // Copia del material de partículas de URP guardada en Assets/Materials.
    static Material MatParticula(string nombre, Color color)
    {
        System.IO.Directory.CreateDirectory(CarpetaMateriales);
        string ruta = CarpetaMateriales + "/" + nombre + ".mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;
        var baseURP = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(GuidParticulaURP));
        if (baseURP == null) throw new System.Exception("No se encontró el material de partículas de URP.");
        var mat = new Material(baseURP) { name = nombre };
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // ------------------------------------------------------------------ utilidades
    // Abre la escena de la etapa anterior; las etapas 2 a 5 parten de ella.
    static Scene AbrirEscena()
    {
        return EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
    }

    // Devuelve el componente si ya existe o lo agrega (para no duplicarlo al repetir una etapa).
    static T Componente<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();
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
