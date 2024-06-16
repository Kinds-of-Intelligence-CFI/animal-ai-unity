using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SizeChangeGoal class is a subclass of BallGoal that represents a goal that changes size.
/// </summary>
public class SizeChangeGoal : BallGoal
{
    [Header("Size Change Params")]
    public float initialSize = 5;
    public float finalSize = 1;
    public float sizeChangeRate;

    public enum InterpolationMethod
    {
        Constant,
        Linear
    }
    [Header("Interpolation Method")]
    public InterpolationMethod interpolationMethod;
    public bool rewardSizeTracking = true;
    public float rewardOverride = 0;
    public int fixedFrameDelay = 150;

    [Header("Size Constraints")]
    private bool isShrinking;
    private bool freeToGrow = true;
    private float sizeProportion = 0;
    private int delayCounter;
    private bool finishedSizeChange = false;
    private Collider[] sphereOverlap;

    private void Awake()
    {
        InitializeValues();
    }

    private void InitializeValues()
    {
        delayCounter = fixedFrameDelay;
        initialSize = Mathf.Clamp(initialSize, 0, sizeMax.x);
        finalSize = Mathf.Clamp(finalSize, 0, sizeMax.x);
        isShrinking = (finalSize <= initialSize);
        sizeMin = Mathf.Min(initialSize, finalSize) * Vector3.one;
        sizeMax = Mathf.Max(initialSize, finalSize) * Vector3.one;
        if (isShrinking == (sizeChangeRate >= 0))
        {
            sizeChangeRate *= -1;
        }
        SetSize(initialSize * Vector3.one);
    }

    public override void SetInitialValue(float v)
    {
        initialSize = v;
        if (delayCounter > 0)
            reward = initialSize;
    }

    public override void SetFinalValue(float v)
    {
        finalSize = v;
    }

    public override void SetChangeRate(float v)
    {
        sizeChangeRate = v;
        if (isShrinking == (sizeChangeRate >= 0))
        {
            sizeChangeRate *= -1;
        }
    }

    public override void SetDelay(float v)
    {
        fixedFrameDelay = Mathf.RoundToInt(v);
        delayCounter = fixedFrameDelay;
    }

    public override void SetSize(Vector3 size)
    {
        base.SetSize(
            (size.x < 0 || size.y < 0 || size.z < 0 || delayCounter > 0)
                ? initialSize * Vector3.one
                : size
        );
        if (!rewardSizeTracking)
        {
            reward = rewardOverride;
        }
    }

    private void FixedUpdate()
    {
        UpdateRayCollisions();
        if (delayCounter > 0)
        {
            delayCounter--;
        }
        else
        {
            if (delayCounter == 1)
            {
                Debug.Log("delayCounter will now hit zero. Starting size change!");
            }

            if (!finishedSizeChange && freeToGrow)
            {
                if ((int)interpolationMethod == 0) /*Constant*/
                {
                    SetSize((_height + sizeChangeRate) * Vector3.one);
                }
                else if ((int)interpolationMethod > 0) /*Polynomial*/
                {
                    PolyInterpolationUpdate();
                }

                if (isShrinking ? _height <= finalSize : _height >= finalSize)
                {
                    SetSize(finalSize * Vector3.one);
                    finishedSizeChange = true;
                }
            }
        }
    }

    private void UpdateRayCollisions()
    {
        if (!isShrinking)
        {
            freeToGrow = !Physics.Raycast(
                transform.position + new Vector3(0, transform.localScale.y / 2, 0),
                Vector3.up,
                Mathf.Abs(sizeChangeRate)
            );

            sphereOverlap = Physics.OverlapSphere(
                transform.position,
                transform.localScale.x / 2 - 0.05f
            );
            List<Collider> overlapList = new List<Collider>();
            foreach (Collider c in sphereOverlap)
            {
                if (c.gameObject.CompareTag("arena") || c.gameObject.CompareTag("Immovable"))
                {
                    overlapList.Add(c);
                }
            }
            if (overlapList.Count > 0)
            {
                freeToGrow = false;
            }
        }
    }

    private void PolyInterpolationUpdate()
    {
        sizeProportion += sizeChangeRate;
        SetSize((sizeProportion * finalSize + (1 - sizeProportion) * initialSize) * Vector3.one);
    }
}
