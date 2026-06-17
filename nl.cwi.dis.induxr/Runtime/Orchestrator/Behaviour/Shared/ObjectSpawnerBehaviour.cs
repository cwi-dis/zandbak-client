using System.Collections.Generic;
using Orchestrator.App;
using Orchestrator.Behaviour.Avatar;
using Orchestrator.ScriptableObjects;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    public class ObjectSpawnerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private SharedObjectPrefabRegistry prefabRegistry;

        [SerializeField]
        private AvatarPrefabRegistry avatarPrefabRegistry;

        private App.Orchestrator _orchestrator;
        private Session _session;

        private readonly Dictionary<string, GameObject> _spawnedObjects = new();
        private readonly Dictionary<string, GameObject> _spawnedAvatars = new();

        private void Awake()
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            _session = _orchestrator.CurrentSession;

            _session.DynamicSharedObjects.ForEach(ObjectSpawned);

            _session.OnObjectSpawned += ObjectSpawned;
            _session.OnObjectDestroyed += ObjectDestroyed;

            // Spawning avatars for users already in the session
            _session.Users.ForEach(UserJoined);

            _session.OnUserJoined += UserJoined;
            _session.OnUserLeft += UserLeft;
        }

        private void Start()
        {
            // Spawn local avatar
            SpawnLocalAvatar();
        }

        private void OnDestroy()
        {
            _session.OnObjectSpawned -= ObjectSpawned;
            _session.OnObjectDestroyed -= ObjectDestroyed;

            _session.OnUserJoined -= UserJoined;
            _session.OnUserLeft -= UserLeft;
        }

        private void ObjectSpawned(SharedObject spawnedObject)
        {
            Debug.Log("Trying to spawn new object with name " + spawnedObject.PrefabName);
            var prefab = prefabRegistry.GetPrefab(spawnedObject.PrefabName);

            if (prefab)
            {
                Debug.Log("Spawning object...");

                var obj = Instantiate(prefab, spawnedObject.Position, spawnedObject.Rotation);
                obj.name = $"{obj.name} ({spawnedObject.Id})";

                var sharedObjectBehavior = obj.GetComponent<SharedObjectBehaviour>();
                if (sharedObjectBehavior)
                {
                    sharedObjectBehavior.Id = spawnedObject.Id;
                }

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

            if (_spawnedObjects.Remove(sharedObject.Id, out var obj))
            {
                Destroy(obj);
                Debug.Log($"Object with ID {sharedObject.Id} destroyed");
            }
            else
            {
                Debug.LogWarning($"Object with id {sharedObject.Id} not found in spawned objects dictionary");
            }
        }

        private void UserJoined(User user)
        {
            if (user.Id == _session.Self.Id) return;
            // If user is of a type that cannot be spawned, ignore
            if (!user.DeviceTypeConfig.CanSpawn) return;

            Debug.Log("Spawning new user with id " + user.Id);

            var remotePlayerPrefab = avatarPrefabRegistry.GetPrefab(user.PrefabName);
            var remoteAvatar = Instantiate(remotePlayerPrefab).GetComponent<AvatarBehaviour>();

            remoteAvatar.name = $"{remoteAvatar.name} ({user.Id}: {remoteAvatar.name})";
            remoteAvatar.Initialize(user);

            _spawnedAvatars.Add(user.Id, remoteAvatar.gameObject);
        }

        private void UserLeft(User user, bool force)
        {
            if (_spawnedAvatars.Remove(user.Id, out var obj))
            {
                Destroy(obj);
                Debug.Log("User found, removing and destroying player object");
            }
        }

        private void SpawnLocalAvatar()
        {
            var user = _orchestrator.Self;

            // Getting random spawn position for self
            var spawnPosition = new Vector3(
                Random.Range(-8, 8),
                0,
                Random.Range(-8, 8)
            );

            var localPlayerPrefab = avatarPrefabRegistry.GetPrefab(user.PrefabName);
            Debug.Log($"Spawning local player at {spawnPosition} with avatar {user.PrefabName}");

            // Spawning local avatar prefab and injecting current user dependency
            var localAvatar = Instantiate(localPlayerPrefab, spawnPosition, Quaternion.identity).GetComponent<AvatarBehaviour>();
            localAvatar.Initialize(user);
        }
    }
}
