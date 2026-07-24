using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Bridges completed plays to the demo rank API and reads its per-song Top 50.
/// The server API has no authentication token yet, so only signed-in accounts
/// are submitted; guest scores deliberately remain local.
/// </summary>
public sealed class OnlineLeaderboardService : MonoBehaviour
{
    private const string ApiBaseUrl = "https://api.rhythmgame.id.vn/api/Rank/";
    private const int TimeoutSeconds = 12;
    private const string SubmittedKeyPrefix = "OnlineRank.LastSubmitted.";

    public static OnlineLeaderboardService Instance { get; private set; }

    [Serializable]
    private class SubmitRankRequest
    {
        public string Username;
        public int Score;
        public string SongName;
        public float Accuracy;
        public int MaxCombo;
    }

    [Serializable]
    private class RemoteRankEntry
    {
        public string username;
        public string Username;
        public int score;
        public int Score;
        public float accuracy;
        public float Accuracy;
        public int maxCombo;
        public int MaxCombo;
        public string songName;
        public string SongName;
    }

    [Serializable]
    private class RemoteRankEntryList { public RemoteRankEntry[] items; }

    private bool _subscribed;
    private readonly HashSet<string> _pendingSubmissions = new();

    public static OnlineLeaderboardService EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        return new GameObject("Online Leaderboard Service").AddComponent<OnlineLeaderboardService>();
    }

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

    private void Start() => TrySubscribe();
    private void Update()
    {
        if (!_subscribed)
            TrySubscribe();
    }

    private void OnDestroy()
    {
        if (SaveManager.Instance != null && _subscribed)
            SaveManager.Instance.OnScoreSaved -= HandleScoreSaved;
    }

    private void TrySubscribe()
    {
        if (_subscribed || SaveManager.Instance == null)
            return;

        SaveManager.Instance.OnScoreSaved += HandleScoreSaved;
        _subscribed = true;
    }

    private void HandleScoreSaved(ScoreSavedArgs args)
    {
        SubmitIfImproved(args.SongGroupId, args.Difficulty, args.Score, args.Accuracy, args.MaxCombo);
    }

    public void SubmitIfImproved(string songGroupId, Difficulty difficulty, int score, float accuracy, int maxCombo, Action<bool> onComplete = null)
    {
        if (!AccountSession.IsSignedIn || string.IsNullOrWhiteSpace(songGroupId) || score <= 0)
        {
            onComplete?.Invoke(false);
            return;
        }

        string songName = SongKey.Build(songGroupId, difficulty);
        string submittedKey = AccountSession.ScopedKey(SubmittedKeyPrefix + songName);
        if (PlayerPrefs.GetInt(submittedKey, 0) >= score)
        {
            onComplete?.Invoke(false);
            return;
        }

        if (!_pendingSubmissions.Add(submittedKey))
            return;

        StartCoroutine(SubmitRoutine(new SubmitRankRequest
        {
            Username = AccountSession.CurrentUsername,
            Score = score,
            SongName = songName,
            Accuracy = accuracy,
            MaxCombo = Mathf.Max(0, maxCombo)
        }, submittedKey, onComplete));
    }

    public void FetchTop(string songGroupId, Difficulty difficulty, Action<List<LeaderboardEntry>> onComplete)
    {
        if (string.IsNullOrWhiteSpace(songGroupId))
        {
            onComplete?.Invoke(new List<LeaderboardEntry>());
            return;
        }

        StartCoroutine(FetchRoutine(SongKey.Build(songGroupId, difficulty), onComplete));
    }

    private IEnumerator SubmitRoutine(SubmitRankRequest payload, string submittedKey, Action<bool> onComplete)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        using UnityWebRequest request = new UnityWebRequest(ApiBaseUrl + "submit", UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(body),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = TimeoutSeconds
        };
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PlayerPrefs.SetInt(submittedKey, payload.Score);
            PlayerPrefs.Save();
            Debug.Log($"[OnlineLeaderboard] Submitted {payload.SongName}: {payload.Score}.");
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogWarning($"[OnlineLeaderboard] Submit failed: {request.error} {request.downloadHandler?.text}");
            onComplete?.Invoke(false);
        }

        _pendingSubmissions.Remove(submittedKey);
    }

    private IEnumerator FetchRoutine(string songName, Action<List<LeaderboardEntry>> onComplete)
    {
        string url = ApiBaseUrl + "leaderboard?limit=50&songName=" + UnityWebRequest.EscapeURL(songName);
        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = TimeoutSeconds;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[OnlineLeaderboard] Fetch failed: {request.error}");
            onComplete?.Invoke(new List<LeaderboardEntry>());
            yield break;
        }

        RemoteRankEntryList response = JsonUtility.FromJson<RemoteRankEntryList>("{\"items\":" + request.downloadHandler.text + "}");
        Dictionary<string, LeaderboardEntry> bestByPlayer = new(StringComparer.OrdinalIgnoreCase);
        if (response?.items != null)
        {
            foreach (RemoteRankEntry remote in response.items)
            {
                if (remote == null)
                    continue;

                string player = !string.IsNullOrWhiteSpace(remote.username) ? remote.username : remote.Username;
                int score = remote.score != 0 ? remote.score : remote.Score;
                float accuracy = remote.accuracy != 0f ? remote.accuracy : remote.Accuracy;
                if (string.IsNullOrWhiteSpace(player))
                    continue;

                LeaderboardEntry candidate = new LeaderboardEntry
                {
                    playerName = player,
                    score = score,
                    accuracy = accuracy,
                    rank = RankCalculator.Calculate(accuracy),
                    songKey = songName
                };

                if (!bestByPlayer.TryGetValue(player, out LeaderboardEntry best) || candidate.score > best.score)
                    bestByPlayer[player] = candidate;
            }
        }

        List<LeaderboardEntry> entries = new(bestByPlayer.Values);
        entries.Sort((left, right) => right.score.CompareTo(left.score));
        onComplete?.Invoke(entries);
    }
}
