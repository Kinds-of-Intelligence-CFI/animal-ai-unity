using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;
using PrefabInterface;

/// <summary>
/// Spawns a reward when interacted with. The reward is chosen from a list of rewards with weights and spawn probabilities.
/// The reward can be spawned at a fixed position or randomly within the arena bounds.
/// 
/// TODO: Refactor the code to use the ArenaBuilder class to get the arena bounds.
/// TODO: Set default values for RewardSpawnPos.
/// </summary>
public class SpawnerButton : MonoBehaviour
{
    public List<string> RewardNames { get; set; }
    public List<float> RewardWeights { get; set; }
    public List<GameObject> Rewards { get; set; }
    public Vector3 RewardSpawnPos { get; set; }
    public List<int> MaxRewardCounts { get; set; }
    public Vector3 SpawnedRewardSize { get; set; }
    public int ButtonPressCount { get; private set; }
    public GameObject LastSpawnedReward { get; private set; }
    public float SpawnProbability { get; set; } = 1f;
    private List<float> rewardWeights;
    public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } =
        new Dictionary<GameObject, int>();
    private bool IsMoving = false;
    private float lastInteractionTime;
    private float totalInteractionInterval = 0f;
    private float _moveDuration;
    private float _resetDuration;

    private ArenaBuilder arenaBuilder;
    public delegate void OnRewardSpawned(GameObject reward);
    public static event OnRewardSpawned RewardSpawned;

    [SerializeField]
    private GameObject childObjectToMove;

    [SerializeField]
    private Vector3 moveOffset;

    [SerializeField]
    private Transform rewardSpawnPoint;

    private static int spawnerCounter = 0;
    public int spawnerID;
    private Vector3 initialPosition;

    public float MoveDuration
    {
        get { return _moveDuration; }
        set { _moveDuration = value; }
    }
    public float ResetDuration
    {
        get { return _resetDuration; }
        set { _resetDuration = value; }
    }

    void Start()
    {
        spawnerID = ++spawnerCounter;
        lastInteractionTime = Time.time;

        if (RewardNames != null && RewardNames.Count > 0)
        {
            Rewards = RewardNames
                .Select(name =>
                {
                    GameObject reward = Resources.Load<GameObject>(name);
                    if (reward == null)
                    {
                        Debug.LogError($"Failed to load reward: {name}");
                    }
                    return reward;
                })
                .ToList();
        }
        if (SpawnedRewardSize != Vector3.zero)
        {
            foreach (GameObject reward in Rewards)
            {
                reward.GetComponent<IPrefab>().SetSize(SpawnedRewardSize);
            }
        }

        rewardWeights = RewardWeights;
        initialPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            if (IsMoving)
            {
                return;
            }

            ButtonPressCount++;
            StartCoroutine(MoveAndReset());
        }
    }

    public bool MoveToTarget(Vector3 origin, Vector3 target, float startTime, float duration)
    {
        float t = (Time.time - startTime) / duration;
        childObjectToMove.transform.position = Vector3.Lerp(origin, target, t);
        return Time.time < startTime + duration;
    }

    public IEnumerator MoveAndReset()
    {
        IsMoving = true;

        Vector3 originalPosition = childObjectToMove.transform.position;
        Vector3 movementDirection = -transform.forward * moveOffset.x; /* Use parent object's negative local Z axis */
        Vector3 targetPosition = originalPosition + movementDirection;
        float startTime = Time.time;

        while (MoveToTarget(originalPosition, targetPosition, startTime, MoveDuration))
        {
            yield return null;
        }
        childObjectToMove.transform.position = targetPosition;

        startTime = Time.time;
        while (MoveToTarget(targetPosition, originalPosition, startTime, ResetDuration))
        {
            yield return null;
        }
        childObjectToMove.transform.position = originalPosition;

        SpawnReward();

        TrainingAgent agent = FindObjectOfType<TrainingAgent>();
        if (agent != null)
        {
            Vector3 spawnerPos = initialPosition;
            string rewardType =
                LastSpawnedReward != null ? LastSpawnedReward.name.Replace("(Clone)", "") : "None";
            string spawnerInfo =
                $"SpawnerButtonID:{spawnerID}, Position:{spawnerPos.x},{spawnerPos.y},{spawnerPos.z}, RewardType:{rewardType}";
            Debug.Log($"Logging SpawnerButton Info: {spawnerInfo}");
            agent.RecordSpawnerInfo(spawnerInfo);
        }
        else
        {
            Debug.LogError("Training Agent not found in the scene.");
        }

        IsMoving = false;
    }

    private GameObject ChooseReward()
    {
        if (Rewards == null || rewardWeights == null || Rewards.Count != rewardWeights.Count)
        {
            Debug.LogError("Invalid rewards or reward weights setup.");
            return null;
        }

        float totalWeight = rewardWeights.Sum();
        float randomNumber = Random.Range(0, totalWeight - float.Epsilon);
        float cumulativeWeight = 0;

        for (int i = 0; i < Rewards.Count; i++)
        {
            cumulativeWeight += rewardWeights[i];
            if (randomNumber <= cumulativeWeight)
            {
                return Rewards[i];
            }
        }

        /* If no reward is selected within the loop (which should not happen), return the last reward */
        return Rewards[Rewards.Count - 1];
    }

    private void SpawnReward()
    {
        GameObject rewardToSpawn = ChooseReward();

        if (rewardToSpawn == null)
        {
            Debug.LogError("Failed to choose a reward to spawn.");
            return;
        }

        int rewardIndex = Rewards.IndexOf(rewardToSpawn);
        if (rewardIndex == -1)
        {
            Debug.LogError("Chosen reward is not in the Rewards list.");
            return;
        }

        if (rewardIndex < MaxRewardCounts.Count)
        {
            Debug.Log(
                "Max allowed spawns for " + rewardToSpawn.name + ": " + MaxRewardCounts[rewardIndex]
            );
        }
        else
        {
            Debug.Log("No max spawn count set for " + rewardToSpawn.name);
        }

        if (
            MaxRewardCounts != null
            && rewardIndex < MaxRewardCounts.Count
            && MaxRewardCounts[rewardIndex] != -1
        )
        {
            if (
                RewardSpawnCounts.TryGetValue(rewardToSpawn, out var count)
                && count >= MaxRewardCounts[rewardIndex]
            )
            {
                Debug.Log("Max reward count reached for reward: " + rewardToSpawn.name);
                return;
            }
        }

        if (Rewards == null || Rewards.Count == 0)
        {
            Debug.LogError("No rewards are set to be spawned.");
            return;
        }

        if (Random.value <= SpawnProbability)
        {
            Vector3 spawnPosition = rewardSpawnPoint.position;

            if (RewardSpawnPos != Vector3.zero)
            {
                spawnPosition = RewardSpawnPos;
            }
            else /* Randomize spawn position within the arena bounds */
            {
                float arenaWidth = arenaBuilder.ArenaWidth;
                float arenaDepth = arenaBuilder.ArenaDepth;

                /* Randomly generate a spawn position within the bounds of the arena, as defined by Arenabuilders.cs. */
                spawnPosition = new Vector3(
                    Random.Range(0, arenaWidth),
                    0,
                    Random.Range(0, arenaDepth)
                );
            }

            if (RewardSpawnPos.x == -1)
            {
                spawnPosition.x = Random.Range(0, arenaBuilder.ArenaWidth);
            }

            if (RewardSpawnPos.y == -1)
            {
                spawnPosition.y = Random.Range(0, 50);
            }
            else
            {
                spawnPosition.y = RewardSpawnPos.y;
            }

            if (RewardSpawnPos.z == -1)
            {
                spawnPosition.z = Random.Range(0, arenaBuilder.ArenaDepth);
            }

            LastSpawnedReward = Instantiate(rewardToSpawn, spawnPosition, Quaternion.identity);

            if (RewardSpawnCounts.TryGetValue(rewardToSpawn, out var count))
            {
                RewardSpawnCounts[rewardToSpawn] = count + 1;
            }
            else
            {
                RewardSpawnCounts[rewardToSpawn] = 1;
            }

            float currentInteractionTime = Time.time;
            totalInteractionInterval += currentInteractionTime - lastInteractionTime;
            lastInteractionTime = currentInteractionTime;

            RewardSpawned?.Invoke(LastSpawnedReward);
        }
    }

    public float GetAverageInteractionInterval()
    {
        if (ButtonPressCount == 0)
            return 0;

        return totalInteractionInterval / ButtonPressCount;
    }
}
