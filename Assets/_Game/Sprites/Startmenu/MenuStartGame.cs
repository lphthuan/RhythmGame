using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuStartGame : MonoBehaviour
{
    public GameObject settingPanel;

    // START
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

   
    // EXIT
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}