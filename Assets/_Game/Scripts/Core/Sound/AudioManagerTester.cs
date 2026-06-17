using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTaskTester : MonoBehaviour
{
	// ==================================================
	// MUSIC
	// ==================================================

	[Header("Music")]

	[SerializeField]
	private AudioSource musicSource;

	// ==================================================
	// SFX CLIPS
	// ==================================================

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

	// ==================================================
	// POOL
	// ==================================================

	[Header("SFX Pool")]

	[SerializeField]
	private AudioSource sfxPrefab;

	[SerializeField]
	private int poolSize = 15;

	// ==================================================
	// VOLUME
	// ==================================================

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

	[SerializeField]
	private float volumeStep = 0.1f;

	// ==================================================
	// OFFSET
	// ==================================================

	[Header("Offset Test")]

	[SerializeField]
	private float userOffsetMs;

	private readonly List<float>
		offsetSamples =
			new List<float>();

	// ==================================================
	// POOL
	// ==================================================

	private readonly List<AudioSource>
		sfxPool =
			new List<AudioSource>();

	// ==================================================
	// UNITY
	// ==================================================

	private void Awake()
	{
		CreatePool();

		ApplyVolume();
	}

	private void Update()
	{
		if (Keyboard.current == null)
		{
			return;
		}

		HandleSFXTest();

		HandleMusicTest();

		HandleVolumeTest();

		HandleOffsetTest();
	}
	// ==================================================
	// POOL
	// ==================================================

	private void CreatePool()
	{
		for (
			int i = 0;
			i < poolSize;
			i++
		)
		{
			CreateNewSource();
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

		Debug.Log(
			"[Tester] Pool Expanded"
		);

		return CreateNewSource();
	}

	// ==================================================
	// SFX TEST
	// ==================================================

	private void HandleSFXTest()
	{
		if (
			Keyboard.current.digit1Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(perfectClip);

			Debug.Log(
				"Perfect SFX"
			);
		}

		if (
			Keyboard.current.digit2Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(greatClip);

			Debug.Log(
				"Great SFX"
			);
		}

		if (
			Keyboard.current.digit3Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(goodClip);

			Debug.Log(
				"Good SFX"
			);
		}

		if (
			Keyboard.current.digit4Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(missClip);

			Debug.Log(
				"Miss SFX"
			);
		}

		if (
			Keyboard.current.digit5Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(comboClip);

			Debug.Log(
				"Combo SFX"
			);
		}

		if (
			Keyboard.current.digit6Key
			.wasPressedThisFrame
		)
		{
			PlaySFX(menuClickClip);

			Debug.Log(
				"Menu Click SFX"
			);
		}
	}

	private void PlaySFX(
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

	// ==================================================
	// MUSIC TEST
	// ==================================================

	private void HandleMusicTest()
	{
		if (
			Keyboard.current.pKey
			.wasPressedThisFrame
		)
		{
			if (
				musicSource != null
			)
			{
				musicSource.Play();

				Debug.Log(
					"Play Music"
				);
			}
		}

		if (
			Keyboard.current.oKey
			.wasPressedThisFrame
		)
		{
			if (
				musicSource != null
			)
			{
				musicSource.Pause();

				Debug.Log(
					"Pause Music"
				);
			}
		}

		if (
			Keyboard.current.rKey
			.wasPressedThisFrame
		)
		{
			if (
				musicSource != null
			)
			{
				musicSource.UnPause();

				Debug.Log(
					"Resume Music"
				);
			}
		}

		if (
			Keyboard.current.tKey
			.wasPressedThisFrame
		)
		{
			if (
				musicSource != null
			)
			{
				musicSource.Stop();

				Debug.Log(
					"Stop Music"
				);
			}
		}
	}
	// ==================================================
	// VOLUME TEST
	// ==================================================

	private void HandleVolumeTest()
	{
		if (
			Keyboard.current.qKey
			.wasPressedThisFrame
		)
		{
			musicVolume += volumeStep;

			musicVolume =
				Mathf.Clamp01(
					musicVolume
				);

			ApplyVolume();

			Debug.Log(
				"Music Volume = "
				+ musicVolume
			);
		}

		if (
			Keyboard.current.aKey
			.wasPressedThisFrame
		)
		{
			musicVolume -= volumeStep;

			musicVolume =
				Mathf.Clamp01(
					musicVolume
				);

			ApplyVolume();

			Debug.Log(
				"Music Volume = "
				+ musicVolume
			);
		}

		if (
			Keyboard.current.wKey
			.wasPressedThisFrame
		)
		{
			sfxVolume += volumeStep;

			sfxVolume =
				Mathf.Clamp01(
					sfxVolume
				);

			Debug.Log(
				"SFX Volume = "
				+ sfxVolume
			);
		}

		if (
			Keyboard.current.sKey
			.wasPressedThisFrame
		)
		{
			sfxVolume -= volumeStep;

			sfxVolume =
				Mathf.Clamp01(
					sfxVolume
				);

			Debug.Log(
				"SFX Volume = "
				+ sfxVolume
			);
		}

		if (
			Keyboard.current.zKey
			.wasPressedThisFrame
		)
		{
			masterVolume += volumeStep;

			masterVolume =
				Mathf.Clamp01(
					masterVolume
				);

			ApplyVolume();

			Debug.Log(
				"Master Volume = "
				+ masterVolume
			);
		}

		if (
			Keyboard.current.xKey
			.wasPressedThisFrame
		)
		{
			masterVolume -= volumeStep;

			masterVolume =
				Mathf.Clamp01(
					masterVolume
				);

			ApplyVolume();

			Debug.Log(
				"Master Volume = "
				+ masterVolume
			);
		}
	}

	private void ApplyVolume()
	{
		if (
			musicSource != null
		)
		{
			musicSource.volume =
				musicVolume *
				masterVolume;
		}
	}

	// ==================================================
	// OFFSET TEST
	// ==================================================

	private void HandleOffsetTest()
	{
		if (
			Keyboard.current.spaceKey
			.wasPressedThisFrame
		)
		{
			float sample =
				Random.Range(
					-50f,
					50f
				);

			offsetSamples.Add(
				sample
			);

			float total = 0f;

			for (
				int i = 0;
				i < offsetSamples.Count;
				i++
			)
			{
				total +=
					offsetSamples[i];
			}

			userOffsetMs =
				total /
				offsetSamples.Count;

			Debug.Log(
				"Calculated Offset = "
				+ userOffsetMs
				+ " ms"
			);
		}
	}
}