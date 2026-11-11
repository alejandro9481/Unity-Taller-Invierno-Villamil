using UnityEngine;

// Hace girar el pivote de las sirenas. Red_Light y Blue_Light son hijas de este objeto,
// así que giran con él y sus haces barren la nieve en círculo.
// Va asignado únicamente a Siren_Pivot.
public class SirenRotation : MonoBehaviour
{
    // Velocidad de giro en grados por segundo. Se ajusta desde el Inspector.
    public float rotationSpeed = 180f;

    void Update()
    {
        // Multiplicar por Time.deltaTime hace que el giro dependa del tiempo y no de los
        // fotogramas por segundo: a 30 o a 144 FPS gira lo mismo en un segundo.
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
