using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Despawn an object at a given location
    /// </summary>
    public class DespawnObject : Operation
    {
        public YAMLDefs.Item spawnable;

        private GameObject spawnedObject = null;

        public override void initialise(AttachedObjectDetails attachedObjectDetails)
        {
            base.initialise(attachedObjectDetails);

            TrainingArena trainingArena = FindAnyObjectByType<TrainingArena>();
            spawnedObject = trainingArena.AddNewItemToArena(spawnable);
        }

        public override void execute()
        {
            if (spawnedObject == null)
            {
                Debug.LogError("Object not present: skipping object despawn (object was likely despawned previously or not initialised)");
                return;
            }

            Debug.Log($"Despawning object: {spawnedObject.name} at position {spawnedObject.transform.position}");
            GameObject.Destroy(spawnedObject);
            spawnedObject = null;
        }
    }
}