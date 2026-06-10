using UnityEngine;

namespace Orchestrator.Attributes
{
    public class PrefabNameSelectionAttribute : PropertyAttribute
    {
        public string RegistryFieldName { get; }

        public PrefabNameSelectionAttribute(string registryFieldName)
        {
            RegistryFieldName = registryFieldName;
        }
    }
}
