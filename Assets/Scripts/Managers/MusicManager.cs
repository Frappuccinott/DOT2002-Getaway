using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public List<string> SongPaths { get; private set; } = new List<string>();
    public List<string> SongNames { get; private set; } = new List<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("[MusicManager]");
            go.AddComponent<MusicManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSongsFromDocuments();
    }

    private void LoadSongsFromDocuments()
    {
        string docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        string musicFolderPath = Path.Combine(docsPath, "Getaway Music");

        if (!Directory.Exists(musicFolderPath))
        {
            Directory.CreateDirectory(musicFolderPath);
            Debug.Log("[MusicManager] Müzik klasörü oluşturuldu: " + musicFolderPath);
            return;
        }

        string[] files = Directory.GetFiles(musicFolderPath);
        foreach (string file in files)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".mp3" || ext == ".wav" || ext == ".ogg")
            {
                SongPaths.Add(file);
                SongNames.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
    }

    public void ShufflePlaylist()
    {
        for (int i = 0; i < SongPaths.Count; i++)
        {
            string tempPath = SongPaths[i];
            string tempName = SongNames[i];
            
            int randomIndex = Random.Range(i, SongPaths.Count);
            
            SongPaths[i] = SongPaths[randomIndex];
            SongNames[i] = SongNames[randomIndex];
            
            SongPaths[randomIndex] = tempPath;
            SongNames[randomIndex] = tempName;
        }
    }

    public void LoadAndPlayAudio(string filePath, string songName, AudioSource source, System.Action<bool> onComplete)
    {
        StartCoroutine(LoadAudioRoutine(filePath, songName, source, onComplete));
    }

    private IEnumerator LoadAudioRoutine(string filePath, string songName, AudioSource source, System.Action<bool> onComplete)
    {
        string url = "file:///" + filePath.Replace("\\", "/");
        AudioType audioType = AudioType.UNKNOWN;
        
        string ext = Path.GetExtension(filePath).ToLower();
        if (ext == ".mp3") audioType = AudioType.MPEG;
        else if (ext == ".wav") audioType = AudioType.WAV;
        else if (ext == ".ogg") audioType = AudioType.OGGVORBIS;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[MusicManager] Ses yükleme hatası: " + www.error);
                onComplete?.Invoke(false);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                clip.name = songName;
                source.clip = clip;
                source.Play();
                onComplete?.Invoke(true);
            }
        }
    }
}
