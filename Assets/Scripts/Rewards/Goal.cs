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
