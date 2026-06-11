using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RhythmSFXReceiver :
	MonoBehaviour,
	INoteResultReceiver
{
	public void OnNoteFinished(
		NoteBase note,
		NoteResult result
	)
	{
		Debug.Log("=== SFX RECEIVER CALLED ===");

		if (note == null)
		{
			Debug.LogError("NOTE NULL");
			return;
		}

		Debug.Log(
			"Result: " + result +
			" | Judgment: " + note.LastJudgment
		);

		if (AudioManager.Instance == null)
		{
			Debug.LogError("AUDIOMANAGER NULL");
			return;
		}

		switch (note.LastJudgment)
		{
			case HitJudgment.Perfect:

				Debug.Log("PLAY PERFECT");

				AudioManager.Instance
					.PlayGameplaySFX(
						AudioManager.SFXType.Perfect
					);

				break;

			case HitJudgment.Great:

				Debug.Log("PLAY GREAT");

				AudioManager.Instance
					.PlayGameplaySFX(
						AudioManager.SFXType.Great
					);

				break;

			case HitJudgment.Good:

				Debug.Log("PLAY GOOD");

				AudioManager.Instance
					.PlayGameplaySFX(
						AudioManager.SFXType.Good
					);

				break;

			case HitJudgment.Miss:

				Debug.Log("PLAY MISS");

				AudioManager.Instance
					.PlayGameplaySFX(
						AudioManager.SFXType.Miss
					);

				break;

			default:

				Debug.LogWarning(
					"UNKNOWN JUDGMENT: " +
					note.LastJudgment
				);

				break;
		}
	}
}