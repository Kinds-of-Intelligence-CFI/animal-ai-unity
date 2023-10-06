using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;

public class Spawner_InteractiveButton : MonoBehaviour
{
    private bool IsMoving = false;
    private float lastInteractionTime;
    private float totalInteractionInterval = 0f;
    public int ButtonPressCount { get; private set; }
    public GameObject LastSpawnedReward { get; private set; }
    public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } =
        new Dictionary<GameObject, int>();

    [SerializeField]
    private GameObject childObjectToMove;

    [SerializeField]
    private Vector3 moveOffset;

    [SerializeField]
    private Transform rewardSpawnPoint;

    [SerializeField]
    private GameObject objectToControl;

    [SerializeField]
    private bool randomizeColor = false;

    [SerializeField]
    private bool showObject;

    [SerializeField]
    private ArenaBuilder arenaBuilder;
    private Transform objectToControlSpawnPoint;
    private List<GameObject> rewards;
    private List<float> rewardWeights;
    public delegate void OnRewardSpawned(GameObject reward);
    public static event OnRewardSpawned RewardSpawned;
    private float _moveDuration;
    private float _resetDuration;
    public float SpawnProbability { get; set; } = 1f;
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
    public List<string> RewardNames { get; set; }
    public List<float> RewardWeights { get; set; }
    public List<GameObject> Rewards { get; set; }
    public Vector3 RewardSpawnPos { get; set; }
    public List<int> MaxRewardCounts { get; set; }

    void Start()
    {
        lastInteractionTime = Time.time;
        UpdateObjectVisibility(); // Set the object to be visible or not based on the showObject flag

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

        rewardWeights = RewardWeights;

        if (randomizeColor)
        {
            RandomizeColor();
        }
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

    private void UpdateObjectVisibility()
    {
        if (objectToControl != null)
        {
            objectToControl.SetActive(showObject);
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
        Vector3 movementDirection = -transform.forward * moveOffset.x; // Use parent's negative local Z axis
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

        // If no reward is selected within the loop (which should not happen), return the last reward
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
            // Otherwise, random spawning.
            else
            {
                float arenaWidth = arenaBuilder.GetArenaWidth();
                float arenaDepth = arenaBuilder.GetArenaDepth();

                // Randomly generate a spawn position within the bounds of the arena, as defined by Arenabuilders.cs.
                spawnPosition = new Vector3(
                    Random.Range(0, arenaWidth),
                    0, // Assuming spawning on the ground.
                    Random.Range(0, arenaDepth)
                );
            }
            // Check for randomization flags for x and z axes
            if (RewardSpawnPos.x == -1)
            {
                spawnPosition.x = Random.Range(0, arenaBuilder.GetArenaWidth());
            }

            if (RewardSpawnPos.y == -1)
            {
                spawnPosition.y = Random.Range(0, 100);
                Debug.Log("Randomized y: " + spawnPosition.y); // Random value between 0 and 100 for the y-axis to make sure no object is too high.
            }
            else
            {
                spawnPosition.y = RewardSpawnPos.y;
                Debug.Log("Set y to: " + spawnPosition.y);
            }

            if (RewardSpawnPos.z == -1)
            {
                spawnPosition.z = Random.Range(0, arenaBuilder.GetArenaDepth());
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

            if (showObject && objectToControl != null)
            {
                Instantiate(
                    objectToControl,
                    objectToControlSpawnPoint.transform.position,
                    Quaternion.identity
                );
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

    private void RandomizeColor()
    {
        Color randomColor = new Color(Random.value, Random.value, Random.value, 1.0f);
        Renderer rend = this.gameObject.GetComponent<Renderer>();

        if (rend != null)
        {
            rend.material.color = randomColor;
        }
        else
        {
            Debug.LogWarning(
                "No Renderer found on the Pillar-Button to set the color. Please add a Renderer to the Pillar-Button prefab."
            );
        }
    }
}
