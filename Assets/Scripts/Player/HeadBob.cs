using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("Yürüme Bob Ayarları")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmount = 0.03f;

    [Header("Koşma Bob Ayarları")]
    [SerializeField] private float runBobSpeed = 14f;
    [SerializeField] private float runBobAmount = 0.05f;

    [Header("Eğilme Bob Ayarları")]
    [SerializeField] private float crouchBobSpeed = 6f;
    [SerializeField] private float crouchBobAmount = 0.015f;

    [Header("Genel Ayarlar")]
    [SerializeField] private float smoothTransition = 10f;
    [SerializeField] private float crouchCameraSmooth = 8f;
    [SerializeField] private PlayerController playerController;

    private float bobTimer;
    private float defaultYPosition;

    private void Awake()
    {
        defaultYPosition = transform.localPosition.y;

        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController == null)
                Debug.LogError("[HeadBob] PlayerController bulunamadı!");
        }
    }

    private void Update()
    {
        if (playerController == null) return;

        float crouchOffset = playerController.CrouchRatio * -1.0f;
        float baseY = defaultYPosition + crouchOffset;

        if (playerController.IsGrounded && playerController.IsMoving)
        {
            float bobSpeed, bobAmount;

            if (playerController.IsCrouching)
            {
                bobSpeed = crouchBobSpeed;
                bobAmount = crouchBobAmount;
            }
            else if (playerController.IsSprinting)
            {
                bobSpeed = runBobSpeed;
                bobAmount = runBobAmount;
            }
            else
            {
                bobSpeed = walkBobSpeed;
                bobAmount = walkBobAmount;
            }

            bobTimer += Time.deltaTime * bobSpeed;
            float targetY = baseY + Mathf.Sin(bobTimer) * bobAmount;

            Vector3 localPos = transform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, targetY, smoothTransition * Time.deltaTime);
            transform.localPosition = localPos;
        }
        else
        {
            bobTimer = 0f;

            Vector3 localPos = transform.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, baseY, crouchCameraSmooth * Time.deltaTime);
            transform.localPosition = localPos;
        }
    }
}
