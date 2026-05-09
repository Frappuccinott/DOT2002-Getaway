using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PortableRadio : MonoBehaviour
{
    [Header("Radyo Müzik Ayarları")]
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private bool shuffleOrder = false;

    private AudioSource audioSource;
    private int currentSongIndex = 0;
    private bool isPlaying = false;
    private bool isLoading = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; 
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    private void Start()
    {
        StartCoroutine(InitRadioRoutine());
    }

    private IEnumerator InitRadioRoutine()
    {
        // Wait briefly for MusicManager to be ready
        yield return new WaitForSeconds(0.1f);
        
        if (MusicManager.Instance == null) yield break;

        if (shuffleOrder && MusicManager.Instance.SongPaths.Count > 0)
        {
            MusicManager.Instance.ShufflePlaylist();
        }

        if (autoPlay && MusicManager.Instance.SongPaths.Count > 0)
        {
            PlaySong(currentSongIndex);
        }
    }

    private void Update()
    {
        if (MusicManager.Instance == null || MusicManager.Instance.SongPaths.Count == 0 || isLoading) return;

        if (isPlaying && !audioSource.isPlaying)
        {
            if (!Application.isFocused || Time.timeScale == 0f) return;
            NextSong();
        }
    }

    public void PlaySong(int index)
    {
        if (MusicManager.Instance == null) return;
        
        var paths = MusicManager.Instance.SongPaths;
        var names = MusicManager.Instance.SongNames;

        if (paths.Count == 0) return;

        if (index < 0 || index >= paths.Count)
            index = 0;

        currentSongIndex = index;
        isLoading = true;
        
        MusicManager.Instance.LoadAndPlayAudio(paths[currentSongIndex], names[currentSongIndex], audioSource, (success) => {
            isLoading = false;
            if (success) isPlaying = true;
        });
    }

    public void NextSong()
    {
        if (MusicManager.Instance == null || MusicManager.Instance.SongPaths.Count == 0 || isLoading) return;

        currentSongIndex++;
        if (currentSongIndex >= MusicManager.Instance.SongPaths.Count)
        {
            currentSongIndex = 0;
            if (shuffleOrder) MusicManager.Instance.ShufflePlaylist(); 
        }

        PlaySong(currentSongIndex);
    }

    public void ToggleRadio()
    {
        if (MusicManager.Instance == null || MusicManager.Instance.SongPaths.Count == 0) return;

        isPlaying = !isPlaying;

        if (isPlaying)
        {
            if (audioSource.clip == null)
                PlaySong(currentSongIndex);
            else
                audioSource.UnPause();
        }
        else
        {
            audioSource.Pause();
        }
    }
}
