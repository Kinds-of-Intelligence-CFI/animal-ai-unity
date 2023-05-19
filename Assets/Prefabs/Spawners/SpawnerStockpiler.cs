using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerStockpiler : GoalSpawner
{
    //[Header("Interactive Button")]
    //[HideInInspector]
    public bool triggerActivated = false; // toggle to activate the trigger. If set to False, the spawner will not spawn goals without trigger activatio via button.
    public override void SetInitialValue(float v) { base.SetInitialValue(Mathf.Max(Mathf.Min(v, 1f), 0.2f)); } // clamp to [0.2, 1] 
    public override void SetFinalValue(float v) { base.SetFinalValue(Mathf.Max(Mathf.Min(v, 1f), 0.2f)); } // clamp to [0.2, 1]
    public bool stockpiling = true; // if true, then the spawner will stockpile goals
    public int doorOpenDelay = -1; // -1 means "open indefinitely"
    public override void SetDoorDelay(float v) { doorOpenDelay = (int)v; } // cast to int
    public bool infiniteDoorOpens = false; // assuming not using
    public float timeUntilDoorOpens = 1.5f; // time until door opens
    public float timeUntilDoorCloses = 1.5f; // time until door closes
    public float minDoorOpenTime = 1.4f; // minimum time the door can be open for
    private Queue<BallGoal> waitingList; // for spawned goals waiting to materialise
    private GameObject Door;
    private int apparentSpawnCount; // = spawnCount + len(waitingList), i.e. goals yet-to-materialise

    public override void SetTimeBetweenDoorOpens(float v)
    {
        // first, check if set to -1 (or < 0); this means "open indefinitely"
        if (v < 0) { infiniteDoorOpens = false; return; }
        // secondly, check is actually above minimum door open time
        if (v < minDoorOpenTime)
        {
            Debug.Log(
                "Invalid TimeBetweenDoorOpens (value " + v + " too small)." +
                "Clamping at minimum door-open time: " + minDoorOpenTime);
            v = minDoorOpenTime;
        }
        // if smaller than the door-close time, then change door-close time
        if (v < timeUntilDoorCloses) { timeUntilDoorCloses = v; }
        // 'closed -> open' door change time is remainder
        // (i.e. subtract 'open -> closed' door change time)
        timeUntilDoorOpens = v - timeUntilDoorCloses;
    }

    public override void Awake()
    {
        base.Awake();
        if (stockpiling)
        {
            Door = transform.GetChild(1).gameObject;
            if (!Door.name.ToLower().Contains("door")) throw new Exception("WARNING: a stockpiling GoalSpawner has not found its Door.");
        }

        waitingList = new Queue<BallGoal>();
        if (stockpiling) { StartCoroutine(manageDoor()); }

        if (infiniteDoorOpens)
        {
            if (timeUntilDoorCloses < minDoorOpenTime) { timeUntilDoorCloses = minDoorOpenTime; Debug.Log("WARNING: TimeUntilDoorCloses too small for food release. Clamping to 0..."); }
            if (timeUntilDoorOpens < 0) { timeUntilDoorOpens = 0; Debug.Log("WARNING: negative TimeUntilDoorOpens given. Clamping to 0..."); }
        }
        else { timeUntilDoorOpens = -1; timeUntilDoorCloses = -1; }

        canRandomizeColor = true;
    }

    private void FixedUpdate()
    {
        if (triggerActivated)
        {

            Debug.Log("Trigger activated and commencing spawning. Debug coming from SpawnerStockpiler.cs");

            if (waitingList.Count > 0 && freeToMaterialise(ripenedSpawnSize))
            {
                print("waitingList.Count: " + waitingList.Count);
                BallGoal newGoal = waitingList.Dequeue();
                print("post-Dequeue() waitingList.Count: " + waitingList.Count);
                print("materialising newGoal: " + newGoal.name);
                immaterialStorageChange(newGoal, true);
            }
        }
        else
        {
            Debug.Log("Trigger NOT activated. Debug coming from SpawnerStockpiler.cs");
        }
    }

    // spawns a new goal and adds it to the waiting list if necessary
    public override BallGoal spawnNewGoal(int listID)
    {
        BallGoal newGoal = base.spawnNewGoal(listID);
        newGoal.name += spawnCount + 1;

        if (!freeToMaterialise(ripenedSpawnSize, newGoal))
        {
            immaterialStorageChange(newGoal, false);
            waitingList.Enqueue(newGoal);
        }

        return newGoal; // return the new goal
    }

    // checks if a goal can materialise at a given position
    private bool freeToMaterialise(float r, BallGoal g = null)
    {
        Collider[] sphereCheck = Physics.OverlapSphere(transform.position + defaultSpawnPosition, r * 0.4f);
        foreach (Collider col in sphereCheck) { if (col.gameObject.tag == spawnObjects[0].tag && (g == null || col.gameObject != g)) { return false; } }
        return true;
    }

    // physically materialises a spawned goal stuck on the waiting list
    // or dematerialises if spawned goal needs to wait
    private void immaterialStorageChange(BallGoal goal, bool materialising)
    {
        goal.GetComponent<MeshRenderer>().enabled = materialising;
        goal.GetComponent<SphereCollider>().enabled = materialising;
        Rigidbody rb = goal.GetComponent<Rigidbody>();
        rb.useGravity = materialising;
        rb.isKinematic = !materialising;
        rb.constraints = materialising ? RigidbodyConstraints.None : RigidbodyConstraints.FreezePosition;
    }

    // manages the door opening and closing
    private IEnumerator manageDoor(bool includeInitDelay = true, bool doorOpening = true)
    {
        if (includeInitDelay) { yield return new WaitForSeconds(Math.Max(doorOpenDelay, 0)); }

        float dt = 0f; float newSize;
        while (dt < 1)
        {
            newSize = base.interpolate(0, 1, dt, doorOpening ? 1 : 0, doorOpening ? 0 : 1);
            Door.transform.localScale = new Vector3(Door.transform.localScale.x, newSize, Door.transform.localScale.z);
            dt += Time.fixedDeltaTime;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        if (infiniteDoorOpens)
        {
            yield return new WaitForSeconds(doorOpening ? timeUntilDoorCloses : timeUntilDoorOpens);
            StartCoroutine(manageDoor(false, !doorOpening));
        }

        yield return null;
    }
}
