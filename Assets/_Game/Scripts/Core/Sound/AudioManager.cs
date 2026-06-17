using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class AudioManager : MonoBehaviour
{
	// ==========================================================
	// Singleton
	// ==========================================================

	public static AudioManager Instance
	{
		get;
		private set;
	}

	// ==========================================================
	// ENUM
	// ==========================================================

	public enum SFXType
	{
		Perfect,
		Great,
		Good,
		Miss,
		MenuClick,
		Combo
	}

	// ==========================================================
	// MUSIC
	// ==========================================================

	[Header("Music")]

	[SerializeField]
	private AudioSource musicSource;

	// ==========================================================
	// SFX
	// ==========================================================

	[Header("SFX Pool")]

	[SerializeField]
	private AudioSource sfxPrefab;

	[SerializeField]
	private int initialPoolSize = 15;

	// ==========================================================
	// GAMEPLAY SFX
	// ==========================================================

	[Header("Gameplay SFX")]

	[SerializeField]
	private AudioClip perfectClip;

	[SerializeField]
	private AudioClip greatClip;

	[SerializeField]
	private AudioClip goodClip;

	[SerializeField]
	private AudioClip missClip;

	[SerializeField]
	private AudioClip comboClip;

	[SerializeField]
	private AudioClip menuClickClip;

	// ==========================================================
	// VOLUME
	// ==========================================================

	[Header("Volume")]

	[Range(0f, 1f)]
	[SerializeField]
	private float masterVolume = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	private float musicVolume = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	private float sfxVolume = 1f;

	// ==========================================================
	// DEBUG
	// ==========================================================

	[Header("Debug")]

	[SerializeField]
	private bool enableDebugLog = false;

	// ==========================================================
	// POOL
	// ==========================================================

	private readonly List<AudioSource>
		sfxPool =
			new List<AudioSource>();

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

		CreateInitialPool();

		LoadVolumeSettings();

		if (musicSource == null)
		{
			Debug.LogError(
				"[AudioManager] MusicSource Missing"
			);
		}
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
	// POOL
	// ==========================================================

	private void CreateInitialPool()
	{
		for (
			int i = 0;
			i < initialPoolSize;
			i++
		)
		{
			CreateNewSource();
		}

		if (enableDebugLog)
		{
			Debug.Log(
				"[AudioManager] Pool Created"
			);
		}
	}

	private AudioSource CreateNewSource()
	{
		AudioSource source =
			Instantiate(
				sfxPrefab,
				transform
			);

		source.playOnAwake = false;

		sfxPool.Add(source);

		return source;
	}

	private AudioSource GetAvailableSource()
	{
		for (
			int i = 0;
			i < sfxPool.Count;
			i++
		)
		{
			if (!sfxPool[i].isPlaying)
			{
				return sfxPool[i];
			}
		}

		if (enableDebugLog)
		{
			Debug.Log(
				"[AudioManager] Pool Expanded"
			);
		}

		return CreateNewSource();
	}

	// ==========================================================
	// MUSIC
	// ==========================================================

	public void LoadMusic(
		AudioClip clip
	)
	{
		if (
			musicSource == null ||
			clip == null
		)
		{
			Debug.LogError(
				"[AudioManager] Invalid Music"
			);

			return;
		}

		musicSource.clip = clip;
	}

	public void PlayMusic()
	{
		if (
			musicSource == null ||
			musicSource.clip == null
		)
		{
			Debug.LogError(
				"[AudioManager] Missing Music"
			);

			return;
		}

		musicSource.volume =
			musicVolume *
			masterVolume;

		musicSource.Play();

		if (enableDebugLog)
		{
			Debug.Log(
				"[AudioManager] Music Play"
			);
		}
	}
	// ==========================================================
	// DSP PLAY
	// ==========================================================

	public void PlayMusicScheduled(
		double dspStartTime
	)
	{
		if (
			musicSource == null ||
			musicSource.clip == null
		)
		{
			Debug.LogError(
				"[AudioManager] Missing Music"
			);

			return;
		}

		musicSource.volume =
			musicVolume *
			masterVolume;

		musicSource.PlayScheduled(
			dspStartTime
		);

		if (enableDebugLog)
		{
			Debug.Log(
				"[AudioManager] DSP Scheduled"
			);
		}
	}

	public void StopMusic()
	{
		if (musicSource == null)
		{
			return;
		}

		musicSource.Stop();
	}

	public void PauseMusic()
	{
		if (musicSource == null)
		{
			return;
		}

		musicSource.Pause();
	}

	public void ResumeMusic()
	{
		if (musicSource == null)
		{
			return;
		}

		musicSource.UnPause();
	}

	// ==========================================================
	// GAMEPLAY SFX
	// ==========================================================

	public void PlayGameplaySFX(
		SFXType type
	)
	{
		switch (type)
		{
			case SFXType.Perfect:
				PlaySFX(perfectClip);
				break;

			case SFXType.Great:
				PlaySFX(greatClip);
				break;

			case SFXType.Good:
				PlaySFX(goodClip);
				break;

			case SFXType.Miss:
				PlaySFX(missClip);
				break;

			case SFXType.Combo:
				PlaySFX(comboClip);
				break;

			case SFXType.MenuClick:
				PlaySFX(menuClickClip);
				break;
		}
	}

	// ==========================================================
	// PLAY SFX
	// ==========================================================

	public void PlaySFX(
		AudioClip clip
	)
	{
		if (clip == null)
		{
			return;
		}

		AudioSource source =
			GetAvailableSource();

		source.clip = clip;

		source.volume =
			sfxVolume *
			masterVolume;

		source.Play();
	}

	// ==========================================================
	// VOLUME
	// ==========================================================

	public void SetMasterVolume(
		float value
	)
	{
		masterVolume =
			Mathf.Clamp01(value);

		ApplyVolume();

		SaveVolumeSettings();
	}

	public void SetMusicVolume(
		float value
	)
	{
		musicVolume =
			Mathf.Clamp01(value);

		ApplyVolume();

		SaveVolumeSettings();
	}

	public void SetSFXVolume(
		float value
	)
	{
		sfxVolume =
			Mathf.Clamp01(value);

		SaveVolumeSettings();
	}
	private void ApplyVolume()
	{
		if (musicSource != null)
		{
			musicSource.volume =
				musicVolume *
				masterVolume;
		}
	}

	// ==========================================================
	// SAVE / LOAD
	// ==========================================================

	private void SaveVolumeSettings()
	{
		PlayerPrefs.SetFloat(
			"MASTER_VOLUME",
			masterVolume
		);

		PlayerPrefs.SetFloat(
			"MUSIC_VOLUME",
			musicVolume
		);

		PlayerPrefs.SetFloat(
			"SFX_VOLUME",
			sfxVolume
		);

		PlayerPrefs.Save();
	}

	private void LoadVolumeSettings()
	{
		masterVolume =
			PlayerPrefs.GetFloat(
				"MASTER_VOLUME",
				1f
			);

		musicVolume =
			PlayerPrefs.GetFloat(
				"MUSIC_VOLUME",
				1f
			);

		sfxVolume =
			PlayerPrefs.GetFloat(
				"SFX_VOLUME",
				1f
			);

		ApplyVolume();
	}

	// ==========================================================
	// MUTE
	// ==========================================================

	public void ToggleMusic(
		bool enable
	)
	{
		if (musicSource == null)
		{
			return;
		}

		musicSource.mute =
			!enable;
	}

	public void ToggleSFX(
		bool enable
	)
	{
		AudioListener.pause =
			!enable;
	}

	// ==========================================================
	// GETTERS
	// ==========================================================

	public AudioSource GetMusicSource()
	{
		return musicSource;
	}

	public float GetMasterVolume()
	{
		return masterVolume;
	}

	public float GetMusicVolume()
	{
		return musicVolume;
	}

	public float GetSFXVolume()
	{
		return sfxVolume;
	}
}