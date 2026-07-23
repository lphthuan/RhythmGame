using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Daily progression stored locally until the account service is authoritative.
/// A date is always calculated in Vietnam time (UTC+7), never the device's
/// local timezone. Server integration can replace TodayKey/claim validation.
/// </summary>
public static class DailyQuestService
{
    private const string StorageKey = "RhythmGame.DailyQuests.v1";
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public enum QuestId { PlaySongs, ClearSong, AllPerfect }

    [Serializable]
    public class QuestState
    {
        public string id;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    private class DailyState
    {
        public string vietnamDate;
        public List<QuestState> quests = new();
    }

    public static event Action Changed;
    public static string TodayKey => DateTimeOffset.UtcNow.ToOffset(VietnamOffset).ToString("yyyy-MM-dd");

    public static IReadOnlyList<QuestState> GetQuests() => EnsureToday().quests;

    public static int GetTarget(QuestId id) => id switch
    {
        QuestId.PlaySongs => 3,
        QuestId.ClearSong => 1,
        QuestId.AllPerfect => 1,
        _ => 1
    };

    public static int GetReward(QuestId id) => id switch
    {
        QuestId.PlaySongs => 250,
        QuestId.ClearSong => 400,
        QuestId.AllPerfect => 750,
        _ => 0
    };

    public static string GetTitle(QuestId id) => id switch
    {
        QuestId.PlaySongs => "Chơi 3 bài nhạc",
        QuestId.ClearSong => "Hoàn thành 1 bài nhạc",
        QuestId.AllPerfect => "Đạt All Perfect 1 lần",
        _ => string.Empty
    };

    public static void RecordResult(bool passed, bool isAllPerfect)
    {
        AddProgress(QuestId.PlaySongs, 1);
        if (passed) AddProgress(QuestId.ClearSong, 1);
        if (isAllPerfect) AddProgress(QuestId.AllPerfect, 1);
    }

    public static bool TryClaim(QuestId id, out string message)
    {
        DailyState state = EnsureToday();
        QuestState quest = GetQuest(state, id);
        if (quest.claimed)
        {
            message = "Phần thưởng này đã nhận.";
            return false;
        }

        if (quest.progress < GetTarget(id))
        {
            message = "Nhiệm vụ chưa hoàn thành.";
            return false;
        }

        quest.claimed = true;
        int reward = GetReward(id);
        PlayerWallet.Add(CurrencyType.Money, reward);
        WalletTransactionJournal.Record("daily_quest_claim:" + TodayKey, CurrencyType.Money, reward, id.ToString());
        Save(state);
        message = $"Đã nhận {reward} Money.";
        return true;
    }

    private static void AddProgress(QuestId id, int amount)
    {
        DailyState state = EnsureToday();
        QuestState quest = GetQuest(state, id);
        quest.progress = Mathf.Min(GetTarget(id), quest.progress + Mathf.Max(0, amount));
        Save(state);
    }

    private static DailyState EnsureToday()
    {
        DailyState state = Load();
        if (state.vietnamDate == TodayKey && state.quests.Count == 3)
            return state;

        state = new DailyState { vietnamDate = TodayKey };
        foreach (QuestId id in Enum.GetValues(typeof(QuestId)))
            state.quests.Add(new QuestState { id = id.ToString() });
        Save(state);
        return state;
    }

    private static QuestState GetQuest(DailyState state, QuestId id)
    {
        string idText = id.ToString();
        QuestState quest = state.quests.Find(x => x.id == idText);
        if (quest != null) return quest;
        quest = new QuestState { id = idText };
        state.quests.Add(quest);
        return quest;
    }

    private static DailyState Load()
    {
        string json = PlayerPrefs.GetString(StorageKey, string.Empty);
        DailyState state = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<DailyState>(json);
        return state ?? new DailyState();
    }

    private static void Save(DailyState state)
    {
        PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
