using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;

public class AuthManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject updatePanel;

    [Header("--- Register Inputs ---")]
    public TMP_InputField registerUsername;
    public TMP_InputField registerEmail;
    public TMP_InputField registerPassword;

    [Header("--- Login Inputs ---")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;

    [Header("--- Update Inputs ---")]
    public TMP_InputField updateEmail;
    public TMP_InputField updateNewUsername;
    public TMP_InputField updateNewPassword;

    [Header("--- Global Message Text ---")]
    public TextMeshProUGUI messageText;

    // ???ng d?n k?t n?i tr?c ti?p t?i c?ng HTTP th??ng, lo?i b? hoàn toàn l?i ng?t k?t n?i HTTPS
    private string baseURL = "http://localhost:5231/api/auth/";

    private void Start()
    {
        SwitchToLoginPanel();
    }

    public void SwitchToRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        if (updatePanel != null) updatePanel.SetActive(false);
        ClearMessage();
    }

    public void SwitchToLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (updatePanel != null) updatePanel.SetActive(false);
        ClearMessage();
    }

    public void Register()
    {
        StartCoroutine(RegisterCoroutine());
    }

    IEnumerator RegisterCoroutine()
    {
        ShowNormal("Registering account...");

        // Gán ?úng tên bi?n vi?t HOA ?? .NET Core nh?n d?ng ???c
        RegisterData data = new RegisterData
        {
            Username = registerUsername.text,
            Email = registerEmail.text,
            Password = registerPassword.text
        };

        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest("register", json, (isSuccess, responseText) => {
            if (isSuccess)
            {
                ShowSuccess("Register successful!");
                Invoke("SwitchToLoginPanel", 1.5f);
            }
            else
            {
                ShowError(responseText);
            }
        }));
    }

    public void Login()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        ShowNormal("Logging in...");

        LoginData data = new LoginData
        {
            Username = loginUsername.text,
            Password = loginPassword.text
        };

        string json = JsonUtility.ToJson(data);

        yield return StartCoroutine(PostRequest("login", json, (isSuccess, responseText) => {
            if (isSuccess)
            {
                ShowSuccess("Login successful!");
                PlayerPrefs.SetString("UserMode", "Member");
                PlayerPrefs.SetString("CurrentUsername", loginUsername.text);
                PlayerPrefs.Save();
                Invoke("LoadGameplayScene", 1f);
            }
            else
            {
                ShowError(responseText);
            }
        }));
    }

    IEnumerator PostRequest(string endpoint, string json, System.Action<bool, string> callback)
    {
        string fullURL = (baseURL + endpoint).Trim();

        UnityWebRequest request = new UnityWebRequest(fullURL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorResponse = (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                ? request.downloadHandler.text
                : "Cannot connect to server! Error: " + request.error;

            callback?.Invoke(false, errorResponse);
        }
        else
        {
            callback?.Invoke(true, request.downloadHandler.text);
        }
    }

    void ShowNormal(string msg) { messageText.text = msg; messageText.color = Color.white; }
    void ShowSuccess(string msg) { messageText.text = msg; messageText.color = Color.green; }
    void ShowError(string msg) { messageText.text = msg; messageText.color = Color.red; }
    void ClearMessage() { messageText.text = ""; }
    void LoadGameplayScene() { SceneManager.LoadScene("MainMenu"); }
}

// ================= CÁC L?P ??I T??NG DATA CHUY?N ??I JSON =================
[System.Serializable]
public class RegisterData
{
    public string Username;
    public string Email;
    public string Password;
}

[System.Serializable]
public class LoginData
{
    public string Username;
    public string Password;
}