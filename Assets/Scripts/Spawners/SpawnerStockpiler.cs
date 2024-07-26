using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpawnerStockpiler is a GoalSpawner that can stockpile goals until there is space to materialize them.
/// </summary>
public class SpawnerStockpiler : GoalSpawner
{
    [Header("Stockpiling Settings")]
    public bool isStockpiling = true;
    public int doorOpenDelay = -1; /* '-1' means "open indefinitely" */
    public bool isDoorOpenInfinite = false;
    public float timeUntilDoorOpens = 1.5f;
    public float timeUntilDoorCloses = 1.5f;
    public float minDoorOpenTime = 1.4f;

    private GameObject doorObject;
    private Queue<BallGoal> waitingGoals = new Queue<BallGoal>();

    public override void Awake()
    {
        base.Awake();
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        if (isStockpiling)
        {
            doorObject = transform.GetChild(1).gameObject;
            if (!doorObject.name.ToLower().Contains("door"))
                throw new Exception("WARNING: A stockpiling GoalSpawner has not found its Door.");

            StartCoroutine(ManageDoor());
        }

        if (isDoorOpenInfinite)
        {
            if (timeUntilDoorCloses < minDoorOpenTime)
            {
                timeUntilDoorCloses = minDoorOpenTime;
                Debug.Log(
                    "WARNING: TimeUntilDoorCloses too small for food release. Clamping to 0..."
                );
            }
            if (timeUntilDoorOpens < 0)
            {
                timeUntilDoorOpens = 0;
                Debug.Log("WARNING: Negative TimeUntilDoorOpens given. Clamping to 0...");
            }
        }
        else
        {
            timeUntilDoorOpens = -1;
            timeUntilDoorCloses = -1;
        }

        canRandomizeColor = true;
    }

    /// <summary>
    /// Manages the door's opening and closing animations.
    /// </summary>
    private IEnumerator ManageDoor(bool includeInitDelay = true, bool isDoorOpening = true)
    {
        if (includeInitDelay)
            yield return new WaitForSeconds(Mathf.Max(doorOpenDelay, 0));

        float deltaTime = 0f;
        float newSize;
        while (deltaTime < 1)
        {
            newSize = base.Interpolate(
                0,
                1,
                deltaTime,
                isDoorOpening ? 1 : 0,
                isDoorOpening ? 0 : 1
            );
            doorObject.transform.localScale = new Vector3(
                doorObject.transform.localScale.x,
                newSize,
                doorObject.transform.localScale.z
            );
            deltaTime += Time.fixedDeltaTime;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        if (isDoorOpenInfinite)
        {
            yield return new WaitForSeconds(
                isDoorOpening ? timeUntilDoorCloses : timeUntilDoorOpens
            );
            StartCoroutine(ManageDoor(false, !isDoorOpening));
        }
    }

    public override BallGoal SpawnNewGoal(int listID)
    {
        BallGoal newGoal = base.SpawnNewGoal(listID);
        newGoal.name += spawnCount + 1;

        if (!FreeToMaterialize(ripenedSpawnSize, newGoal))
        {
            ChangeImmaterialStorage(newGoal, false);
            waitingGoals.Enqueue(newGoal);
        }

        return newGoal;
    }

    private void FixedUpdate()
    {
        if (waitingGoals.Count > 0 && FreeToMaterialize(ripenedSpawnSize))
        {
            BallGoal newGoal = waitingGoals.Dequeue();
            ChangeImmaterialStorage(newGoal, true);
        }
    }

    private bool FreeToMaterialize(float radius, BallGoal goal = null)
    {
        Collider[] sphereCheck = Physics.OverlapSphere(
            transform.position + defaultSpawnPosition,
            radius * 0.4f
        );
        foreach (Collider col in sphereCheck)
        {
            if (
                col.gameObject.tag == spawnObjects[0].tag
                && (goal == null || col.gameObject != goal)
            )
                return false;
        }
        return true;
    }

    /// <summary>
    /// Physically materialize or dematerialize a spawned goal.
    /// </summary>
    private void ChangeImmaterialStorage(BallGoal goal, bool isMaterializing)
    {
        goal.GetComponent<MeshRenderer>().enabled = isMaterializing;
        goal.GetComponent<SphereCollider>().enabled = isMaterializing;
        Rigidbody rb = goal.GetComponent<Rigidbody>();
        rb.useGravity = isMaterializing;
        rb.isKinematic = !isMaterializing;
        rb.constraints = isMaterializing
            ? RigidbodyConstraints.None
            : RigidbodyConstraints.FreezePosition;
    }

    public override void SetTimeBetweenDoorOpens(float value)
    {
        if (value < 0)
        {
            isDoorOpenInfinite = false;
            return;
        }

        if (value < minDoorOpenTime)
        {
            Debug.Log(
                "Invalid TimeBetweenDoorOpens (value "
                    + value
                    + " too small). Clamping at minimum door-open time: "
                    + minDoorOpenTime
            );
            value = minDoorOpenTime;
        }

        if (value < timeUntilDoorCloses)
            timeUntilDoorCloses = value;

        timeUntilDoorOpens = value - timeUntilDoorCloses;
    }
}
