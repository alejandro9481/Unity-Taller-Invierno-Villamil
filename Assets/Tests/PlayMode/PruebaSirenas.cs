using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Comprueba en Play que el script de las sirenas funciona como pide el taller.
/// El ensamblado de pruebas no puede referenciar los scripts del juego, así que el
/// componente se busca por nombre y la velocidad se lee por reflexión (eso además
/// confirma que rotationSpeed es una variable pública).
/// Se ejecuta desde Window > General > Test Runner > PlayMode.
/// </summary>
public class PruebaSirenas
{
    const string RutaEscena = "Assets/Scenes/EscenaInvernal.unity";

    [UnityTest]
    public IEnumerator LaSirenaGira()
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        yield return null;

        // El script existe una sola vez en la escena y está en Siren_Pivot.
        var scripts = Object.FindObjectsByType<MonoBehaviour>()
            .Where(m => m.GetType().Name == "SirenRotation").ToArray();
        Assert.AreEqual(1, scripts.Length, "Debe haber exactamente un SirenRotation en la escena.");
        var script = scripts[0];
        Assert.AreEqual("Siren_Pivot", script.gameObject.name, "SirenRotation debe estar en Siren_Pivot.");

        var campo = script.GetType().GetField("rotationSpeed");
        Assert.IsNotNull(campo, "rotationSpeed debe ser una variable pública.");
        float velocidad = (float)campo.GetValue(script);
        Assert.Greater(Mathf.Abs(velocidad), 0f, "rotationSpeed no puede ser 0.");

        var pivote = script.transform;
        var roja = GameObject.Find("Red_Light").transform;
        Assert.AreEqual(pivote, roja.parent, "Red_Light debe ser hija de Siren_Pivot.");
        Vector3 posicionInicial = roja.position;

        // Suma lo que giró el pivote cuadro a cuadro y el tiempo transcurrido.
        float girado = 0f, tiempo = 0f;
        float anguloAnterior = pivote.localEulerAngles.y;
        while (tiempo < 0.5f)
        {
            yield return null;
            tiempo += Time.deltaTime;
            float angulo = pivote.localEulerAngles.y;
            girado += Mathf.DeltaAngle(anguloAnterior, angulo);
            anguloAnterior = angulo;
        }

        // El giro debe ser velocidad × tiempo: eso solo pasa si usa Time.deltaTime.
        float esperado = velocidad * tiempo;
        Debug.Log($"[Prueba] Siren_Pivot giró {girado:F1}° en {tiempo:F2} s (esperado {esperado:F1}° a {velocidad}°/s).");
        Assert.AreEqual(esperado, girado, Mathf.Abs(esperado) * 0.05f,
            $"Giró {girado:F1}° en {tiempo:F2} s; con {velocidad}°/s se esperaban {esperado:F1}°.");

        // La luz roja se movió en círculo alrededor del pivote (sigue a la misma distancia).
        Assert.Greater(Vector3.Distance(posicionInicial, roja.position), 0.1f, "Red_Light no se movió.");
        Assert.AreEqual(0.35f, Vector3.Distance(pivote.position, roja.position), 0.01f);
    }
}
