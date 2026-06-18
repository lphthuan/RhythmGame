using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    public string targetScene = "StoreMenu";

    public void GoBack()
    {
        SceneManager.LoadScene(targetScene);
    }
}