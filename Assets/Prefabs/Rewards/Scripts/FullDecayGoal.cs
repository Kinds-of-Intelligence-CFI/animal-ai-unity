using UnityEngine;
using System;
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

    private Material _mat;
    private bool isDecaying = false;
    private float middleDecayProportion;
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

    void StartDecay(bool reset = false)
    {
        isDecaying = true;
        if (reset)
        {
            reward = initialReward;
        }
    }

    void StopDecay(bool reset = false)
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

    void FixedUpdate()
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

    private void UpdateColour(float p)
    {
        if (p != Mathf.Clamp(p, 0, 1))
        {
            Debug.Log("UpdateColour passed a bad proprtion! Clamping . . .");
            p = Mathf.Clamp(p, 0, 1);
        }

        if (useMiddle && p < middleDecayProportion)
        {
            p = (p / middleDecayProportion);
            _mat.SetColor(
                "_EmissionColor",
                p * neutralColour
                    + (1 - p) * badColour
                    + (0.5f - Mathf.Abs(p - 0.5f)) * Color.white * 0.1f
            );
        }
        else if (useMiddle)
        {
            p = ((p - middleDecayProportion) / (1 - middleDecayProportion));
            _mat.SetColor(
                "_EmissionColor",
                p * goodColour
                    + (1 - p) * neutralColour
                    + (0.5f - Mathf.Abs(p - 0.5f)) * Color.white * 0.1f
            );
        }
        else
        {
            _mat.SetColor(
                "_EmissionColor",
                p * goodColour
                    + (1 - p) * badColour
                    + (0.5f - Mathf.Abs(p - 0.5f)) * Color.white * 0.1f
            );
        }
    }

    float GetProportion(float r)
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
