using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Spawn and despawn an object at a given location
    /// </summary>
    public class ToggleObject : Operation
    {
        public YAMLDefs.Item spawnable;
        public bool objectInitiallyPresent;
        public bool spawnAndForget = false;
        public delegate void OnRewardSpawned(GameObject reward);
        public static event OnRewardSpawned RewardSpawned;
        private GameObject spawnedObject = null;

        public override void initialise(AttachedObjectDetails attachedObjectDetails)
        {
            base.initialise(attachedObjectDetails);

            if (objectInitiallyPresent) {
                TrainingArena trainingArena = FindAnyObjectByType<TrainingArena>();
                if (!spawnAndForget) {
                    spawnedObject = trainingArena.AddNewItemToArena(spawnable);
                }
            }
        }

        public override void execute()
        {
            if (spawnedObject == null)
            {
                spawnObject();
                return;
            }

            despawnObject();
        }

        private void spawnObject()
        {
            Debug.Log($"Spawning object: {spawnable.name} at position {spawnable.positions}");
            TrainingArena trainingArena = FindAnyObjectByType<TrainingArena>();

            GameObject current_spawnedObject = trainingArena.AddNewItemToArena(spawnable);
            if (!spawnAndForget) {
                spawnedObject = current_spawnedObject;
            }

            // TODO: Is there a better place for this object-specific behaviour?
            if (current_spawnedObject != null && current_spawnedObject.name.Contains("Goal"))
            {
                if (string.IsNullOrEmpty(attachedObjectDetails.ID)) {
                    Debug.LogWarning("Spawn Object Operation attached object details not initialised: spawn details will not be logged in the CSV");
                }
                TrainingAgent agent = FindAnyObjectByType<TrainingAgent>();
                if (agent != null)
                {
                    Vector3 spawnerPos = attachedObjectDetails.location;
                    string rewardType = current_spawnedObject.name.Replace("(Clone)", "");
                    string spawnerInfo =
                        $"SpawnerButtonID:{attachedObjectDetails.ID}, Position:{spawnerPos.x},{spawnerPos.y},{spawnerPos.z}, RewardType:{rewardType}";
                    Debug.Log($"Logging SpawnerButton Info: {spawnerInfo}");
                    agent.RecordSpawnerInfo(spawnerInfo);
                }
                else
                {
                    Debug.LogError("Training Agent not found in the scene.");
                }

                RewardSpawned?.Invoke(current_spawnedObject);
            }
        }

        private void despawnObject()
        {
            Debug.Log($"Despawning object: {spawnedObject.name} at position {spawnedObject.transform.position}");
            GameObject.Destroy(spawnedObject);
            spawnedObject = null;
        }
    }
}