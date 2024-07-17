using UnityEngine;

/// <summary>
/// A zone that triggers an event when an agent enters it.
/// It's primary use is to trigger a log event when an agent enters a datazone gameobject.
/// </summary>
public class DataZone : Prefab
{
    public delegate void DataZoneHandler(string TriggerZoneID);
    public static event DataZoneHandler OnInDataZone;
    public static event DataZoneHandler OnOutDataZone;
    public string TriggerZoneID { get; set; }
    public bool ZoneVisibility { get; set; } = true;
    public bool isAgentInZone { get; set; } = false;

    private void Start()
    {
        SetVisibility(ZoneVisibility);
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public void SetVisibility(bool visibility)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = visibility;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent") && !isAgentInZone)
        {
            isAgentInZone = true;
            OnInDataZone?.Invoke(TriggerZoneID);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            isAgentInZone = false;
            OnOutDataZone?.Invoke(TriggerZoneID);
        }
    }
}
