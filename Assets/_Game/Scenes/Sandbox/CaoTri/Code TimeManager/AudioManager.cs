using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	// Music
	// ==========================================================

	[Header("Music")]
	[SerializeField]
	private AudioSource musicSource;

	// ==========================================================
	// SFX Pool
	// ==========================================================

	[Header("SFX")]
	[SerializeField]
	private AudioSource sfxPrefab;

	[SerializeField]
	private int poolSize = 10;

	private Queue<AudioSource> sfxPool =
		new Queue<AudioSource>();

	// ==========================================================
	// Volume
	// ==========================================================

	[Header("Volume")]

	[Range(0f, 1f)]
	[SerializeField]
	private float musicVolume = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	private float sfxVolume = 1f;

	// ==========================================================
	// UNITY
	// ==========================================================

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);

			return;
		}

		Instance = this;

		DontDestroyOnLoad(gameObject);

		CreatePool();
	}

	// ==========================================================
	// CREATE SFX POOL
	// ==========================================================

	private void CreatePool()
	{
		for (int i = 0; i < poolSize; i++)
		{
			AudioSource source =
				Instantiate(
					sfxPrefab,
					transform
				);

			source.gameObject.SetActive(false);

			sfxPool.Enqueue(source);
		}
	}

	// ==========================================================
	// PLAY MUSIC
	// ==========================================================

	public void PlayMusic()
	{
		if (musicSource.clip == null)
		{
			Debug.LogError(
				"No Music Clip"
			);

			return;
		}

		musicSource.volume =
			musicVolume;

		musicSource.Play();
	}

	// ==========================================================
	// STOP MUSIC
	// ==========================================================

	public void StopMusic()
	{
		musicSource.Stop();
	}

	// ==========================================================
	// PAUSE MUSIC
	// ==========================================================

	public void PauseMusic()
	{
		musicSource.Pause();
	}

	// ==========================================================
	// RESUME MUSIC
	// ==========================================================

	public void ResumeMusic()
	{
		musicSource.UnPause();
	}

	// ==========================================================
	// PLAY SFX
	// ==========================================================

	public void PlaySFX(AudioClip clip)
	{
		AudioSource source =
			GetPooledSource();

		source.clip = clip;

		source.volume = sfxVolume;

		source.gameObject.SetActive(true);

		source.Play();

		StartCoroutine(
			ReturnToPool(
				source,
				clip.length
			)
		);
	}

	// ==========================================================
	// GET POOLED SOURCE
	// ==========================================================

	private AudioSource GetPooledSource()
	{
		AudioSource source =
			sfxPool.Dequeue();

		sfxPool.Enqueue(source);

		return source;
	}

	// ==========================================================
	// RETURN TO POOL
	// ==========================================================

	private IEnumerator ReturnToPool(
		AudioSource source,
		float delay
	)
	{
		yield return new WaitForSecondsRealtime(
			delay
		);

		source.Stop();

		source.gameObject.SetActive(false);
	}

	// ==========================================================
	// GET MUSIC SOURCE
	// ==========================================================

	public AudioSource GetMusicSource()
	{
		return musicSource;
	}

	// ==========================================================
	// VOLUME
	// ==========================================================

	public void SetMusicVolume(float value)
	{
		musicVolume = value;

		musicSource.volume =
			musicVolume;
	}

	public void SetSFXVolume(float value)
	{
		sfxVolume = value;
	}
}