using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;

namespace YAMLDefs{
public class YAMLReader{
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

public class ArenaConfig{
    public IDictionary<int, Arena> arenas { get; set;}
}

public class Arena{
    public int t {get; set;} = 0;
    public List<Item> items {get; set;} = new List<Item>();
    public float pass_mark {get; set;} = 0;
    public List<int> blackouts {get; set;} = new List<int>();
    public int random_seed {get; set;} = 0;
}

public class Item{
    public string name {get; set;} = "";
    public List<Vector3> positions { get; set;} = new List<Vector3>();
    public List<float> rotations { get; set;} = new List<float>();
    public List<Vector3> sizes { get; set;} = new List<Vector3>();
    public List<RGB> colors { get; set;} = new List<RGB>();

    // ======== EXTRA/OPTIONAL PARAMETERS ========
    // use for SignPosterboard symbols, Decay/SizeChange rates, Dispenser settings, etc.
    public List<string> skins { get; set; } = new List<string>(); // Agent only
    public List<string> symbolNames { get; set; } = new List<string>(); // SignPosterboard only
    public List<float> delays { get; set; } = new List<float>(); // all uniques except Posterboard
    public List<float> initialValues { get; set; } = new List<float>(); // all w/value change
    public List<float> finalValues { get; set; } = new List<float>(); // " "
    public List<float> changeRates { get; set; } = new List<float>(); // Decay/SizeChange
    public List<int> spawnCounts { get; set; } = new List<int>(); // Spawners only
    public List<RGB> spawnColors { get; set; } = new List<RGB>(); // Spawners only
    public List<float> timesBetweenSpawns { get; set; } = new List<float>(); // Spawners only
    public List<float> ripenTimes { get; set; } = new List<float>(); // SpawnerTree only
    public List<float> doorDelays { get; set; } = new List<float>(); // SpawnerDispenser/Container only
    public List<float> timesBetweenDoorOpens { get; set; } = new List<float>(); // " "
    public List<float> frozenAgentDelays { get; set; } = new List<float>(); // Agent only
    public List<float> moveDurations { get; set; } = new List<float>(); // InteractiveButton only
}

//Not needed - can just use the Unity Vector3 implementation directly
// class Vector3{
//     public float x {get; set;}    
//     public float y {get; set;}
//     public float z {get; set;}
// }

public class RGB{
    public float r {get; set;} = 0;
    public float g {get; set;} = 0;
    public float b {get; set;} = 0;
}

}