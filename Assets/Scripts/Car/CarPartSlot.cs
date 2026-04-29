using UnityEngine;

public class CarPartSlot : MonoBehaviour, IInteractable
{
    [Header("Slot Ayarları")]
    [SerializeField] private CarPartType acceptedPartType;
    [SerializeField] private GameObject partVisual;


    [Header("Interaction Strings")]
    [SerializeField] private string installPromptText = "Install [F]";
    [SerializeField] private string removePromptText = "Remove [F]";

    [Header("Mekanik Bağlantılar")]
    [SerializeField] private WheelCollider linkedWheelCollider;

    [Header("Yeşil Önizleme")]
    [SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.35f);

    [Header("Başlangıç Ayarları")]
    [SerializeField] private bool isPreInstalled = false;

    private bool isInstalled;
    private PickupableCarPart installedPart;
    private Vector3 installedPartOriginalScale;
    private PlayerInteraction cachedPlayer;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material previewMaterial;
    private bool isPreviewing;
    private Collider detectionCollider;

    public CarPartType AcceptedPartType => acceptedPartType;
    public bool IsInstalled => isInstalled;

    public string InteractionPrompt => isInstalled ? removePromptText : installPromptText;
    public InteractionType Type => InteractionType.Pickup;

    public bool CanInteract
    {
        get
        {
            if (cachedPlayer == null)
                cachedPlayer = GameReferences.Instance?.PlayerInteraction;
            if (cachedPlayer == null) return false;
            return isInstalled
                ? !cachedPlayer.HasCarPart
                : cachedPlayer.HasCarPart && cachedPlayer.HeldPartType == acceptedPartType;
        }
    }

    public void Install(PickupableCarPart part, Vector3 originalScale)
    {
        isInstalled = true;
        installedPart = part;
        installedPartOriginalScale = originalScale;

        if (isPreviewing) HidePreview();

        if (partVisual != null)
        {
            partVisual.SetActive(true);
            RestoreOriginalMaterials();
            if (detectionCollider != null) detectionCollider.enabled = false;

            if (part != null)
            {
                GameObject obj = part.gameObject;
                AudioSource audio = obj.GetComponent<AudioSource>();

                if (audio != null && audio.isPlaying)
                {
                    // Keep audio alive: parent to slot, shrink to invisible
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = Vector3.one * 0.001f;

                    Renderer[] partRenderers = obj.GetComponentsInChildren<Renderer>();
                    foreach (var r in partRenderers) r.enabled = false;

                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

                    PhysicsUtils.SetCollidersEnabled(obj, false);
                }
                else
                {
                    obj.SetActive(false);
                }
            }
        }
        else if (part != null)
        {
            GameObject obj = part.gameObject;
            obj.SetActive(true);
            obj.transform.SetParent(transform);
            obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.transform.localScale = Vector3.one;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

            PhysicsUtils.SetCollidersEnabled(obj, false);
            if (detectionCollider != null) detectionCollider.enabled = false;
        }

        if (linkedWheelCollider != null) linkedWheelCollider.gameObject.SetActive(true);
        IgnoreCollisionsWithDoors();
        GetComponentInParent<CarAssemblyManager>()?.OnPartInstalled(acceptedPartType);
    }

    public PickupableCarPart Uninstall()
    {
        isInstalled = false;
        if (partVisual != null)
        {
            partVisual.SetActive(false);
            if (detectionCollider != null) detectionCollider.enabled = true;
        }

        Collider myCol = GetComponent<Collider>();
        if (myCol != null) myCol.enabled = true;

        if (linkedWheelCollider != null) linkedWheelCollider.gameObject.SetActive(false);

        PickupableCarPart part = installedPart;
        installedPart = null;

        if (part != null)
        {
            GameObject obj = part.gameObject;
            obj.transform.SetParent(null);
            obj.transform.localScale = installedPartOriginalScale;
            obj.SetActive(true);

            Renderer[] partRenderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in partRenderers) r.enabled = true;

            PhysicsUtils.SetCollidersEnabled(obj, true);
        }

