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
        public List<PrefabRegistryEntry> registryEntries;

        protected readonly Dictionary<string, PrefabRegistryEntry> RegistryCache = new();
        protected bool IsInitialized = false;

        private void OnEnable()
        {
            IsInitialized = false;
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            RegistryCache.Clear();

            foreach (var entry in registryEntries)
            {
                if (entry.prefab == null)
                {
                    if (!string.IsNullOrEmpty(entry.prefabName))
                    {
                        Debug.LogWarning($"Prefab registry entry '{entry.prefabName}' has no prefab assigned. Skipping.");
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(entry.prefabName))
                {
                    Debug.LogWarning($"Prefab registry entry with null or empty name: {entry.prefab}");
                    continue;
                }

                if (RegistryCache.ContainsKey(entry.prefabName))
                {
                    Debug.LogError($"Duplicate prefab registry entry for name: {entry.prefabName}");
                    continue;
                }

                RegistryCache[entry.prefabName] = entry;
            }

            IsInitialized = true;
        }

        public virtual GameObject GetPrefab(string prefabName)
        {
            if (!IsInitialized)
                Initialize();

            if (RegistryCache.TryGetValue(prefabName, out var entry))
                return entry.prefab;

            Debug.LogWarning($"Prefab not found in registry: {prefabName}");
            return null;
        }

        public virtual bool GetPrefab(string prefabName, out GameObject prefab)
        {
            prefab = GetPrefab(prefabName);
            return prefab is not null;
        }

        public virtual bool HasPrefab(string prefabName)
        {
            if (!IsInitialized)
                Initialize();

            return RegistryCache.ContainsKey(prefabName);
        }

        public IEnumerable<string> GetPrefabNames()
        {
            if (!IsInitialized)
                Initialize();
            return RegistryCache.Keys;
        }
    }
}
