using System;
using System.Collections;
using UnityEngine;
using ArenaBuilders;

/// <summary>
/// Spawns goals in the training arena. The base class for all goal spawners/dispensers.
/// </summary>
public class GoalSpawner : Prefab
{
	[Header("Spawning Parameters")]
	public BallGoal[] spawnObjects;
	public float initialSpawnSize;
	public float ripenedSpawnSize;
	public bool variableSize;
	public bool variableSpawnPosition;
	public float sphericalSpawnRadius;
	public Vector3 defaultSpawnPosition;
	public float timeToRipen; // Seconds
	public float timeBetweenSpawns; // Seconds
	public float delaySeconds; // Seconds
	public int spawnCount; // '-1' = infinite spawning

	[ColorUsage(true, true)]
	private Color colourOverride;

	// Random Seeds
	public int objSpawnSeed = 0;
	public int spawnSizeSeed = 0;

	// RNGs
	private System.Random[] RNGs = new System.Random[4];
	private enum E { OBJECT = 0, SIZE = 1, ANGLE = 2 }

	private float height;
	private ArenaBuilder AB;
	private bool spawnsRandomObjects;

	public virtual void Awake()
	{
		InitializeParameters();
		StartCoroutine(StartSpawning());
	}

	// Initialize spawning parameters and RNGs
	private void InitializeParameters()
	{
		typicalOrigin = false;
		sizeMin = sizeMax = Vector3Int.one;
		canRandomizeColor = false;
		ratioSize = Vector3Int.one;

		height = GetComponent<Renderer>().bounds.size.y;
		AB = transform.parent.parent.GetComponent<TrainingArena>().Builder;

		spawnsRandomObjects = (spawnObjects.Length > 1);

		if (spawnsRandomObjects)
			RNGs[(int)E.OBJECT] = new System.Random(objSpawnSeed);

		if (variableSize)
			RNGs[(int)E.SIZE] = new System.Random(spawnSizeSeed);

		if (variableSpawnPosition)
			RNGs[(int)E.ANGLE] = new System.Random(1);

		if (timeToRipen <= 0)
			initialSpawnSize = ripenedSpawnSize;
	}

	private IEnumerator StartSpawning()
	{
		yield return new WaitForSeconds(delaySeconds);

		while (CanStillSpawn())
		{
			BallGoal newGoal = SpawnNewGoal(0);
			StartCoroutine(ManageSingleSpawnLifeCycle(newGoal, variableSize ? (newGoal.reward - initialSpawnSize) : 0));

			if (!WillSpawnInfinite())
				spawnCount--;

			yield return new WaitForSeconds(timeBetweenSpawns);
		}
	}

	private bool CanStillSpawn()
	{
		return spawnCount != 0;
	}

	private bool WillSpawnInfinite()
	{
		return spawnCount == -1;
	}

	public virtual BallGoal SpawnNewGoal(int listID)
	{
		Vector3 spawnPos = variableSpawnPosition ? CalculateSpawnPosition() : defaultSpawnPosition;

		BallGoal newGoal = (BallGoal)Instantiate(spawnObjects[listID], transform.position + spawnPos, Quaternion.identity);
		AB.AddToGoodGoalsMultiSpawned(newGoal);
		newGoal.transform.parent = transform;

		if (variableSize)
			SetVariableSize(newGoal);

		return newGoal;
	}

	// Calculate spawn position
	private Vector3 CalculateSpawnPosition()
	{
		float phi = (float)(RNGs[(int)E.ANGLE].NextDouble() * 2 * Math.PI);
		float theta = (float)((RNGs[(int)E.ANGLE].NextDouble() * 0.6f + 0.2f) * Math.PI);
		return defaultSpawnPosition + SphericalToCartesian(sphericalSpawnRadius, theta, phi);
	}

	// Set variable size for a goal
	private void SetVariableSize(BallGoal goal)
	{
		float sizeNoise = (float)(RNGs[(int)E.SIZE].NextDouble() - 0.5f) * 0.5f;
		goal.sizeMax = Vector3.one * (ripenedSpawnSize + 0.25f);
		goal.sizeMin = Vector3.one * (initialSpawnSize - 0.25f);
		goal.gameObject.GetComponent<Rigidbody>().useGravity = false;
		goal.gameObject.GetComponent<Rigidbody>().isKinematic = true;
		goal.enabled = true;
	}

	// Convert spherical coordinates to cartesian
	private Vector3 SphericalToCartesian(float r, float theta, float phi)
	{
		float sin_theta = Mathf.Sin(theta);
		return new Vector3(r * Mathf.Cos(phi) * sin_theta, r * Mathf.Cos(theta), r * Mathf.Sin(phi) * sin_theta);
	}

	// Manage the entire life cycle of a spawned object
	private IEnumerator ManageSingleSpawnLifeCycle(BallGoal newGoal, float sizeNoise = 0)
	{
		float dt = 0f;
		float newSize;

		while (dt < timeToRipen && newGoal != null)
		{
			newSize = Mathf.Clamp(interpolate(0, timeToRipen, dt, initialSpawnSize + sizeNoise, ripenedSpawnSize + sizeNoise), initialSpawnSize, ripenedSpawnSize);
			newGoal.SetSize(Vector3.one * newSize);
			dt += Time.deltaTime;
			yield return null;
		}

		if (newGoal != null)
		{
			newGoal.SetSize(Vector3.one * ripenedSpawnSize);
			newGoal.gameObject.GetComponent<Rigidbody>().useGravity = true;
			newGoal.gameObject.GetComponent<Rigidbody>().isKinematic = false;
		}
	}

	// Interpolate between two values
	public float interpolate(float tLo, float tHi, float t, float sLo, float sHi)
	{
		t = Mathf.Clamp(t, tLo, tHi);
		float p = (t - tLo) / (tHi - tLo);
		return sHi * p + sLo * (1 - p);
	}
}
