using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class NeonButon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TextMeshProUGUI yazi;
    
    [ColorUsage(true, true)] 
    [Tooltip("Fareyle gelince patlayacak HDR renk")]
    public Color parlakRenk = Color.green; // Varsayılan olarak parlak yeşil
    
    private Color baslangicRengi;

    void Awake()
    {
        yazi = GetComponent<TextMeshProUGUI>();
        if (yazi != null)
        {
            baslangicRengi = yazi.color; // Sahnede ayarladığın orijinal rengi hafızaya al
            
            // Eğer Unity kaynaklı bir hatadan veya önceki scriptten dolayı 
            // renk tamamen görünmez (alpha = 0) kaydedildiyse, zorla görünür yap:
            if (baslangicRengi.a <= 0.05f) 
            {
                baslangicRengi = Color.white;
                yazi.color = Color.white;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (yazi != null)
        {
            Color neon = parlakRenk;
            neon.a = 1f; // Kullanıcı yanlışlıkla saydamlığı 0 yapmışsa zorla 1 (görünür) yap
            yazi.color = neon; // Üstüne gelince neon rengi yak
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (yazi != null)
        {
            yazi.color = baslangicRengi; // Fareyi çekince eski haline döndür
        }
    }
}