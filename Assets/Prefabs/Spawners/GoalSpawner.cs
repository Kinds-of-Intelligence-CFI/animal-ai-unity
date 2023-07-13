using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ArenaBuilders;

public class GoalSpawner : Prefab
{
    [Header("Spawning Params")]
    public BallGoal[] spawnObjects;
    public float initialSpawnSize;

    public override void SetInitialValue(float v)
    {
        initialSpawnSize = Mathf.Clamp(v, 0.2f, 3f);
    }

    public float ripenedSpawnSize;

    public override void SetFinalValue(float v)
    {
        ripenedSpawnSize = Mathf.Clamp(v, 0.2f, 3f);
    }

    public bool variableSize;
    public bool variableSpawnPosition;
    public float sphericalSpawnRadius;
    public Vector3 defaultSpawnPosition;
    public float timeToRipen; // in seconds

    public override void SetRipenTime(float v)
    {
        timeToRipen = v;
    }

    public float timeBetweenSpawns; // also in seconds

    public override void SetTimeBetweenSpawns(float v)
    {
        timeBetweenSpawns = v;
    }

    public float delaySeconds;

    public override void SetDelay(float v)
    {
        delaySeconds = (int)v;
    }

    public int spawnCount; // total number spawner can spawn; -1 if infinite

    public override void SetSpawnCount(float v)
    {
        spawnCount = (int)v;
    }

    [ColorUsage(true, true)]
    private Color colourOverride;

    public override void SetSpawnColor(Vector3 v)
    {
        // overwrite only if not (-1, -1, -1); this is a substitute for 'null' in the YAML configs
        if (v != -Vector3.one)
        {
            colourOverride.r = v.x / 255f;
            colourOverride.g = v.y / 255f;
            colourOverride.b = v.z / 255f;
        } // HDR intensity constrained automatically at 0 from initial/default colourOverride value
    }

    private bool willSpawnInfinite()
    {
        return spawnCount == -1;
    }

    private bool canStillSpawn()
    {
        return spawnCount != 0;
    }

    //private bool isSpawning = false;

    private float height;

    private ArenaBuilder AB;

    // random-object-spawning toggle and associated objects
    private bool spawnsRandomObjects;
    public int objSpawnSeed = 0;
    public int spawnSizeSeed = 0;

    /* IMPORTANT use ''System''.Random so can be locally instanced;
     * ..this allows us to fix a sequence via a particular seed.
     * Four RNGs depending on which random variations are toggled:
     * (1) OBJECT: for spawn-object selection
     * (2) SIZE: for eventual size of spawned object when released
     * (3) H_ANGLE: proportion around the tree where spawning occurs
     * (4) V_ANGLE: extent up the tree where spawning occurs */
    private System.Random[] RNGs = new System.Random[4];

    private enum E
    {
        OBJECT = 0,
        SIZE = 1,
        ANGLE = 2
    };

    public virtual void Awake()
    {
        // overwrite 'typicalOrigin' because origin of geometry is at base
        typicalOrigin = false;
        // combats random size setting from ArenaBuilder
        sizeMin = sizeMax = Vector3Int.one;
        canRandomizeColor = false;
        ratioSize = Vector3Int.one;

        height = GetComponent<Renderer>().bounds.size.y;
        AB = this.transform.parent.parent.GetComponent<TrainingArena>().Builder;

        // sets to random if more than one spawn object to choose from
        // else just spawns the same object repeatedly
        // assumes uniform random sampling (for now?)
        spawnsRandomObjects = (spawnObjects.Length > 1);
        if (spawnsRandomObjects)
        {
            RNGs[(int)E.OBJECT] = new System.Random(objSpawnSeed);
        }
        if (variableSize)
        {
            RNGs[(int)E.SIZE] = new System.Random(spawnSizeSeed);
        }
        if (variableSpawnPosition)
        {
            RNGs[(int)E.ANGLE] = new System.Random(1);
        }

        // by default, ignore initialSpawnSize is there is no 'ripening' phase
        if (timeToRipen <= 0)
        {
            initialSpawnSize = ripenedSpawnSize;
        }

        StartCoroutine(startSpawning());
    }

    public override void SetSize(Vector3 size)
    {
        // bypasses random size assignment (used e.g. by ArenaBuilder) from parent Prefab class,
        // fixing to desired size otherwise just changes size as usual
        sizeMin = sizeMax = Vector3Int.one; //new Vector3(0.311f, 0.319f, 0.314f);
        base.SetSize(Vector3Int.one);
        _height = height;
    }

