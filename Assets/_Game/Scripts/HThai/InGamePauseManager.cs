using UnityEngine;
using UnityEngine.SceneManagement; // THÊM DÒNG NÀY ĐỂ DÙNG LỆNH LOAD SCENE

public class InGamePauseManager : MonoBehaviour
{
    [Header("--- GIAO DIỆN ---")]
    public GameObject pauseMenuPanel;    // Kéo PauseMenuPanel vào đây (thay vì SettingsPanel)

    [Header("--- ÂM THANH ---")]
    public AudioSource musicAudioSource;

    private bool isPaused = false;

    // Gắn vào nút Pause ( || ) ở góc màn hình
    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true); // Hiện bảng PAUSED
        Time.timeScale = 0f; // Dừng thời gian

        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause(); // Dừng nhạc
        }
    }

    // Gắn vào nút "Resume"
    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false); // Ẩn bảng PAUSED
        Time.timeScale = 1f; // Chạy lại thời gian

        if (musicAudioSource != null)
        {
            musicAudioSource.UnPause(); // Phát tiếp nhạc
        }
    }

    // Gắn vào nút "Retry"
    public void RetryGame()
    {
        Time.timeScale = 1f; // Bắt buộc phải trả lại thời gian trước khi load lại
        // Load lại chính Scene hiện tại đang chơi
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Gắn vào nút "Quit"
    public void QuitGame()
    {
        Time.timeScale = 1f;
        // Đổi "MainMenu" thành đúng tên Scene sảnh chờ của bạn
        SceneManager.LoadScene("SampleScene");
    }
}