        GetComponentInParent<CarAssemblyManager>()?.OnPartRemoved(acceptedPartType);
        return part;
    }

    public void SetLookedAt(bool isLooking, bool hasCorrectPart)
    {
        if (partVisual == null || isInstalled) return;

        bool shouldPreview = isLooking && hasCorrectPart;
        if (shouldPreview && !isPreviewing) ShowPreview();
        else if (!shouldPreview && isPreviewing) HidePreview();
    }

    private void Start()
    {
        CreatePreviewMaterial();

        if (partVisual != null)
        {
            renderers = GetOnlyMyRenderers();
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterials;

            if (GetComponent<Collider>() == null)
                CreateDetectionCollider();

            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                if (gameObject.layer != interactableLayer) gameObject.layer = interactableLayer;

                Transform[] allChildren = partVisual.GetComponentsInChildren<Transform>(true);
                foreach (Transform t in allChildren)
                {
                    if (t.gameObject.layer != interactableLayer)
                        t.gameObject.layer = interactableLayer;
                }
            }

            if (isPreInstalled)
            {
                partVisual.SetActive(true);
                isInstalled = true;
                if (detectionCollider != null) detectionCollider.enabled = false;
                IgnoreCollisionsWithDoors();
                GetComponentInParent<CarAssemblyManager>()?.OnPartInstalled(acceptedPartType);
                if (linkedWheelCollider != null) linkedWheelCollider.gameObject.SetActive(true);
            }
            else
            {
                partVisual.SetActive(false);
                if (linkedWheelCollider != null) linkedWheelCollider.gameObject.SetActive(false);
            }
        }
    }

    public bool HasInstalledChildSlots()
    {
        if (partVisual == null) return false;
        CarPartSlot[] childSlots = partVisual.GetComponentsInChildren<CarPartSlot>(true);
        foreach (var slot in childSlots)
        {
            if (slot != this && slot.IsInstalled) return true;
        }
        return false;
    }

    private void CreateDetectionCollider()
    {
        Renderer[] visualRenderers = GetOnlyMyRenderers();
        if (visualRenderers.Length == 0) return;

        Bounds localBounds = new Bounds(transform.InverseTransformPoint(visualRenderers[0].bounds.center), Vector3.zero);

        foreach (Renderer r in visualRenderers)
        {
            Bounds rBounds = r.bounds;
            Vector3 ext = rBounds.extents;
            Vector3 c = rBounds.center;

            Vector3[] corners = new Vector3[8];
            corners[0] = transform.InverseTransformPoint(c + new Vector3(ext.x, ext.y, ext.z));
            corners[1] = transform.InverseTransformPoint(c + new Vector3(ext.x, ext.y, -ext.z));
            corners[2] = transform.InverseTransformPoint(c + new Vector3(ext.x, -ext.y, ext.z));
            corners[3] = transform.InverseTransformPoint(c + new Vector3(ext.x, -ext.y, -ext.z));
            corners[4] = transform.InverseTransformPoint(c + new Vector3(-ext.x, ext.y, ext.z));
            corners[5] = transform.InverseTransformPoint(c + new Vector3(-ext.x, ext.y, -ext.z));
            corners[6] = transform.InverseTransformPoint(c + new Vector3(-ext.x, -ext.y, ext.z));
            corners[7] = transform.InverseTransformPoint(c + new Vector3(-ext.x, -ext.y, -ext.z));

            foreach (Vector3 corner in corners)
                localBounds.Encapsulate(corner);
        }

        BoxCollider detectionCol = gameObject.AddComponent<BoxCollider>();
        detectionCol.center = localBounds.center;
        detectionCol.size = localBounds.size;
        detectionCol.isTrigger = true;
        detectionCollider = detectionCol;
    }

    private void CreatePreviewMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit");

        if (shader != null)
        {
            previewMaterial = new Material(shader);
            previewMaterial.SetFloat("_Surface", 1);
            previewMaterial.SetFloat("_Blend", 0);
            previewMaterial.SetFloat("_AlphaClip", 0);
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            previewMaterial.SetColor("_BaseColor", previewColor);
            previewMaterial.color = previewColor;
        }
        else
        {
            previewMaterial = new Material(Shader.Find("Sprites/Default"));
            previewMaterial.color = previewColor;
            previewMaterial.renderQueue = 3000;
        }
    }

    private void ShowPreview()
    {
        partVisual.SetActive(true);
        if (renderers != null && previewMaterial != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = new Material[renderers[i].sharedMaterials.Length];
                for (int j = 0; j < mats.Length; j++) mats[j] = previewMaterial;
                renderers[i].materials = mats;
            }
        }
        isPreviewing = true;
    }

    private void HidePreview()
    {
        partVisual.SetActive(false);
        RestoreOriginalMaterials();
        isPreviewing = false;
    }

    private void RestoreOriginalMaterials()
    {
        if (renderers == null || originalMaterials == null) return;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].materials = originalMaterials[i];
    }

    private void OnDestroy()
    {
        if (previewMaterial != null) Destroy(previewMaterial);
    }

    private Renderer[] GetOnlyMyRenderers()
    {
        if (partVisual == null) return new Renderer[0];

        System.Collections.Generic.List<Renderer> myRenderers = new System.Collections.Generic.List<Renderer>();
        Renderer[] allRenderers = partVisual.GetComponentsInChildren<Renderer>(true);

        foreach (var r in allRenderers)
        {
            CarPartSlot closestSlot = r.GetComponentInParent<CarPartSlot>();
            if (closestSlot == this || closestSlot == null)
                myRenderers.Add(r);
        }

        return myRenderers.ToArray();
    }

    private void IgnoreCollisionsWithDoors()
    {
        if (partVisual == null) return;
        
        Collider[] myColliders = partVisual.GetComponentsInChildren<Collider>();
        if (myColliders.Length == 0) return;

        HingeDoor[] doors = transform.root.GetComponentsInChildren<HingeDoor>(true);
        foreach (var door in doors)
        {
            Collider[] doorCols = door.GetComponentsInChildren<Collider>();
            foreach (var dCol in doorCols)
            {
                foreach (var mCol in myColliders)
                {
                    if (dCol.enabled && mCol.enabled && dCol.gameObject.activeInHierarchy && mCol.gameObject.activeInHierarchy)
                    {
                        Physics.IgnoreCollision(dCol, mCol, true);
                    }
                }
            }
        }
    }
}
