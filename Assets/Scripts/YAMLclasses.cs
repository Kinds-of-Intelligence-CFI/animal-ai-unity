using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;

namespace YAMLDefs
{
	public class YAMLReader
	{
		/// <summary>
		/// A deserialiser for reading YAML files in AmimalAI Format
		/// </summary>
		public YamlDotNet.Serialization.IDeserializer deserializer = new DeserializerBuilder()
			.WithTagMapping("!ArenaConfig", typeof(YAMLDefs.ArenaConfig))
			.WithTagMapping("!Arena", typeof(YAMLDefs.Arena))
			.WithTagMapping("!Item", typeof(YAMLDefs.Item))
			.WithTagMapping("!Vector3", typeof(Vector3))
			.WithTagMapping("!RGB", typeof(YAMLDefs.RGB))
			.Build();
	}

	public class ArenaConfig
	{
		public IDictionary<int, Arena> arenas { get; set; }
		public bool randomizeArenas = false;
		public bool showNotification { get; set; } = false;
		public bool canResetEpisode { get; set; } = true;
		public bool canChangePerspective { get; set; } = true;
	}

	public class Arena
	{
		public int t { get; set; } = 0;
		public List<Item> items { get; set; } = new List<Item>();
		public float pass_mark { get; set; } = 0;
		public static float CurrentPassMark { get; private set; }

		public void SetCurrentPassMark()
		{
			CurrentPassMark = pass_mark;
			Debug.Log("Current Pass Mark: " + CurrentPassMark);
			Debug.Log("Pass Mark: " + pass_mark);
		}

		public List<int> blackouts { get; set; } = new List<int>();
		public int random_seed { get; set; } = 0;
	}

	public class Item
	{
		// ======== MANDATORY PARAMETERS (ALL OBJECTS) ========
		public string name { get; set; } = "";
		public List<Vector3> positions { get; set; } = new List<Vector3>();
		public List<float> rotations { get; set; } = new List<float>();
		public List<Vector3> sizes { get; set; } = new List<Vector3>();
		public List<RGB> colors { get; set; } = new List<RGB>();

		// ======== EXTRA/OPTIONAL PARAMETERS ========
		public List<string> skins { get; set; } = new List<string>(); // Agent only (this is optional as the default is randomized skin selection)
		public List<float> frozenAgentDelays { get; set; } = new List<float>(); // Agent only

		public List<string> symbolNames { get; set; } = new List<string>(); // SignBoard only

		public List<float> delays { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		public List<float> initialValues { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		public List<float> finalValues { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		public List<int> spawnCounts { get; set; } = new List<int>(); // SpawnerDispenser/Container only
		public List<RGB> spawnColors { get; set; } = new List<RGB>(); // SpawnerDispenser/Container only
		public List<float> timesBetweenSpawns { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		public List<float> doorDelays { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		public List<float> timesBetweenDoorOpens { get; set; } = new List<float>(); // SpawnerDispenser/Container only
		
		public List<float> changeRates { get; set; } = new List<float>(); // Decay/SizeChange only (RIPEN/DECAY GOALS only)
		
		public List<float> ripenTimes { get; set; } = new List<float>(); // SpawnerTree only
		
		public List<float> moveDurations { get; set; } = new List<float>(); // InteractiveButton only
		public List<float> resetDurations { get; set; } = new List<float>(); // InteractiveButton only
		public float spawnProbability { get; set; } = 1f; // InteractiveButton only
		public List<string> rewardNames { get; set; } = new List<string>(); // InteractiveButton only
		public List<float> rewardWeights { get; set; } = new List<float>(); // InteractiveButton only
		public Vector3 rewardSpawnPos { get; set; } = new Vector3(0, 0, 0); // InteractiveButton only
		public List<int> maxRewardCounts { get; set; } = new List<int>(); // InteractiveButton only
	}

	public class RGB
	{
		public float r { get; set; } = 0;
		public float g { get; set; } = 0;
		public float b { get; set; } = 0;
	}

	public static class AliasMapper
	{
		private static readonly HashSet<string> ObjectsToCheck = new HashSet<string>
	{ 
		// As of 11/20/2023, the below objects have been assigned new names and are now assigned aliases.
		"Cardbox1",
		"Cardbox2",
		"LObject",
		"LObject2",
		"UObject",
		"AntiDecayGoal",
		"SpawnerDispenser",
		"SpawnerContainer",
		"SignPosterboard",
		"Pillar-Button"
	};
		private static readonly Dictionary<string, string> AliasMap = new Dictionary<string, string>
		{
			{ "Cardbox1", "LightBlock" },
			{ "Cardbox2", "HeavyBlock" },
			{ "LObject", "LBlock" },
			{ "LObject2", "JBlock" },
			{ "UObject", "UBlock" },
			{ "AntiDecayGoal", "RipenGoal" },
			{ "SpawnerDispenser", "SpawnerDispenserTall" },
			{ "SpawnerContainer", "SpawnerContainerShort" },
			{ "SignPosterboard", "SignBoard" },
			{ "Pillar-Button", "SpawnerButton" },
		};

		public static string ResolveAlias(string name)
		{
			if (ObjectsToCheck.Contains(name) && AliasMap.TryGetValue(name, out string newName))
			{
				Debug.Log($"Alias found: '{name}' is mapped to '{newName}'");
				return newName;
			}
			return name;
		}
	}
}
