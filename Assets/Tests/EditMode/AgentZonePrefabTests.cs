using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The contract of the AgentZone prefab in Resources, which AgentZone.Create instantiates for every agent that
/// carries a zone: the code relies on each of these settings rather than applying them itself.
/// </summary>
public class AgentZonePrefabTests
{
    private GameObject _prefab;

    [SetUp]
    public void Setup()
    {
        _prefab = Resources.Load<GameObject>(AgentZone.PrefabResourcePath);
        Assert.IsNotNull(_prefab, $"The agent zone prefab should be at Resources/{AgentZone.PrefabResourcePath}");
    }

    [Test]
    public void CarriesTheZoneScripts()
    {
        Assert.IsNotNull(_prefab.GetComponent<AgentZone>());
        Assert.IsNotNull(_prefab.GetComponent<TriggerVolume>());
    }

    [Test]
    public void IsAUnitTriggerSphere_SoItsScaleIsTheZonesDiameter()
    {
        SphereCollider collider = _prefab.GetComponent<SphereCollider>();
        Assert.IsNotNull(collider);
        Assert.IsTrue(collider.isTrigger, "Agents must pass through the zone");
        Assert.AreEqual(0.5f, collider.radius, 1e-6f);
        Assert.AreEqual(Vector3.zero, collider.center);
        Assert.AreEqual(Vector3.one, _prefab.transform.localScale);
        Assert.IsNull(_prefab.GetComponent<Rigidbody>(), "The zone is part of the agent's rigidbody, not a body of its own");
    }

    [Test]
    public void IsIgnoredByRaySensorsAndLineCasts()
    {
        Assert.AreEqual(LayerMask.NameToLayer("Ignore Raycast"), _prefab.layer);
        Assert.IsFalse(_prefab.CompareTag("agent"), "The zone must not pass for an agent");
    }

    [Test]
    public void IsInvisibleByDefault_WithATranslucentMaterialForWhenItIsShown()
    {
        MeshRenderer renderer = _prefab.GetComponent<MeshRenderer>();
        Assert.IsNotNull(renderer);
        Assert.IsFalse(renderer.enabled, "Zones are drawn only when zoneVisibility is set");
        Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, renderer.shadowCastingMode);
        Assert.AreEqual(1f, renderer.sharedMaterial.GetFloat("_Surface"), "The material is transparent, so the agent inside stays visible");
    }
}
