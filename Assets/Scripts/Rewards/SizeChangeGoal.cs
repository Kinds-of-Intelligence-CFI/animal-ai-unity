using UnityEngine;

/// <summary>
/// SizeChangeGoal class is a subclass of BallGoal that represents a goal that changes size.
/// </summary>
public class SizeChangeGoal : BallGoal
{
    [Header("Size Change Params")]
    [SerializeField] private float initialSize = 5f;
    [SerializeField] private float finalSize = 1f;
    [SerializeField] private float sizeChangeRate;
    [SerializeField] private InterpolationMethod interpolationMethod;

    [Header("Reward Params")][SerializeField] private bool rewardSizeTracking = true;
    [SerializeField] public float rewardOverride = 0f;

    [Header("Grow / Shrink Timing Params")]
    [SerializeField] private int fixedFrameDelay = 150;

    private bool isShrinking;
    private bool freeToGrow = true;
    private float sizeProportion = 0f;
    private int delayCounter;
    private bool finishedSizeChange = false;

    private const float MinSize = 0f;
    private const float MaxSize = 8f;

    public enum InterpolationMethod { Constant, Linear }

    public override void SetInitialValue(float v)
    {
        initialSize = Mathf.Clamp(v, MinSize, MaxSize);
        if (delayCounter > 0) reward = initialSize;
    }

    public override void SetFinalValue(float v)
    {
        finalSize = Mathf.Clamp(v, MinSize, MaxSize);
    }

    public override void SetChangeRate(float v)
    {
        sizeChangeRate = v;
        CheckIfNeedToFlip();
    }

    public override void SetDelay(float v)
    {
        fixedFrameDelay = Mathf.RoundToInt(v);
        delayCounter = fixedFrameDelay;
    }

    public override void SetSize(Vector3 size)
    {
        base.SetSize((size.x < 0 || size.y < 0 || size.z < 0 || delayCounter > 0) ? initialSize * Vector3.one : size);
        if (!rewardSizeTracking) { reward = rewardOverride; }
    }

    private void Start()
    {
        isShrinking = (finalSize <= initialSize);
        delayCounter = fixedFrameDelay;

        sizeMin = Mathf.Min(initialSize, finalSize) * Vector3.one;
        sizeMax = Mathf.Max(initialSize, finalSize) * Vector3.one;

        CheckIfNeedToFlip();
        SetSize(initialSize * Vector3.one);
    }

    private void FixedUpdate()
    {
        if (delayCounter > 0)
        {
            delayCounter--;
            return;
        }

        if (!finishedSizeChange)
        {
            if (!isShrinking)
            {
                CheckGrowthFreedom();
            }

            if (freeToGrow)
            {
                UpdateSize();
            }
        }
    }

    private void CheckGrowthFreedom()
    {
        freeToGrow = !Physics.Raycast(transform.position + new Vector3(0, transform.localScale.y / 2, 0), Vector3.up, Mathf.Abs(sizeChangeRate));

        var sphereOverlap = Physics.OverlapSphere(transform.position, transform.localScale.x / 2 - 0.05f);
        foreach (var c in sphereOverlap)
        {
            if (c.CompareTag("arena") || c.CompareTag("Immovable"))
            {
                freeToGrow = false;
                break;
            }
        }
    }
    private void UpdateSize()
    {
        if (interpolationMethod == InterpolationMethod.Constant)
        {
            SetSize((_height + sizeChangeRate) * Vector3.one);
        }
        else
        {
            PolyInterpolationUpdate();
        }

        if (isShrinking ? _height <= finalSize : _height >= finalSize)
        {
            SetSize(finalSize * Vector3.one);
            finishedSizeChange = true;
        }
    }

    private void PolyInterpolationUpdate()
    {
        sizeProportion += sizeChangeRate;
        SetSize((sizeProportion * finalSize + (1 - sizeProportion) * initialSize) * Vector3.one);
    }

    private void CheckIfNeedToFlip()
    {
        isShrinking = (finalSize <= initialSize);
        if (isShrinking == (sizeChangeRate >= 0)) { sizeChangeRate *= -1; }
    }
}