using UnityEngine;
using UnityEngine.SceneManagement;
using RhythmGame.Common.Interfaces;

public class GameManager : MonoBehaviour, IGameFlowController
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private string lobbySceneName = "MusicSelectionScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RetryGame()
    {
        // Đảm bảo game tiếp tục chạy
        Time.timeScale = 1f;

        // Tắt nhạc (AudioManager không bị xoá khi đổi Scene)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Tải lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToLobby()
    {
        // Đảm bảo game tiếp tục chạy
        Time.timeScale = 1f;

        // Tắt nhạc
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Chuyển về Scene chính
        SceneManager.LoadScene(lobbySceneName);
    }
}
