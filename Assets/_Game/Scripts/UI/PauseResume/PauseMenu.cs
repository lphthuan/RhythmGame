using UnityEngine;
using System.Collections;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    // Text đếm ngược
    public TextMeshProUGUI countdownText;

    private bool isPaused = false;

    // Khi bấm nút Setting
    public void PauseGame()
    {
        pausePanel.SetActive(true);

        countdownText.gameObject.SetActive(false);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        isPaused = true;

        Debug.Log("Paused");
    }

    // Khi bấm Resume
    public void ResumeGame()
    {
        StartCoroutine(ResumeCountdown());
    }

    IEnumerator ResumeCountdown()
    {
        countdownText.gameObject.SetActive(true);

        // Đếm ngược 3 -> 2 -> 1
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();

            Debug.Log("Game chạy sau: " + i);

            yield return new WaitForSecondsRealtime(1f);
        }

        countdownText.text = "GO!";

        Debug.Log("Game tiếp tục");

        yield return new WaitForSecondsRealtime(0.5f);

        countdownText.gameObject.SetActive(false);

        pausePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        isPaused = false;
    }

    // Tự pause khi Alt Tab
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !isPaused)
        {
            PauseGame();
        }
    }
}