using UnityEngine;

public class ChartPlaybackClock : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float audioOffset = 0f;

    [Header("State")]
    [SerializeField] private bool playOnStart = true;



    private float _startTime;
    private float _pausedSongTime;
    private bool _isPlaying;

    public float SongTime
    {
        get
        {
            float rawTime;

            if (audioSource != null)
            {
                rawTime = audioSource.time;
            }
            else if (!_isPlaying)
            {
                rawTime = _pausedSongTime;
            }
            else
            {
                rawTime = Time.time - _startTime;
            }

            return rawTime + audioOffset;
        }
    }

    public bool IsPlaying => _isPlaying;

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    public void Play()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }

        _startTime = Time.time - _pausedSongTime;
        _isPlaying = true;
    }

    public void Pause()
    {
        _pausedSongTime = SongTime;
        _isPlaying = false;

        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    public void Stop()
    {
        _pausedSongTime = 0f;
        _isPlaying = false;

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}