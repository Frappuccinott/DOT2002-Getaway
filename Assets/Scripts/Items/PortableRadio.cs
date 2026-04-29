using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PortableRadio : MonoBehaviour
{
    [Header("Radyo Müzik Ayarları")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private bool shuffleOrder = false;

    private AudioSource audioSource;
    private int currentSongIndex = 0;
    private bool isPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f; 

        if (shuffleOrder && playlist != null && playlist.Length > 0)
        {
            ShufflePlaylist();
        }
    }

    private void Start()
    {
        if (autoPlay && playlist != null && playlist.Length > 0)
        {
            PlaySong(currentSongIndex);
        }
    }

    private void Update()
    {
        if (playlist == null || playlist.Length == 0) return;

        if (isPlaying && !audioSource.isPlaying)
        {
            NextSong();
        }
    }

    public void PlaySong(int index)
    {
        if (playlist == null || playlist.Length == 0) return;

        if (index < 0 || index >= playlist.Length)
            index = 0;

        currentSongIndex = index;
        audioSource.clip = playlist[currentSongIndex];
        audioSource.Play();
        isPlaying = true;
    }

    public void NextSong()
    {
        if (playlist == null || playlist.Length == 0) return;

        currentSongIndex++;
        if (currentSongIndex >= playlist.Length)
        {
            currentSongIndex = 0;
            if (shuffleOrder) ShufflePlaylist(); 
        }

        PlaySong(currentSongIndex);
    }

    public void ToggleRadio()
    {
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

    private void ShufflePlaylist()
    {
        for (int i = 0; i < playlist.Length; i++)
        {
            AudioClip temp = playlist[i];
            int randomIndex = Random.Range(i, playlist.Length);
            playlist[i] = playlist[randomIndex];
            playlist[randomIndex] = temp;
        }
    }
}
