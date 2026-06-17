using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmTimeManager : MonoBehaviour
{
	// ==========================================================
	// Singleton
	// ==========================================================

	public static RhythmTimeManager Instance
	{
		get;
		private set;
	}

	// ==========================================================
	// SONG SETTINGS
	// ==========================================================

	[Header("Song")]

	[SerializeField]
	private float bpm = 120f;

	[SerializeField]
	private float userOffsetMs = 0f;

	[SerializeField]
	private double songStartDelay = 1.0;

	// ==========================================================
	// DEBUG
	// ==========================================================

	[Header("Debug")]

	[SerializeField]
	private bool enableDebugLog = false;

	[SerializeField]
	private float debugInterval = 0.5f;

	// ==========================================================
	// DSP
	// ==========================================================

	private double dspSongStartTime;

	private double pauseDSPTime;

	private bool wasPlaying;

	private bool isPaused;

	// ==========================================================
	// DEBUG
	// ==========================================================

	private float debugTimer;

	// ==========================================================
	// PROPERTIES
	// ==========================================================

	public double SongPositionSeconds
	{
		get;
		private set;
	}

	public double SecondsPerBeat =>
		60.0 / bpm;

	public double SongPositionInBeats =>
		SongPositionSeconds /
		SecondsPerBeat;

	public bool IsSongFinished =>
		SongProgress >= 1.0;

	public double SongProgress
	{
		get
		{
			AudioSource source =
				AudioManager.Instance?
					.GetMusicSource();

			if (
				source == null ||
				source.clip == null
			)
			{
				return 0;
			}

			return Mathf.Clamp01(
				(float)(
					SongPositionSeconds /
					source.clip.length
				)
			);
		}
	}

	// ==========================================================
	// UNITY
	// ==========================================================

	private void Awake()
	{
		InitializeSingleton();

		if (Instance != this)
		{
			return;
		}

		LoadOffset();
	}

	private void Update()
	{
		if (
			AudioManager.Instance == null
		)
		{
			return;
		}

		AudioSource musicSource =
			AudioManager.Instance
				.GetMusicSource();

		if (musicSource == null)
		{
			return;
		}

		HandlePauseInput();

		HandlePauseResume();

		if (!musicSource.isPlaying)
		{
			return;
		}

		UpdateSongPosition();

		HandleDebug();

		CheckSongFinished();
	}
	// ==========================================================
	// SINGLETON
	// ==========================================================

	private void InitializeSingleton()
	{
		if (
			Instance != null &&
			Instance != this
		)
		{
			Destroy(gameObject);

			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}
	// ==========================================================
	// STARTSONG
	// ==========================================================
	public void StartSong()
	{
		if (AudioManager.Instance == null)
		{
			Debug.LogError(
				"[RhythmTimeManager] Missing AudioManager"
			);

			return;
		}

		dspSongStartTime =
			AudioSettings.dspTime
			+ songStartDelay;

		AudioManager.Instance
			.PlayMusicScheduled(
				dspSongStartTime
			);

		wasPlaying = true;

		isPaused = false;
	}
	private void Start()
	{
		if (Instance != this)
		{
			return;
		}

		if (AudioManager.Instance == null)
		{
			Debug.LogError(
				"[RhythmTimeManager] AudioManager Missing"
			);

			return;
		}

		AudioSource musicSource =
			AudioManager.Instance
				.GetMusicSource();

		if (musicSource == null)
		{
			Debug.LogError(
				"[RhythmTimeManager] MusicSource Missing"
			);

			return;
		}

		if (musicSource.clip == null)
		{
			Debug.LogError(
				"[RhythmTimeManager] Music Clip Missing"
			);

			return;
		}

		Debug.Log(
			"[RhythmTimeManager] Auto Start Song"
		);

		StartSong();
	}

	// ==========================================================
	// SONG POSITION
	// ==========================================================

	private void UpdateSongPosition()
	{
		SongPositionSeconds =
			(
				AudioSettings.dspTime
				- dspSongStartTime
			)
			+ (
				userOffsetMs / 1000.0
			);

		if (SongPositionSeconds < 0)
		{
			SongPositionSeconds = 0;
		}
	}

	// ==========================================================
	// PAUSE INPUT
	// ==========================================================

	private void HandlePauseInput()
	{
		if (
			Keyboard.current == null
		)
		{
			return;
		}

		if (
			Keyboard.current
				.escapeKey
				.wasPressedThisFrame
		)
		{
			if (isPaused)
			{
				ResumeGame();
			}
			else
			{
				PauseGame();
			}
		}
	}

	// ==========================================================
	// DSP RESYNC
	// ==========================================================

	private void HandlePauseResume()
	{
		AudioSource musicSource =
			AudioManager.Instance
				.GetMusicSource();

		bool isPlayingNow =
			musicSource.isPlaying;

		if (
			wasPlaying &&
			!isPlayingNow
		)
		{
			pauseDSPTime =
				AudioSettings.dspTime;

			if (enableDebugLog)
			{
				Debug.Log(
					"[RhythmTimeManager] Pause DSP"
				);
			}
		}

		if (
			!wasPlaying &&
			isPlayingNow
		)
		{
			double pausedDuration =
				AudioSettings.dspTime
				- pauseDSPTime;

			dspSongStartTime +=
				pausedDuration;

			if (enableDebugLog)
			{
				Debug.Log(
					"[RhythmTimeManager] DSP Resync"
				);
			}
		}

		wasPlaying = isPlayingNow;
	}

	// ==========================================================
	// PAUSE
	// ==========================================================

	public void PauseGame()
	{
		AudioManager.Instance
			.PauseMusic();

		isPaused = true;

		if (enableDebugLog)
		{
			Debug.Log(
				"[RhythmTimeManager] Pause"
			);
		}
	}

	// ==========================================================
	// RESUME
	// ==========================================================

	public void ResumeGame()
	{
		AudioManager.Instance
			.ResumeMusic();

		isPaused = false;

		if (enableDebugLog)
		{
			Debug.Log(
				"[RhythmTimeManager] Resume"
			);
		}
	}
	// ==========================================================
	// BPM
	// ==========================================================

	public void SetBPM(
		float newBpm
	)
	{
		if (newBpm <= 0)
		{
			return;
		}

		bpm = newBpm;
	}

	public float GetBPM()
	{
		return bpm;
	}

	// ==========================================================
	// OFFSET
	// ==========================================================

	public void SetOffset(
		float offsetMs
	)
	{
		userOffsetMs = offsetMs;

		PlayerPrefs.SetFloat(
			"USER_OFFSET",
			userOffsetMs
		);

		PlayerPrefs.Save();

		if (enableDebugLog)
		{
			Debug.Log(
				"[RhythmTimeManager] Offset = "
				+ userOffsetMs
				+ " ms"
			);
		}
	}

	public float GetOffset()
	{
		return userOffsetMs;
	}

	public void LoadOffset()
	{
		userOffsetMs =
			PlayerPrefs.GetFloat(
				"USER_OFFSET",
				0f
			);
	}

	// ==========================================================
	// DEBUG
	// ==========================================================

	private void HandleDebug()
	{
		if (!enableDebugLog)
		{
			return;
		}

		debugTimer +=
			Time.unscaledDeltaTime;

		if (debugTimer < debugInterval)
		{
			return;
		}

		debugTimer = 0f;

		Debug.Log(
			"Song Time: "
			+ SongPositionSeconds
				.ToString("F3")
			+ " | Beat: "
			+ SongPositionInBeats
				.ToString("F2")
			+ " | Progress: "
			+ (
				SongProgress * 100f
			).ToString("F0")
			+ "%"
			+ " | Offset: "
			+ userOffsetMs
			+ " ms"
		);
	}

	// ==========================================================
	// SONG FINISH
	// ==========================================================

	private void CheckSongFinished()
	{
		if (
			IsSongFinished &&
			enableDebugLog
		)
		{
			Debug.Log(
				"[RhythmTimeManager] Song Finished"
			);
		}
	}

	// ==========================================================
	// RESET
	// ==========================================================

	public void ResetSongTime()
	{
		SongPositionSeconds = 0;

		dspSongStartTime = 0;

		pauseDSPTime = 0;

		isPaused = false;

		wasPlaying = false;
	}
}