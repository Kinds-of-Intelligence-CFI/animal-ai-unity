using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner_InteractiveButton : MonoBehaviour
{
    // Encapsulated fields with public get and private set
    public int ButtonPressCount { get; private set; } = 0;
    public GameObject LastSpawnedReward { get; private set; }
    public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } = new Dictionary<GameObject, int>();

    [SerializeField] private GameObject childObjectToMove;
    [SerializeField] private Vector3 moveOffset;
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float resetDuration = 1f;
    [SerializeField] private Transform rewardSpawnPoint;
    [SerializeField] private GameObject objectToControl; // signposter
    [SerializeField] private bool showObject; // show the signposter prefab?
    [SerializeField] private List<GameObject> rewards;
    [SerializeField] private List<int> rewardWeights;

    private float lastInteractionTime;
    private float totalInteractionInterval = 0f;

    void Start()
    {
        lastInteractionTime = Time.time;
        UpdateObjectVisibility(); // update the object visibility
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("agent"))
        {
            ButtonPressCount++;

            // Start the MoveAndReset coroutine
            StartCoroutine(MoveAndReset());

            GameObject rewardToSpawn = ChooseReward();
            Instantiate(rewardToSpawn, rewardSpawnPoint.transform.position, Quaternion.identity);
            LastSpawnedReward = rewardToSpawn;

            if (!RewardSpawnCounts.ContainsKey(rewardToSpawn))
            {
                RewardSpawnCounts[rewardToSpawn] = 0;
            }
            RewardSpawnCounts[rewardToSpawn]++;

            float currentInteractionTime = Time.time;
            totalInteractionInterval += currentInteractionTime - lastInteractionTime;
            lastInteractionTime = currentInteractionTime;

            Debug.Log("Trigger activated. Debug coming from Spawner_InteractiveButton.cs");
        }
        else
        {
            Debug.Log("Trigger NOT activated. Debug coming from Spawner_InteractiveButton.cs");
        }
    }

    private void UpdateObjectVisibility()
    {
        if (objectToControl != null)
        {
            objectToControl.SetActive(showObject);
        }
    }

    private IEnumerator MoveAndReset()
    {
        Vector3 originalPosition = childObjectToMove.transform.position;
        Vector3 targetPosition = originalPosition + moveOffset;
        float startTime = Time.time;
        while (Time.time < startTime + moveDuration)
        {
            float t = (Time.time - startTime) / moveDuration;
            childObjectToMove.transform.position = Vector3.Lerp(originalPosition, targetPosition, t);
            yield return null;
        }
        childObjectToMove.transform.position = targetPosition;

        startTime = Time.time;
        while (Time.time < startTime + resetDuration)
        {
            float t = (Time.time - startTime) / resetDuration;
            childObjectToMove.transform.position = Vector3.Lerp(targetPosition, originalPosition, t);
            yield return null;
        }
        childObjectToMove.transform.position = originalPosition;
    }

    private GameObject ChooseReward()
    {
        int totalWeight = 0;
        foreach (int weight in rewardWeights)
        {
            totalWeight += weight;
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
    {
        if (ButtonPressCount == 0)
            return 0;

        return totalInteractionInterval / ButtonPressCount;
    }
}
