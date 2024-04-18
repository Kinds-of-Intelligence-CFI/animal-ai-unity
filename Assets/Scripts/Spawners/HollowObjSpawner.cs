using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ArenaBuilders;

public class HollowObjSpawner : MonoBehaviour
{
	[SerializeField]
	public Vector3 rewardSpawnLocation { get; set; }
	public List<string> rewardToSpawn { get; set; }
	public List<bool> delayRewardSpawn { get; set; }
	public List<float> delayTime { get; set; }
	public List<GameObject> Rewards { get; set; }

	void Start()
	{

	}

}
