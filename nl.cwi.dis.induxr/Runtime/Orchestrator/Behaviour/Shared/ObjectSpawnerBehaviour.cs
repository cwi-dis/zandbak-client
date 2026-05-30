using System.Collections.Generic;
using Orchestrator.App;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    public class ObjectSpawnerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private SharedPrefabRegistry prefabRegistry;

        private App.Orchestrator _orchestrator;
        private Session _session;

        private Dictionary<string, GameObject> _spawnedObjects = new();

        private void Awake()
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            _session = _orchestrator.CurrentSession;

            _session.DynamicSharedObjects.ForEach(ObjectSpawned);

            _session.OnObjectSpawned += ObjectSpawned;
            _session.OnObjectDestroyed += ObjectDestroyed;
        }

        private void OnDestroy()
        {
            _session.OnObjectSpawned -= ObjectSpawned;
        }

        private void ObjectSpawned(SharedObject spawnedObject)
        {
            Debug.Log("Trying to spawn new object with name " + spawnedObject.PrefabName);
            var prefab = prefabRegistry.GetPrefab(spawnedObject.PrefabName);

            if (prefab)
            {
                Debug.Log("Spawning object...");
                var obj = Instantiate(prefab, spawnedObject.Position, spawnedObject.Rotation);
                _spawnedObjects.Add(spawnedObject.Id, obj);
            }
            else
            {
                Debug.LogError($"Could not find prefab {spawnedObject.PrefabName} in prefab registry");
            }
        }

        private void ObjectDestroyed(SharedObject sharedObject)
        {
            Debug.Log("Destroying object with id: " + sharedObject.Id);

            if (_spawnedObjects.TryGetValue(sharedObject.Id, out var obj))
            {
                Destroy(obj);
                _spawnedObjects.Remove(sharedObject.Id);

                Debug.Log($"Object with ID {sharedObject.Id} destroyed");
            }
            else
            {
                Debug.LogWarning($"Object with id {sharedObject.Id} not found in spawned objects dictionary");
            }
        }
    }
}
