using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Comprueba en Play la jerarquía exacta de 10 objetos del taller y las dos partículas.
/// Se ejecuta desde Window > General > Test Runner > PlayMode.
/// </summary>
public class PruebaEscena
{
    const string RutaEscena = "Assets/Scenes/EscenaInvernal.unity";

    // Nombre del objeto -> nombre de su padre ("" = raíz), según la tabla del enunciado.
    static readonly Dictionary<string, string> Jerarquia = new Dictionary<string, string>
    {
        { "Main Camera", "" },
        { "Moon_Light", "" },
        { "Snow_Terrain", "" },
        { "Police_Car", "" },
        { "Car_Mesh", "Police_Car" },
        { "Siren_Pivot", "Police_Car" },
        { "Red_Light", "Siren_Pivot" },
        { "Blue_Light", "Siren_Pivot" },
        { "Exhaust_Smoke", "Police_Car" },
        { "Global_Blizzard", "" },
    };

    static IEnumerator CargarEscena()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        yield return null;
    }

    [UnityTest]
    public IEnumerator LaJerarquiaEsLaDelTaller()
    {
        yield return CargarEscena();
        var escena = SceneManager.GetSceneByPath(RutaEscena);
        var todos = escena.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject).ToList();

        Assert.AreEqual(10, todos.Count,
            "Debe haber exactamente 10 objetos. Hay: " + string.Join(", ", todos.Select(g => g.name)));
        foreach (var par in Jerarquia)
        {
            var go = todos.FirstOrDefault(g => g.name == par.Key);
            Assert.IsNotNull(go, "Falta el objeto " + par.Key);
            string padre = go.transform.parent == null ? "" : go.transform.parent.name;
            Assert.AreEqual(par.Value, padre, "Padre incorrecto para " + par.Key);
        }

        Assert.Less(GameObject.Find("Police_Car").transform.position.y, 0f, "Police_Car debe estar enterrado (Y negativa).");
        Assert.IsNotNull(GameObject.Find("Main Camera").GetComponent<Camera>());
        Assert.AreEqual(LightType.Directional, GameObject.Find("Moon_Light").GetComponent<Light>().type);
        Assert.IsNotNull(GameObject.Find("Car_Mesh").GetComponent<MeshRenderer>());
        Assert.IsNotNull(GameObject.Find("Snow_Terrain").GetComponent<MeshRenderer>());
        foreach (var nombre in new[] { "Red_Light", "Blue_Light" })
        {
            var luz = GameObject.Find(nombre).GetComponent<Light>();
            Assert.IsTrue(luz.type == LightType.Spot || luz.type == LightType.Point, nombre + " debe ser Spot o Point.");
        }
        // Las dos sirenas apuntan a lados opuestos.
        float apuntan = Vector3.Dot(GameObject.Find("Red_Light").transform.forward, GameObject.Find("Blue_Light").transform.forward);
        Assert.Less(apuntan, 0f, "Red_Light y Blue_Light deben apuntar a lados opuestos.");
    }

    [UnityTest]
    public IEnumerator LasParticulasEmiten()
    {
        yield return CargarEscena();
        yield return new WaitForSeconds(2f);

        var nieve = GameObject.Find("Global_Blizzard").GetComponent<ParticleSystem>();
        var humo = GameObject.Find("Exhaust_Smoke").GetComponent<ParticleSystem>();
        Assert.IsNotNull(nieve, "Global_Blizzard debe tener un ParticleSystem.");
        Assert.IsNotNull(humo, "Exhaust_Smoke debe tener un ParticleSystem.");
        Debug.Log($"[Prueba] Partículas vivas a los 2 s: nieve {nieve.particleCount}, humo {humo.particleCount}.");

        Assert.Greater(nieve.particleCount, 0, "La nieve no está emitiendo.");
        Assert.Greater(humo.particleCount, 0, "El humo no está emitiendo.");
        Assert.AreEqual(ParticleSystemShapeType.Box, nieve.shape.shapeType);
        Assert.AreEqual(ParticleSystemShapeType.Cone, humo.shape.shapeType);
        Assert.IsTrue(humo.colorOverLifetime.enabled, "El humo debe usar Color over Lifetime.");
        Assert.IsTrue(humo.sizeOverLifetime.enabled, "El humo debe usar Size over Lifetime.");
        Assert.Greater(nieve.main.gravityModifier.constant, 0f, "La nieve debe caer con gravedad.");
        Assert.IsNull(nieve.transform.parent, "Global_Blizzard debe ser independiente.");

        // La nieve cae: la velocidad media de las partículas apunta hacia abajo.
        var particulas = new ParticleSystem.Particle[nieve.particleCount];
        int n = nieve.GetParticles(particulas);
        float vy = particulas.Take(n).Average(p => p.velocity.y + p.animatedVelocity.y);
        Assert.Less(vy, 0f, "Los copos deben ir hacia abajo.");
    }
}
