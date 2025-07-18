using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArenaBuilders;
using Operations;

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
    public Dictionary<GameObject, int> RewardSpawnCounts { get; private set; } =
        new Dictionary<GameObject, int>();
    private bool IsMoving = false;
    private float _moveDuration;
    private float _resetDuration;
    public List<Operation> Operations { get; set; } = new List<Operation>();

    [SerializeField]
    private GameObject childObjectToMove;

    [SerializeField]
    private Vector3 moveOffset;

    private static int spawnerCounter = 0;
    public int spawnerID;

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

        // Map legacy syntax to an operation
        if (RewardNames != null && RewardNames.Count > 0)
        {
            SpawnReward newOperation = gameObject.AddComponent<SpawnReward>();
            AttachedObjectDetails details = new AttachedObjectDetails(spawnerID.ToString(), transform.position);
            newOperation.Initialize(details);
            newOperation.SpawnProbability = SpawnProbability;
            newOperation.rewardNames = RewardNames;
            newOperation.rewardWeights = RewardWeights;
            newOperation.rewardSpawnPos = RewardSpawnPos;
            if (MaxRewardCounts != null && MaxRewardCounts.Count > 0)
            {
                newOperation.MaxRewardCounts = MaxRewardCounts;
            }
            newOperation.SpawnedRewardSize = SpawnedRewardSize;
            Operations.Add(newOperation);
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

        for (int i = 0; i < Operations.Count; i++)
        {
            Debug.Log($"Performing SpawnerButton operation {i}: {Operations[i]}");
            Operations[i].execute();
        }

        IsMoving = false;
    }
}
