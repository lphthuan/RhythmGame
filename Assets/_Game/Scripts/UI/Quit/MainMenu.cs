using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public AudioSource buttonSound;

    public void QuitGame()
    {
        StartCoroutine(QuitSequence());
    }

    IEnumerator QuitSequence()
    {
        // Phát âm thanh khi bấm
        if (buttonSound != null)
            buttonSound.Play();

        // Chờ âm thanh phát một chút
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Thanks for playing!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}