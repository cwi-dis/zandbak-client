using System.Collections.Generic;
using UnityEngine;

namespace Orchestrator.ScriptableObjects
{
    [CreateAssetMenu(fileName = "SharedObjectPrefabRegistry", menuName = "Networking/Shared Object Prefab Registry")]
    public class SharedObjectPrefabRegistry : ScriptableObject
    {
        [System.Serializable]
        public struct PrefabRegistryEntry
        {
            public string prefabName;
            public GameObject prefab;
        }

        [SerializeField]
        public List<PrefabRegistryEntry> registryEntries = new();

        protected readonly Dictionary<string, PrefabRegistryEntry> _registryCache = new();
        protected bool _isInitialized = false;

        public void Initialize()
        {
            if (_isInitialized) return;
            _registryCache.Clear();

            foreach (var entry in registryEntries)
            {
                if (entry.prefab == null)
                    continue;

                if (string.IsNullOrEmpty(entry.prefabName))
                {
                    Debug.LogWarning($"Prefab registry entry with null or empty name: {entry.prefab}");
                    continue;
                }

                if (_registryCache.ContainsKey(entry.prefabName))
                {
                    Debug.LogError("Duplicate prefab registry entry for name: {entry.prefabName}");
                    continue;
                }

                _registryCache[entry.prefabName] = entry;
            }

            _isInitialized = true;
        }

        public GameObject GetPrefab(string prefabName)
        {
            if (!_isInitialized)
                Initialize();

            if (_registryCache.TryGetValue(prefabName, out var entry))
                return entry.prefab;

            Debug.LogWarning($"Prefab not found in registry: {prefabName}");
            return null;
        }
    }
}