    protected override float AdjustY(float yIn)
    {
        return yIn;
        // trivial call - just in case of the GoalSpawner, we have origin at the bottom not in middle of bounding box
        // so no need to compensate for origin position via AdjustY
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + defaultSpawnPosition, sphericalSpawnRadius);
        var bs = transform.GetComponent<Renderer>().bounds.size;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, bs.y / 2, 0), bs);
        Gizmos.DrawSphere(transform.position + defaultSpawnPosition, 0.5f);
    }

    private IEnumerator startSpawning()
    {
        yield return new WaitForSeconds(delaySeconds);

        //isSpawning = true;
        while (canStillSpawn())
        {
            // spawn first, wait second, repeat

            BallGoal newGoal = spawnNewGoal(0);
            if (variableSize)
            {
                var sizeNoise = newGoal.reward - initialSpawnSize;
                StartCoroutine(manageRipeningGrowth(newGoal, sizeNoise));
                StartCoroutine(waitForRipening(newGoal, sizeNoise));
            }
            else
            {
                StartCoroutine(manageRipeningGrowth(newGoal));
                StartCoroutine(waitForRipening(newGoal));
            }

            if (!willSpawnInfinite())
            {
                spawnCount--;
            }

            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    public virtual BallGoal spawnNewGoal(int listID)
    {
        // calculate spawning location if necessary
        Vector3 spawnPos;
        if (variableSpawnPosition)
        {
            float phi /*azimuthal angle*/
            = (float)(RNGs[(int)E.ANGLE].NextDouble() * 2 * Math.PI);
            float theta /*polar/inclination angle*/
            = (float)((RNGs[(int)E.ANGLE].NextDouble() * 0.6f + 0.2f) * Math.PI);
            spawnPos =
                defaultSpawnPosition + sphericalToCartesian(sphericalSpawnRadius, theta, phi);
        }
        else
        {
            spawnPos = defaultSpawnPosition;
        }

        BallGoal newGoal = (BallGoal)Instantiate(
            spawnObjects[listID],
            transform.position + spawnPos,
            Quaternion.identity
        );
        AB.AddToGoodGoalsMultiSpawned(newGoal);
        newGoal.transform.parent = this.transform;
        float sizeNoise = variableSize
            ? ((float)(RNGs[(int)E.SIZE].NextDouble() - 0.5f) * 0.5f)
            : 0;
        // edit the prefab we have been given to make sure we can actually set fruit size as we want to
        newGoal.sizeMax = Vector3.one * (ripenedSpawnSize + (variableSize ? 0.25f : 0f));
        newGoal.sizeMin = Vector3.one * (initialSpawnSize - (variableSize ? 0.25f : 0f));
        newGoal.SetSize(Vector3.one * (initialSpawnSize + sizeNoise));
        //print("newGoal size and reward: SIZE " + newGoal.transform.localScale.x + " REWARD " + newGoal.reward);

        // Use this to override the colour of the balls spawned by this spawner:
        // if (colourOverride != null) {
        //     Material _mat = newGoal.GetComponent<MeshRenderer>().material;
        //     _mat.SetColor("_EmissionColor", colourOverride);
        // }

        newGoal.gameObject.GetComponent<Rigidbody>().useGravity = false;
        newGoal.gameObject.GetComponent<Rigidbody>().isKinematic = true;

        newGoal.enabled = true;
        return newGoal;
    }

    private IEnumerator waitForRipening(BallGoal newGoal, float sizeNoise = 0)
    {
        yield return new WaitForSeconds(timeToRipen);

        if (newGoal != null)
        {
            // now ensure its growth is complete at exactly ripenedSpawnSize
            newGoal.SetSize(
                new Func<float, Vector3>(x => new Vector3(x, x, x))(ripenedSpawnSize + sizeNoise)
            );
            // toggle kinematic/gravity settings.
            newGoal.gameObject.GetComponent<Rigidbody>().useGravity = true;
            newGoal.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    private IEnumerator manageRipeningGrowth(BallGoal newGoal, float sizeNoise = 0)
    {
        float dt = 0f;
        float newSize;
        while (dt < timeToRipen && newGoal != null)
        {
            newSize = interpolate(
                0,
                timeToRipen,
                dt,
                initialSpawnSize + sizeNoise,
                ripenedSpawnSize + sizeNoise
            );
            newGoal.SetSize(new Func<float, Vector3>(x => new Vector3(x, x, x))(newSize));
            dt += Time.fixedDeltaTime;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }

        yield return null;
    }

    Vector3 sphericalToCartesian(float r, float theta, float phi)
    {
        float sin_theta = Mathf.Sin(theta);
        return new Vector3(
            r * Mathf.Cos(phi) * sin_theta,
            r * Mathf.Cos(theta),
            r * Mathf.Sin(phi) * sin_theta
        );
    }

    public float interpolate(float tLo, float tHi, float t, float sLo, float sHi)
    {
        t = Mathf.Clamp(t, tLo, tHi); // ensure t is actually clamped within [tLo, tHi]
        float p = (t - tLo) / (tHi - tLo); // get proportion to interpolate with
        return sHi * p + sLo * (1 - p);
    }
}
