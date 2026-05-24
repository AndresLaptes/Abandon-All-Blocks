using UnityEngine;

public class CambiadorDeMusica : MonoBehaviour
{
    [Tooltip("Arrastra aquí el archivo de sonido (.mp3/.wav) que quieres que suene EN ESTA ESCENA")]
    public AudioClip musicaDeEstaEscena;

    void Start()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.CambiarMusica(musicaDeEstaEscena);
        }
    }
}