using Orchestrator.Wrapping;
using Orchestrator.App;
using Orchestrator.ScriptableObjects;
using UnityEngine;

public class SpawnOnButtonPress : MonoBehaviour
{
    private Orchestrator.App.Orchestrator _orchestrator;
    private Session _session;

    [SerializeField] public SharedObjectPrefabRegistry sharedObjectPrefabRegistry;
    [SerializeField] public string prefabName = "tiltedCube";
    [SerializeField] public KeyCode spawnKey = KeyCode.B;
    [SerializeField] public float spawnOffset = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _orchestrator = OrchestratorController.Instance.Orchestrator;
        _session = _orchestrator.CurrentSession;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!Input.GetKeyDown(spawnKey))
            return;

        if (!sharedObjectPrefabRegistry.HasPrefab(prefabName))
        {
            Debug.LogWarning($"Prefab '{prefabName}' not found in shared object prefab registry.");
            return;
        }

        var spawnPosition = transform.position + transform.forward * spawnOffset;
        spawnPosition.y = 1;

        Debug.Log($"Spawning shared object at {spawnPosition} with rotation {transform.rotation}");

        _session.SpawnSharedObject(prefabName, spawnPosition, Quaternion.identity);
    }
}
