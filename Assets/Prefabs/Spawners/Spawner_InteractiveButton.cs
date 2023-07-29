using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
    // Public properties to track button press counts, last spawned reward, and reward spawn counts
    public int ButtonPressCount { get; private set; }
    public GameObject LastSpawnedReward { get; private set; }
    public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } = new Dictionary<GameObject, int>();

    // Serialized fields for customizing button and reward behavior in Unity Editor
    [SerializeField] private GameObject childObjectToMove;
    [SerializeField] private Vector3 moveOffset;
    private float _moveDuration;
    private float _resetDuration;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private GameObject objectToControl;
    [SerializeField] private bool showObject;
    [SerializeField] private List<GameObject> rewards;
    [SerializeField] private List<int> rewardWeights;
    [SerializeField] private Transform objectToControlSpawnPoint;

    private float lastInteractionTime;
    private float totalInteractionInterval = 0f;
    public delegate void OnRewardSpawned(GameObject reward);
    public static event OnRewardSpawned RewardSpawned;

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
        lastInteractionTime = Time.time;
        UpdateObjectVisibility(); // Set the object to be visible or not based on the showObject flag
    }

    private void OnTriggerEnter(Collider other)

    // Interaction logic: button press count, start moving button, choose reward, spawn reward
    // calculate interaction time, log information, spawn objectToControl (if any)
    {
        if (other.CompareTag("agent"))
        {
            ButtonPressCount++;
            StartCoroutine(MoveAndReset());

            if (rewards == null || rewards.Count == 0)
            {
                Debug.LogError("No rewards are set to be spawned.");
                return;
            }

            GameObject rewardToSpawn = ChooseReward();

            if (rewardToSpawn == null)
            {
                Debug.LogError("Failed to choose a reward to spawn.");
                return;
            }

            LastSpawnedReward = Instantiate(rewardToSpawn, rewardSpawnPoint.transform.position, Quaternion.identity);

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
                Instantiate(objectToControl, objectToControlSpawnPoint.transform.position, Quaternion.identity);
            }

            float currentInteractionTime = Time.time;
            totalInteractionInterval += currentInteractionTime - lastInteractionTime;
            lastInteractionTime = currentInteractionTime;

            //Debug.Log("Trigger activated. Debug coming from Spawner_InteractiveButton.cs");

            RewardSpawned?.Invoke(LastSpawnedReward);
        }
        else
        {
            // Debug.Log("Trigger NOT activated. Debug coming from Spawner_InteractiveButton.cs");
        }
    }

    private void UpdateObjectVisibility()

    // Check for objectToControl and set its visibility
    {
        if (objectToControl != null)
        {
            objectToControl.SetActive(showObject);
        }
    }

    public IEnumerator MoveAndReset()

    // Coroutine to animate button press (move button and reset its position)
    {
        Vector3 originalPosition = childObjectToMove.transform.position;
        Vector3 targetPosition = originalPosition + moveOffset;
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
    }

    public bool MoveToTarget(Vector3 origin, Vector3 target, float startTime, float duration)

    // Move the button from origin to target in duration seconds
    {
        float t = (Time.time - startTime) / MoveDuration;
        childObjectToMove.transform.position = Vector3.Lerp(origin, target, t);
        return Time.time < startTime + MoveDuration;
    }

    private GameObject ChooseReward()

    // Chooses and returns reward based on the defined reward weights
    {
        if (rewards == null || rewards.Count == 0 || rewardWeights == null || rewardWeights.Count == 0 || rewards.Count != rewardWeights.Count)
        {
            Debug.LogError("Invalid rewards or reward weights setup.");
            return null;
        }

        int totalWeight = 0;
        for (int i = 0; i < rewardWeights.Count; i++)
        {
            totalWeight += rewardWeights[i];
        }

        int randomValue = Random.Range(0, totalWeight);
        for (int i = 0; i < rewards.Count; i++)
        {
            if (randomValue < rewardWeights[i])
            {
                return rewards[i];
            }
            randomValue -= rewardWeights[i];
        }
        return rewards[rewards.Count - 1];
    }

    public float GetAverageInteractionInterval()

    // Calculate and return average interaction interval
    {
        if (ButtonPressCount == 0)
            return 0;

        return totalInteractionInterval / ButtonPressCount;
    }
}
