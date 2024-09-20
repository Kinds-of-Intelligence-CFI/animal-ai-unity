using UnityEngine;

/// <summary>
/// Goal class is a base class for all the goals in the environment.
/// </summary>
public class Goal : Prefab
{
    [Header("Goal Settings")]
    public int numberOfGoals = 1;
    public float reward = 1;
    public bool isMulti = false;
    public string rewardType = "None"; /* Reward types as strings */

    void Awake()
    {
        canRandomizeColor = false;
    }

    protected override void ApplyColorToRenderer(Renderer renderer, Color color, bool colorSpecified)
    {
        if (!colorSpecified)
        {
            return;
        }

        /* Create a new material to apply color changes. This is unique for each instance */
        Material originalMaterial = renderer.material;
        renderer.material = new Material(originalMaterial);

        if (renderer.material.HasProperty("_BaseColor"))
        {
            renderer.material.SetColor("_BaseColor", color);
        }
        else
        {
            renderer.material.color = color;
        }

        if (colorSpecified || canRandomizeColor)
        {
            /* Apply emissive color if available */
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                renderer.material.SetColor("_EmissionColor", color);
                renderer.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            /* Copy over the emissive color from the original material */
            if (renderer.material.HasProperty("_EmissionColor"))
            {
                Color originalEmissionColor = originalMaterial.GetColor("_EmissionColor");
                renderer.material.SetColor("_EmissionColor", originalEmissionColor);
                if (originalMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    renderer.material.EnableKeyword("_EMISSION");
                }
                else
                {
                    renderer.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("agent"))
        {
            TrainingAgent agent = other.GetComponent<TrainingAgent>();
            if (agent != null)
            {
                agent.RecordRewardType(rewardType);
                agent.UpdateHealth(reward, true);
            }
        }
    }

    public virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("agent"))
        {
            TrainingAgent agent = collision.gameObject.GetComponent<TrainingAgent>();
            if (agent != null)
            {
                agent.RecordRewardType(rewardType);
                if (!isMulti)
                {
                    agent.UpdateHealth(reward, true);
                }
                else
                {
                    agent.numberOfGoalsCollected++;
                    if (agent.numberOfGoalsCollected >= numberOfGoals)
                    {
                        agent.UpdateHealth(reward, true);
                    }
                    else
                    {
                        agent.UpdateHealth(reward);
                    }
                    gameObject.SetActive(false);
                    Object.Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogError("Agent not found in the collision object.");
            }
        }
    }
}
