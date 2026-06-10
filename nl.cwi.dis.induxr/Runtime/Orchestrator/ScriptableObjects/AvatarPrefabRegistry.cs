using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator.ScriptableObjects
{
    [CreateAssetMenu(fileName = "AvatarPrefabRegistry", menuName = "Networking/Avatar Prefab Registry")]
    public class AvatarPrefabRegistry : SharedObjectPrefabRegistry
    {
        [SerializeField]
        private GameObject defaultAvatar;

        public GameObject DefaultAvatar => defaultAvatar;

        public override GameObject GetPrefab(string prefabName)
        {
            if (!_isInitialized)
                Initialize();

            if (_registryCache.TryGetValue(prefabName, out var entry))
                return entry.prefab;

            Debug.LogWarning($"Prefab '{prefabName}' not found in registry, returning default");
            return defaultAvatar;
        }

    }
}
