using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;

/// <summary>
/// YAMLDefs namespace contains classes that are used to deserialize YAML files.
/// </summary>
namespace YAMLDefs
{
	public class YAMLReader
	{
		public YamlDotNet.Serialization.IDeserializer deserializer = new DeserializerBuilder()
			.WithTagMapping("!ArenaConfig", typeof(YAMLDefs.ArenaConfig))
			.WithTagMapping("!Arena", typeof(YAMLDefs.Arena))
			.WithTagMapping("!Item", typeof(YAMLDefs.Item))
			.WithTagMapping("!Vector3", typeof(Vector3))
			.WithTagMapping("!RGB", typeof(YAMLDefs.RGB))
			//.IgnoreUnmatchedProperties() TODO: research this and it's implications.
			.Build();
	}

	public class ArenaConfig
	{
		public IDictionary<int, Arena> arenas { get; set; }
		public bool randomizeArenas { get; set; } = false;
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
		}

		public List<int> blackouts { get; set; } = new List<int>();
		public int random_seed { get; set; } = 0;
	}

	public class Item
	{
		// REQUIRED PARAMETERS
		public string name { get; set; } = "";
		public List<Vector3> positions { get; set; } = new List<Vector3>();
		public List<float> rotations { get; set; } = new List<float>();
		public List<Vector3> sizes { get; set; } = new List<Vector3>();
		public List<RGB> colors { get; set; } = new List<RGB>();

		// EXTRA/OPTIONAL PARAMETERS
		public List<string> skins { get; set; } = new List<string>();
		public List<float> frozenAgentDelays { get; set; } = new List<float>();

		public List<string> symbolNames { get; set; } = new List<string>();

		public List<float> delays { get; set; } = new List<float>();
		public List<float> initialValues { get; set; } = new List<float>();
		public List<float> finalValues { get; set; } = new List<float>();
		public List<int> spawnCounts { get; set; } = new List<int>();
		public List<RGB> spawnColors { get; set; } = new List<RGB>();
		public List<float> timesBetweenSpawns { get; set; } = new List<float>();
		public List<float> doorDelays { get; set; } = new List<float>();
		public List<float> timesBetweenDoorOpens { get; set; } = new List<float>();
		public List<float> changeRates { get; set; } = new List<float>();
		public List<float> ripenTimes { get; set; } = new List<float>();

		public List<float> moveDurations { get; set; } = new List<float>();
		public List<float> resetDurations { get; set; } = new List<float>();
		public float spawnProbability { get; set; } = 1f;
		public List<string> rewardNames { get; set; } = new List<string>();
		public List<float> rewardWeights { get; set; } = new List<float>();
		public Vector3 rewardSpawnPos { get; set; } = new Vector3(0, 0, 0);
		public List<int> maxRewardCounts { get; set; } = new List<int>();
	}

	public class RGB
	{
		public float r { get; set; } = 0;
		public float g { get; set; } = 0;
		public float b { get; set; } = 0;
	}

	public static class AliasMapper
	{
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
			if (AliasMap.TryGetValue(name, out string newName))
			{
				Debug.Log($"Alias found: '{name}' is mapped to '{newName}'");
				return newName;
			}
			return name;
		}
	}
}
