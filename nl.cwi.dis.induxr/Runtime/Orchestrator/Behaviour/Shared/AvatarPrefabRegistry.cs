using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    [CreateAssetMenu(fileName = "AvatarPrefabRegistry", menuName = "Networking/Avatar Prefab Registry")]
    public class AvatarPrefabRegistry : SharedObjectPrefabRegistry
    {
        [SerializeField]
        private GameObject defaultAvatar;

        public GameObject DefaultAvatar => defaultAvatar;

        public new GameObject GetPrefab(string avatarPrefabName)
        {
            if (!_isInitialized)
                Initialize();

            if (_registryCache.TryGetValue(avatarPrefabName, out var entry))
                return entry.prefab;

            Debug.LogWarning($"Prefab '{avatarPrefabName}' not found in registry, returning default");
            return defaultAvatar;
        }

    }
}
