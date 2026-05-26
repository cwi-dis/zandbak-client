using Orchestrator.App;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    public class ObjectSpawnerBehaviour : MonoBehaviour
    {
        private App.Orchestrator _orchestrator;
        private Session _session;

        private void Awake()
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            _session = _orchestrator.CurrentSession;

            _session.OnObjectSpawned += ObjectSpawned;
        }

        private void OnDestroy()
        {
            _session.OnObjectSpawned -= ObjectSpawned;
        }

        private void ObjectSpawned(SharedObject spawnedObject)
        {
            Debug.Log("Trying to spawn new object with path " + spawnedObject.PrefabName);
            var prefab = Resources.Load<GameObject>(spawnedObject.PrefabName);

            if (prefab)
            {
                Debug.Log("Spawning object...");
                Instantiate(prefab, spawnedObject.Position, spawnedObject.Rotation);
            }
            else
            {
                Debug.LogError($"Could not find prefab {spawnedObject.PrefabName} in Resources/ folder");
            }
        }
    }
}
