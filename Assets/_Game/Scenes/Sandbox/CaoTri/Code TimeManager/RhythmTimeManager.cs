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
	// BPM
	// ==========================================================

	[Header("Song Settings")]

	[SerializeField]
	private float bpm = 120f;

	// ==========================================================
	// Offset
	// ==========================================================

	[Header("Offset")]

	[SerializeField]
	private float userOffsetMs = 0f;

	// ==========================================================
	// DSP Timing
	// ==========================================================

	// DSP song start time
	private double dspSongStartTime;

	// DSP pause time
	private double pauseDSPTime;

	// Detect pause/resume
	private bool wasPlaying;

	// ==========================================================
	// Runtime Values
	// ==========================================================

	// Current song time in seconds
	public double SongPositionSeconds
	{
		get;
		private set;
	}

	// Beat duration
	public double SecondsPerBeat =>
		60.0 / bpm;

	// Current song position in beats
	public double SongPositionInBeats =>
		SongPositionSeconds /
		SecondsPerBeat;

	// Song progress 0 -> 1
	public double SongProgress =>
		SongPositionSeconds /
		AudioManager.Instance
			.GetMusicSource()
			.clip.length;

	// Song finished
	public bool IsSongFinished =>
		SongProgress >= 1.0;

	// ==========================================================
	// UNITY
	// ==========================================================

	private void Awake()
	{
		// Singleton setup
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);

			return;
		}

		Instance = this;

		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		// ==========================================================
		// ESC = Pause / Resume
		// ==========================================================

		if (Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			AudioSource source =
				AudioManager.Instance
					.GetMusicSource();

			if (source.isPlaying)
			{
				PauseGame();

				Debug.Log("PAUSE");
			}
			else
			{
				ResumeGame();

				Debug.Log("RESUME");
			}
		}

		// ==========================================================
		// HANDLE DSP RESYNC
		// ==========================================================

		HandlePauseResume();

		AudioSource musicSource =
			AudioManager.Instance
				.GetMusicSource();

		// Stop update if paused
		if (!musicSource.isPlaying)
			return;

		// ==========================================================
		// DSP TIME
		// ==========================================================

		SongPositionSeconds =
			(
				AudioSettings.dspTime
				- dspSongStartTime
			)
			+ (userOffsetMs / 1000.0);

		// ==========================================================
		// DEBUG
		// ==========================================================

		Debug.Log(
			"Time: "
			+ SongPositionSeconds.ToString("F2")
			+ " | Beat: "
			+ SongPositionInBeats.ToString("F2")
			+ " | Progress: "
			+ (SongProgress * 100f).ToString("F0")
			+ "%"
		);

		// ==========================================================
		// SONG FINISHED
		// ==========================================================

		if (IsSongFinished)
		{
			Debug.Log(
				"SONG FINISHED"
			);
		}
	}

	// ==========================================================
	// START SONG
	// ==========================================================

	public void StartSong()
	{
		dspSongStartTime =
			AudioSettings.dspTime;

		AudioManager.Instance.PlayMusic();

		wasPlaying = true;

		Debug.Log("SONG START");
	}

	// ==========================================================
	// SET BPM
	// ==========================================================

	public void SetBPM(float newBpm)
	{
		bpm = newBpm;

		Debug.Log(
			"NEW BPM: " + bpm
		);
	}

	// ==========================================================
	// HANDLE PAUSE / RESUME DSP SYNC
	// ==========================================================

	private void HandlePauseResume()
	{
		AudioSource music =
			AudioManager.Instance
				.GetMusicSource();

		bool isPlayingNow =
			music.isPlaying;

		// ==========================================================
		// PAUSE DETECTED
		// ==========================================================

		if (wasPlaying && !isPlayingNow)
		{
			pauseDSPTime =
				AudioSettings.dspTime;

			Debug.Log(
				"DSP PAUSE DETECTED"
			);
		}

		// ==========================================================
		// RESUME DETECTED
		// ==========================================================

		if (!wasPlaying && isPlayingNow)
		{
			double pausedDuration =
				AudioSettings.dspTime
				- pauseDSPTime;

			// Prevent desync
			dspSongStartTime +=
				pausedDuration;

			Debug.Log(
				"DSP RESYNC COMPLETE"
			);
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

		Time.timeScale = 0f;
	}

	// ==========================================================
	// RESUME
	// ==========================================================

	public void ResumeGame()
	{
		AudioManager.Instance
			.ResumeMusic();

		Time.timeScale = 1f;
	}

	// ==========================================================
	// OFFSET
	// ==========================================================

	public void SetOffset(float offsetMs)
	{
		userOffsetMs = offsetMs;

		Debug.Log(
			"OFFSET: "
			+ userOffsetMs
			+ " ms"
		);
	}
}