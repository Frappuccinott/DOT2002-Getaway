using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class NeonButon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    TextMeshProUGUI yazi;
    
    [Tooltip("Düz dururkenki soluk renk")]
    public Color normalRenk = new Color(1f, 1f, 1f, 0.5f); // Yarı saydam beyaz
    
    [ColorUsage(true, true)] 
    [Tooltip("Fareyle gelince patlayacak HDR renk")]
    public Color parlakRenk; 

    void Start()
    {
        yazi = GetComponent<TextMeshProUGUI>();
        yazi.color = normalRenk; // Başlangıç rengini ayarla
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        yazi.color = parlakRenk; // Üstüne gelince yak
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        yazi.color = normalRenk; // Çekince söndür
    }
}