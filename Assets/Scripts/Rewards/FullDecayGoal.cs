using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// A goal that decays from an initial reward to a final reward over time.
/// </summary>
public class FullDecayGoal : BallGoal
{
    [Header("Full Decay Goal Settings")]
    public float initialReward;
    public float middleReward;
    public float finalReward;
    public bool useMiddle;

    [Header("Episode End Settings")]
    public float goodRewardEndThreshold;
    public float badRewardEndThreshold;
    public bool useGoodEpisodeEndThreshold;
    public bool useBadEpisodeEndThreshold;

    [ColorUsage(true, true)]
    public Color goodColour;

    [ColorUsage(true, true)]
    public Color neutralColour;

    [ColorUsage(true, true)]
    public Color badColour;

    [Header("Decay Settings")]
    public float decayRate = -0.001f;
    public bool flipDecayDirection = false;

    public Material _mat;
    public bool isDecaying = false;
    public float middleDecayProportion;
    private float decayWidth;

    void Awake()
    {
        _mat = this.gameObject.GetComponent<MeshRenderer>().material;
        _mat.EnableKeyword("_EMISSION");
        RandomizeColor();

        if (flipDecayDirection ? decayRate < 0 : decayRate > 0)
        {
            decayRate *= -1;
            Debug.Log("Had to flip decay rate");
        }

        canRandomizeColor = false;
        sizeMax = sizeMin = new Vector3(reward, reward, reward);

        if (useMiddle)
        {
            if (
                middleReward < Mathf.Min(initialReward, finalReward)
                || middleReward > Mathf.Max(initialReward, finalReward)
            )
            {
                Debug.Log("middleReward not in expected range. Clamping . . .");
                middleReward = Mathf.Clamp(
                    middleReward,
                    Mathf.Min(initialReward, finalReward),
                    Mathf.Max(initialReward, finalReward)
                );
            }
        }

        decayWidth = Mathf.Abs(initialReward - finalReward);

        if (useMiddle)
        {
            middleDecayProportion =
                (flipDecayDirection ? (middleReward - initialReward) : (middleReward - finalReward))
                / decayWidth;
        }

        reward = initialReward;
        SetTag();

        UpdateColour(1);
        StartDecay();
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

    void SetTag()
    {
        if (IsGoodGoal())
        {
            this.gameObject.tag = "goodGoal";
        }
        else if (IsBadGoal())
        {
            this.gameObject.tag = "badGoal";
        }
        else
        {
            this.gameObject.tag = "goodGoalMulti";
        }
    }

    bool IsGoodGoal()
    {
        return useGoodEpisodeEndThreshold && reward >= goodRewardEndThreshold;
    }

    bool IsBadGoal()
    {
        return useBadEpisodeEndThreshold && reward <= badRewardEndThreshold;
    }

    public void FixedUpdate()
    {
        if (IsGoodGoal() || IsBadGoal())
        {
            SetEpisodeEnds(true);
        }
        else
        {
            SetEpisodeEnds(false);
        }

        if (isDecaying)
        {
            UpdateGoal(decayRate);
            SetTag();
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

    private bool HasFinalDecayBeenReached()
    {
        return flipDecayDirection ? reward >= finalReward : reward <= finalReward;
    }

    private bool StillInInitDecayState()
    {
        return flipDecayDirection ? reward <= initialReward : reward >= initialReward;
    }

    void UpdateGoal(float rate = -0.001f)
    {
        UpdateValue(rate);
        UpdateColour(GetProportion(reward));
    }

    private void UpdateValue(float rate)
    {
        reward = Mathf.Clamp(
            reward + rate,
            Mathf.Min(initialReward, finalReward),
            Mathf.Max(initialReward, finalReward)
        );
    }

    public void UpdateColour(float p)
    {
        p = Mathf.Clamp(p, 0f, 1f);

        Color targetColor;

        if (useMiddle && p < middleDecayProportion)
        {
            p = p / middleDecayProportion;
            targetColor = Color.Lerp(badColour, neutralColour, p);
        }
        else if (useMiddle)
        {
            p = (p - middleDecayProportion) / (1 - middleDecayProportion);
            targetColor = Color.Lerp(neutralColour, goodColour, p);
        }
        else
        {
            targetColor = Color.Lerp(badColour, goodColour, p);
        }

        _mat.SetColor("_EmissionColor", targetColor);
    }

    public float GetProportion(float r)
    {
        return (r - Mathf.Min(initialReward, finalReward)) / decayWidth;
    }

    void RandomizeColor()
    {
        goodColour = new Color(Random.value, Random.value, Random.value);
        neutralColour = new Color(Random.value, Random.value, Random.value);
        badColour = new Color(Random.value, Random.value, Random.value);
    }
}
