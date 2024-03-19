using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HotZone class is a subclass of Goal that represents a hot zone in the environment.
/// </summary>
public class HotZone : Goal
{
    [Header("Hot Zone Settings")]
    private GameObject hotZoneFogOverlayObject;
    private Image hotZoneFog;
    private PlayerControls playerControls;
    private bool insideHotZone;

    private void Awake()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        hotZoneFogOverlayObject = GameObject
            .FindGameObjectWithTag("EffectCanvas")
            .transform.Find("HotZoneFog")
            .gameObject;
        hotZoneFog = hotZoneFogOverlayObject.GetComponent<Image>();
        hotZoneFog.enabled = false;
        playerControls = GameObject
            .FindGameObjectWithTag("PlayerControls")
            .GetComponent<PlayerControls>();
    }

    public override void SetSize(Vector3 size)
    {
        base.SetSize(size);
        AdjustHeight();
        AdjustMaterialScale();
    }

    private void AdjustHeight()
    {
        _height = Mathf.Max(sizeMin.y, Mathf.Min(sizeMax.y, transform.localScale.y));
    }

    private void AdjustMaterialScale()
    {
        Vector3 scale = transform.localScale;
        GetComponent<Renderer>().material.SetVector("_ObjScale", scale);
    }

    protected override float AdjustY(float yIn)
    {
        return -0.15f;
    }

    public override void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            other.GetComponent<TrainingAgent>().AddExtraReward(reward);
        }
    }

    public void OnTriggerStay(Collider other)
    {
        OnTriggerEnter(other);
    }

    public void FixedUpdate()
    {
        UpdateHotZoneVisibility();
        UpdateRendererCulling();
    }

    private void UpdateHotZoneVisibility()
    {
        Vector3 playerPosition = playerControls.getActiveCam().gameObject.transform.position;
        Vector3 offset = GetComponent<BoxCollider>().bounds.center - playerPosition;
        Ray inputRay = new Ray(playerPosition, offset.normalized);

        insideHotZone = !GetComponent<BoxCollider>()
            .Raycast(inputRay, out _, offset.magnitude * 1.1f);
        hotZoneFog.enabled = insideHotZone;
    }

    private void UpdateRendererCulling()
    {
        float cullValue = playerControls.cameraID == 0 ? 2f : 0f;
        GetComponent<Renderer>().material.SetFloat("_Cull", cullValue);
    }
}
