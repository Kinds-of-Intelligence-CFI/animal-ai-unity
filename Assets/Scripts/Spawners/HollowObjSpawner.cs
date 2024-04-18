using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;

public class HollowObjSpawner : MonoBehaviour
{
	public Vector3 rewardSpawnPosition { get; set; }
	public List<string> rewardToSpawn { get; set; }
	public List<float> delayRewardSpawn { get; set; }

	public List<GameObject> Rewards { get; set; }

	void Start()
	{

	}

}
