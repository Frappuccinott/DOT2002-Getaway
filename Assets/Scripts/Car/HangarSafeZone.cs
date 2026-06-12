using UnityEngine;

/// <summary>
/// Hangar Güvenli Bölge Tetikleyicisi
/// ====================================
/// 
/// Bu script hangar/garaj objesine eklenir.
/// Hangar objesinde "Is Trigger = true" olan büyük bir BoxCollider olmalıdır.
/// 
/// Araba hangar içindeyken CarCrashManager.inSafeZone = true olur,
/// hangardan çıkınca otomatik olarak false olur.
/// 
/// Kurulum:
/// 1. Hangar objesine büyük bir BoxCollider ekleyin (tüm hangarı kapsasın)
/// 2. BoxCollider'ın "Is Trigger" kutusunu işaretleyin
/// 3. Bu scripti aynı objeye ekleyin
/// 4. Arabanın ROOT objesinde "Car" Tag'ı olmalı
/// 5. Arabanın Rigidbody'si olmalı (zaten var)
/// </summary>
public class HangarSafeZone : MonoBehaviour
{
    /// <summary>
    /// Trigger'a herhangi bir collider girdiğinde çağrılır.
    /// Arabanın root objesinde "Car" etiketi varsa güvenli bölgeyi AÇ.
    /// 
    /// NOT: other.CompareTag yerine other.transform.root.CompareTag kullanılır
    /// çünkü trigger'a giren collider arabanın bir alt parçası (tekerlek, kapı vb.)
    /// olabilir — etiket sadece root objede tanımlıdır.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Root objeyi kontrol et — alt parçalar da trigger'a girebilir
        if (!other.transform.root.CompareTag("Car")) return;

        // Arabadaki CarCrashManager'ı bul
        CarCrashManager crashManager = other.transform.root.GetComponentInChildren<CarCrashManager>();

        if (crashManager != null)
        {
            crashManager.inSafeZone = true;
            Debug.Log("[HangarSafeZone] Araba hangara girdi — güvenli bölge AKTİF.");
        }
    }

    /// <summary>
    /// Trigger'dan herhangi bir collider çıktığında çağrılır.
    /// Arabanın root objesinde "Car" etiketi varsa güvenli bölgeyi KAPAT.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Root objeyi kontrol et — alt parçalar da trigger'dan çıkabilir
        if (!other.transform.root.CompareTag("Car")) return;

        // Arabadaki CarCrashManager'ı bul
        CarCrashManager crashManager = other.transform.root.GetComponentInChildren<CarCrashManager>();

        if (crashManager != null)
        {
            crashManager.inSafeZone = false;
            Debug.Log("[HangarSafeZone] Araba hangardan çıktı — güvenli bölge KAPALI.");
        }
    }
}
