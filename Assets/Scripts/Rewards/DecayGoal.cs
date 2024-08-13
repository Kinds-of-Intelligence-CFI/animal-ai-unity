using UnityEngine;

/// <summary>
/// A BallGoal that decays the reward value of the ball over time.
/// </summary>
public class DecayGoal : BallGoal
{
    [Header("Reward Params")]
    public float initialReward;
    public float finalReward;

    [Header("Decay Params")]
    public float decayRate = -0.001f;

    [Header("Colour Params")]
    [ColorUsage(true, true)]
    public Color initialColour;

    [ColorUsage(true, true)]
    public Color finalColour;

    public bool flipDecayDirection = false;
    public int fixedFrameDelay = 150; /* Controls how long the goal waits before starting to decay */

    private Material _basemat;
    private Material _radialmat;
    public bool isDecaying = false;
    private float decayWidth;
    private float loAlpha = 0.11f;
    private float hiAlpha = 0.35f;
    public int delayCounter;

    public override void SetChangeRate(float v)
    {
        decayRate = v;
        CheckIfNeedToFlip();
    }

    public override void SetInitialValue(float v)
    {
        initialReward = v;
        if (StillInInitDecayState())
            reward = initialReward;
    }

    public override void SetFinalValue(float v)
    {
        finalReward = v;
    }

    public override void SetDelay(float v)
    {
        fixedFrameDelay = Mathf.RoundToInt(v);
        delayCounter = fixedFrameDelay; /* Reset delay counter to new value */
    }

    public void CheckIfNeedToFlip()
    {
        if (flipDecayDirection ? decayRate < 0 : decayRate > 0)
        {
            decayRate *= -1;
            Debug.Log("Had to flip decay rate");
        }
    }

    void Awake()
    {
        _basemat = GetComponent<MeshRenderer>().material;
        _basemat.EnableKeyword("_EMISSION");
        _basemat.SetColor("_EmissionColor", flipDecayDirection ? finalColour : initialColour);

        _radialmat = GetComponent<MeshRenderer>().materials[2];
        _radialmat.SetFloat("_Cutoff", isDecaying ? loAlpha : hiAlpha);

        canRandomizeColor = false;
        SetEpisodeEnds(false);
        sizeMax = 5 * Vector3Int.one;
        sizeMin = Vector3Int.zero;
        isDecaying = false;
    }

    void Start()
    {
        initialReward = Mathf.Clamp(initialReward, 0, sizeMax.x);
        finalReward = Mathf.Clamp(finalReward, 0, sizeMax.x);

        SetSize((flipDecayDirection ? finalReward : initialReward) * Vector3.one);

        CheckIfNeedToFlip();

        delayCounter = fixedFrameDelay;
        tag = "goodGoalMulti";
        reward = initialReward;

        if (
            (flipDecayDirection && finalReward < initialReward)
            || (!flipDecayDirection && finalReward > initialReward)
        )
        {
            finalReward = initialReward;
        }

        decayWidth = Mathf.Abs(initialReward - finalReward);

        UpdateColour(flipDecayDirection ? 0 : 1);
    }

    public override void SetSize(Vector3 size)
    {
        base.SetSize((flipDecayDirection ? finalReward : initialReward) * Vector3.one);
    }

    void FixedUpdate()
    {
        if (StillInInitDecayState() && !isDecaying)
        {
            if (delayCounter > 0)
            {
                delayCounter--;
            }
            else
            {
                StartDecay();
                UpdateGoal(0);
            }
        }

        if (isDecaying)
        {
            UpdateGoal(decayRate);
        }

        if (HasFinalDecayBeenReached() && isDecaying)
        {
            StopDecay();
            UpdateGoal(0);
        }
    }

    void SetEpisodeEnds(bool shouldEpisodeEnd)
    {
        isMulti = !shouldEpisodeEnd;
    }

    public bool HasFinalDecayBeenReached()
    {
        return flipDecayDirection ? reward >= finalReward : reward <= finalReward;
    }

    private bool StillInInitDecayState()
    {
        return flipDecayDirection ? reward <= initialReward : reward >= initialReward;
    }

    public void UpdateGoal(float rate = -0.001f)
    {
        if (!isDecaying)
            return;

        UpdateValue(rate);
        UpdateColour(GetProportion(reward));
    }

    public void UpdateValue(float rate)
    {
        reward = Mathf.Clamp(
            reward + rate,
            Mathf.Min(initialReward, finalReward),
            Mathf.Max(initialReward, finalReward)
        );
    }

    public void UpdateColour(float p)
    {
        p = Mathf.Clamp(p, 0, 1);

        _basemat.SetColor(
            "_EmissionColor",
            p * initialColour
                + (1 - p) * finalColour
                + (0.5f - Mathf.Abs(p - 0.5f)) * Color.white * 0.1f
        );
        _radialmat.SetFloat(
            "_Cutoff",
            Mathf.Clamp(p * loAlpha + (1 - p) * hiAlpha, loAlpha, hiAlpha)
        );
    }

    float GetProportion(float r)
    {
        return decayWidth != 0
            ? ((r - Mathf.Min(initialReward, finalReward)) / decayWidth)
            : (flipDecayDirection ? 1 : 0);
    }

    public void StartDecay(bool reset = false)
    {
        isDecaying = true;
        if (reset)
        {
            reward = initialReward;
        }
    }

    public void StopDecay(bool reset = false)
    {
        isDecaying = false;
        if (reset)
        {
            reward = finalReward;
        }
    }
}
