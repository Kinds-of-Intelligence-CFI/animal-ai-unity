using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerStockpiler : GoalSpawner
{

    // for enabling this script when the agent enters the trigger zone (button). The script will not run otherwise when pressed play.
    public bool triggerActivated = false;
    public override void SetInitialValue(float v) { base.SetInitialValue(Mathf.Max(Mathf.Min(v, 1f), 0.2f)); }
    public override void SetFinalValue(float v) { base.SetFinalValue(Mathf.Max(Mathf.Min(v, 1f), 0.2f)); }
    public bool stockpiling = true;
    public int doorOpenDelay = -1; // assuming not using
    public override void SetDoorDelay(float v) {doorOpenDelay = (int)v; }
    public bool infiniteDoorOpens = false; // assuming not using
    public float timeUntilDoorOpens = 1.5f;
    public float timeUntilDoorCloses = 1.5f;
    public override void SetTimeBetweenDoorOpens(float v) {
        // first, check if set to -1 (or < 0); this means "open indefinitely"
        if (v < 0) { infiniteDoorOpens = false; return; }
        // secondly, check is actually above minimum door open time
        if (v < minDoorOpenTime) { Debug.Log(
                "Invalid TimeBetweenDoorOpens (value "+v+" too small)."+
                "Clamping at minimum door-open time: "+minDoorOpenTime);
            v = minDoorOpenTime;
        }
        // if smaller than the door-close time, then change door-close time
        if (v < timeUntilDoorCloses) { timeUntilDoorCloses = v; }
        // 'closed -> open' door change time is remainder
        // (i.e. subtract 'open -> closed' door change time)
        timeUntilDoorOpens = v - timeUntilDoorCloses;
    }
    public float minDoorOpenTime = 1.4f;

    private Queue<BallGoal> waitingList; // for spawned goals waiting to materialise
    private GameObject Door;
    private int apparentSpawnCount; // = spawnCount + len(waitingList), i.e. goals yet-to-materialise

    // Start is called before the first frame update
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

        if (infiniteDoorOpens) {
            if (timeUntilDoorCloses < minDoorOpenTime) { timeUntilDoorCloses = minDoorOpenTime; Debug.Log("WARNING: TimeUntilDoorCloses too small for food release. Clamping to 0..."); }
            if (timeUntilDoorOpens < 0) { timeUntilDoorOpens = 0; Debug.Log("WARNING: negative TimeUntilDoorOpens given. Clamping to 0..."); }
        }
        else { timeUntilDoorOpens = -1; timeUntilDoorCloses = -1; }

        canRandomizeColor = true;
    }

    private void FixedUpdate()
    {
        if (triggerActivated) {

            // Debug.Log("triggerActivated"); 

            if (waitingList.Count > 0 && freeToMaterialise(ripenedSpawnSize)) 
            {
                print("waitingList.Count: " + waitingList.Count);
                BallGoal newGoal = waitingList.Dequeue();
                print("post-Dequeue() waitingList.Count: " + waitingList.Count);
                print("materialising newGoal: " + newGoal.name);
                immaterialStorageChange(newGoal, true);
            }
        }
        else {
            //  Debug.Log("trigger NOT activated");
        }
    }

    public override BallGoal spawnNewGoal(int listID) {
        BallGoal newGoal = base.spawnNewGoal(listID);
        newGoal.name += spawnCount + 1;

        if (!freeToMaterialise(ripenedSpawnSize, newGoal)) {
            immaterialStorageChange(newGoal, false);
            waitingList.Enqueue(newGoal);
        }

        return newGoal;
    }

    private bool freeToMaterialise(float r, BallGoal g=null) {
        Collider[] sphereCheck = Physics.OverlapSphere(transform.position + defaultSpawnPosition, r*0.4f);
        foreach (Collider col in sphereCheck) { if (col.gameObject.tag == spawnObjects[0].tag && (g==null || col.gameObject!=g)) { return false; } }
        return true;
    }

    // physically materialises a spawned goal stuck on the waiting list
    // or dematerialises if spawned goal needs to wait
    private void immaterialStorageChange(BallGoal goal, bool materialising) {
        goal.GetComponent<MeshRenderer>().enabled = materialising;
        goal.GetComponent<SphereCollider>().enabled = materialising;
        Rigidbody rb = goal.GetComponent<Rigidbody>();
        rb.useGravity = materialising;
        rb.isKinematic = !materialising;
        rb.constraints = materialising?RigidbodyConstraints.None:RigidbodyConstraints.FreezePosition;
    }

    private IEnumerator manageDoor(bool includeInitDelay = true, bool doorOpening=true)
    {
        if (includeInitDelay) { yield return new WaitForSeconds(Math.Max(doorOpenDelay, 0)); }

        float dt = 0f; float newSize;
        while (dt < 1)
        {
            newSize = base.interpolate(0, 1, dt, doorOpening?1:0, doorOpening?0:1);
            Door.transform.localScale = new Vector3(Door.transform.localScale.x, newSize, Door.transform.localScale.z);
            dt += Time.fixedDeltaTime;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        if (infiniteDoorOpens) {
            yield return new WaitForSeconds(doorOpening ? timeUntilDoorCloses : timeUntilDoorOpens);
            StartCoroutine(manageDoor(false, !doorOpening));
        }


        yield return null;
    }
}
