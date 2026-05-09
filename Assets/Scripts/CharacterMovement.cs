using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 0.15f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Crosshair")]
    public float crosshairSize = 20f;
    public float crosshairThickness = 3f;
    public Color crosshairColor = Color.white;

    [Header("Animation")]
    public Animator animator;

    private static readonly int ExcitementParam = Animator.StringToHash("Excitement");

    private CharacterController controller;
    private Vector3 velocity;
    private float pitchAngle;
    private bool isPlayingEmote;
    private float emoteTimer;
    private float emoteDuration;

    private Texture2D crosshairDot;
    private Texture2D crosshairRing;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }

        if (animator == null)
            animator = GetComponent<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        BuildCrosshairTextures();
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleAnimationInput();
    }

    void HandleAnimationInput()
    {
        if (isPlayingEmote)
        {
            emoteTimer += Time.deltaTime;

            if (emoteTimer >= emoteDuration)
            {
                isPlayingEmote = false;
                animator.SetFloat(ExcitementParam, 0f);

                AnimatorStateInfo idleState = animator.GetCurrentAnimatorStateInfo(0);
                animator.Play(idleState.fullPathHash, 0, 0f);
            }

            return;
        }

        Keyboard kb = Keyboard.current;
        int requested = -1;

        if (kb.digit1Key.wasPressedThisFrame) requested = 1;
        else if (kb.digit2Key.wasPressedThisFrame) requested = 2;
        else if (kb.digit3Key.wasPressedThisFrame) requested = 3;

        if (requested < 0) return;

        animator.SetFloat(ExcitementParam, requested);

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        animator.Play(state.fullPathHash, 0, 0f);
        animator.Update(0f);

        emoteDuration = GetLongestClipLength();
        emoteTimer = 0f;
        isPlayingEmote = true;
    }

    float GetLongestClipLength()
    {
        float longest = 0f;
        foreach (AnimatorClipInfo clip in animator.GetCurrentAnimatorClipInfo(0))
        {
            if (clip.clip.length > longest)
                longest = clip.clip.length;
        }
        return longest > 0f ? longest : 2f;
    }

    void HandleLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        transform.Rotate(Vector3.up * mouseDelta.x * mouseSensitivity);

        pitchAngle += mouseDelta.y * mouseSensitivity;
        pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(-pitchAngle, 0f, 0f);
    }

    void HandleMovement()
    {
        Keyboard kb = Keyboard.current;

        float h = 0f, v = 0f;

        if (kb.aKey.isPressed) h = -1f;
        if (kb.dKey.isPressed) h = 1f;
        if (kb.wKey.isPressed) v = 1f;
        if (kb.sKey.isPressed) v = -1f;

        Vector3 move = transform.right * h + transform.forward * v;

        if (controller.isGrounded)
        {
            velocity.y = -2f;
            if (kb.spaceKey.wasPressedThisFrame)
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move((move * moveSpeed + new Vector3(0, velocity.y, 0)) * Time.deltaTime);
    }

    void BuildCrosshairTextures()
    {
        int size = 64;
        crosshairRing = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerR = size / 2f - 1f;
        float innerR = outerR - crosshairThickness * 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = (dist <= outerR && dist >= innerR) ? crosshairColor : Color.clear;
            }

        crosshairRing.SetPixels(pixels);
        crosshairRing.Apply();

        int dotSize = 8;
        crosshairDot = new Texture2D(dotSize, dotSize);
        Color[] dotPixels = new Color[dotSize * dotSize];
        Vector2 dotCenter = new Vector2(dotSize / 2f, dotSize / 2f);

        for (int y = 0; y < dotSize; y++)
            for (int x = 0; x < dotSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), dotCenter);
                dotPixels[y * dotSize + x] = dist <= dotSize / 2f ? crosshairColor : Color.clear;
            }

        crosshairDot.SetPixels(dotPixels);
        crosshairDot.Apply();
    }

    void OnGUI()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float ringSize = crosshairSize * 2f;

        GUI.DrawTexture(new Rect(cx - ringSize / 2f, cy - ringSize / 2f, ringSize, ringSize), crosshairRing);

        float dotSize = crosshairSize * 0.25f;
        GUI.DrawTexture(new Rect(cx - dotSize / 2f, cy - dotSize / 2f, dotSize, dotSize), crosshairDot);
    }

    public Vector3 GetCrosshairWorldPoint(float maxDistance = 100f)
    {
        if (cameraTransform == null)
            return transform.position + transform.forward * maxDistance;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return hit.point;

        return ray.GetPoint(maxDistance);
    }
}