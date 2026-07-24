using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    // ???ng d?n k?t n?i tr?c ti?p t?i c?ng HTTP th??ng, lo?i b? ho�n to�n l?i ng?t k?t n?i HTTPS
    private string baseURL = "https://api.rhythmgame.id.vn/api/Auth/";

    private void Start()
    {
        // The auth scene always opens at Login. Players can explicitly choose
        // the existing Register action when they need a new account.
        SwitchToLoginPanel();
        CreateGuestButton();
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
        if (!ValidateRegisterInputs()) return;
        StartCoroutine(RegisterCoroutine());
    }

    bool ValidateRegisterInputs()
    {
        if (registerUsername == null || registerEmail == null || registerPassword == null)
        {
            ShowError("Register form is not configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(registerUsername.text))
        {
            ShowError("Please enter a username.");
            registerUsername.Select();
            return false;
        }

        if (string.IsNullOrWhiteSpace(registerEmail.text))
        {
            ShowError("Please enter your email.");
            registerEmail.Select();
            return false;
        }

        if (!registerEmail.text.Contains("@"))
        {
            ShowError("Please enter a valid email.");
            registerEmail.Select();
            return false;
        }

        if (string.IsNullOrWhiteSpace(registerPassword.text))
        {
            ShowError("Please enter a password.");
            registerPassword.Select();
            return false;
        }

        if (registerPassword.text.Length < 6)
        {
            ShowError("Password must be at least 6 characters.");
            registerPassword.Select();
            return false;
        }

        if (!ContainsUppercaseLetter(registerPassword.text))
        {
            ShowError("Password must contain at least 1 uppercase letter.");
            registerPassword.Select();
            return false;
        }

        return true;
    }

    IEnumerator RegisterCoroutine()
    {
        ShowNormal("Registering account...");

        // G�n ?�ng t�n bi?n vi?t HOA ?? .NET Core nh?n d?ng ???c
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
        if (!ValidateLoginInputs()) return;
        StartCoroutine(LoginCoroutine());
    }

    bool ValidateLoginInputs()
    {
        if (loginUsername == null || loginPassword == null)
        {
            ShowError("Login form is not configured.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(loginUsername.text))
        {
            ShowError("Please enter your username.");
            loginUsername.Select();
            return false;
        }

        if (string.IsNullOrWhiteSpace(loginPassword.text))
        {
            ShowError("Please enter your password.");
            loginPassword.Select();
            return false;
        }

        return true;
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
                AccountSession.SignIn(loginUsername.text);
                Invoke(nameof(LoadStartMenuScene), 1f);
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

    void ShowNormal(string msg) { if (messageText == null) return; messageText.text = msg; messageText.color = Color.white; }
    void ShowSuccess(string msg) { if (messageText == null) return; messageText.text = msg; messageText.color = Color.green; }
    void ShowError(string msg) { if (messageText == null) return; messageText.text = msg; messageText.color = Color.red; }
    void ClearMessage() { if (messageText != null) messageText.text = ""; }
    public void Logout()
    {
        AccountSession.SignOut();
        SwitchToLoginPanel();
        ShowSuccess("Logged out. Guest data is now active on this device.");
    }
    public void PlayAsGuest()
    {
        AccountSession.SignOut();
        ShowNormal("Starting guest session...");
        Invoke(nameof(LoadStartMenuScene), 0.35f);
    }

    private void CreateGuestButton()
    {
        if (loginPanel == null || loginPanel.transform.Find("Button_PlayAsGuest") != null)
            return;

        Transform registerButton = loginPanel.transform.Find("Button_SwitchToRegister");
        if (registerButton == null)
            return;

        GameObject guest = Instantiate(registerButton.gameObject, loginPanel.transform);
        guest.name = "Button_PlayAsGuest";
        RectTransform guestRect = guest.GetComponent<RectTransform>();
        guestRect.anchoredPosition = new Vector2(0f, -238f);

        Button button = guest.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(PlayAsGuest);

        TextMeshProUGUI label = guest.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "PLAY AS GUEST";
    }

    private void LoadStartMenuScene() => SceneLoadUtility.LoadSceneByName("StartMenu");

    private static bool ContainsUppercaseLetter(string value)
    {
        foreach (char character in value)
            if (char.IsUpper(character))
                return true;

        return false;
    }
}

// ================= C�C L?P ??I T??NG DATA CHUY?N ??I JSON =================
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
