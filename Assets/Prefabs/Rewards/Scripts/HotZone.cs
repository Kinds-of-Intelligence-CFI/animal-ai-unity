// using System.Collections;
// using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotZone : Goal
{
    private GameObject hotZoneFogOverlayObject;
    private Image hotZoneFog;
    public PlayerControls playerControls;
    private bool insideHotZone;

    private void Awake()
    {
        hotZoneFogOverlayObject = GameObject.FindGameObjectWithTag("EffectCanvas").transform.Find("HotZoneFog").gameObject;
        hotZoneFog = hotZoneFogOverlayObject.GetComponent<Image>();
        hotZoneFog.enabled = false;
        playerControls = GameObject.FindGameObjectWithTag("PlayerControls").GetComponent<PlayerControls>();
    }

    public override void SetSize(Vector3 size)//This switches back to grandparent method @TODO change prefab.cs to allow direct access and have other children of Prefab.cs override separate method.
    {
        Vector3 clippedSize = Vector3.Max(sizeMin, Vector3.Min(sizeMax, size)) * sizeAdjustment;
        float sizeX = size.x < 0 ? Random.Range(sizeMin[0], sizeMax[0]) : clippedSize.x;
        float sizeY = size.y < 0 ? Random.Range(sizeMin[1], sizeMax[1]) : clippedSize.y;
        float sizeZ = size.z < 0 ? Random.Range(sizeMin[2], sizeMax[2]) : clippedSize.z;

        _height = sizeY;
        transform.localScale = new Vector3(sizeX * ratioSize.x,
                                            sizeY * ratioSize.y,
                                            sizeZ * ratioSize.z);

        GetComponent<Renderer>().material.SetVector("_ObjScale", new Vector3(sizeX, sizeY, sizeZ));
    }

    protected override float AdjustY(float yIn)
    {
        return -0.15f;
    }

    public override void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("agent"))
        {
            collision.GetComponent<TrainingAgent>().AddExtraReward(reward);
        }
    }

    public void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.CompareTag("agent"))
        {
            collision.GetComponent<TrainingAgent>().AddExtraReward(reward);
        }
    }

    public void OnTriggerExit(Collider collision)
    {
        // Used to contain hotZone fog stuff, but replaced with better alternative actually relevant to the active camera
        // Leaving void here because could be useful in future: if (collision.gameObject.GetComponentInChildren<Camera>() == playerControls.getActiveCam())
    }

    private void FixedUpdate()
    {
        Vector3 p = playerControls.getActiveCam().gameObject.transform.position;
        Vector3 offset = this.GetComponent<BoxCollider>().bounds.center - p;
        Ray inputRay = new Ray(p, offset.normalized);
        RaycastHit rHit;

        insideHotZone = !this.GetComponent<BoxCollider>().Raycast(inputRay, out rHit, offset.magnitude * 1.1f);
        hotZoneFog.enabled = insideHotZone;
        this.GetComponent<Renderer>().material.SetFloat("_Cull", playerControls.cameraID==0 ? 2f : 0f);
    }
}
