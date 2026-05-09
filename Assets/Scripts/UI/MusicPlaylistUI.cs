using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;

public class MusicPlaylistUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playlistText;

    private void Start()
    {
        // Wait a small frame for MusicManager to initialize and load files
        StartCoroutine(UpdatePlaylistRoutine());
    }

    private IEnumerator UpdatePlaylistRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        UpdatePlaylistUI();
    }

    public void UpdatePlaylistUI()
    {
        if (playlistText == null)
        {
            playlistText = GetComponent<TMP_Text>();
            if (playlistText == null) return;
        }

        if (MusicManager.Instance == null)
        {
            playlistText.text = "Sistem Yükleniyor...";
            return;
        }

        var names = MusicManager.Instance.SongNames;
        if (names.Count == 0)
        {
            playlistText.text = "Belgeler/Getaway Music klasöründe müzik bulunamadı.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>MÜZİK LİSTESİ:</b>");
        for (int i = 0; i < names.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {names[i]}");
        }

        playlistText.text = sb.ToString();
    }
}
