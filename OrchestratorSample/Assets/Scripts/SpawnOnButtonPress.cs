using Orchestrator.Wrapping;
using Orchestrator.App;
using UnityEngine;

public class SpawnOnButtonPress : MonoBehaviour
{
    private Orchestrator.App.Orchestrator _orchestrator;
    private Session _session;

    [SerializeField] private KeyCode spawnKey = KeyCode.B;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _orchestrator = OrchestratorController.Instance.Orchestrator;
        _session = _orchestrator.CurrentSession;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            Debug.Log($"Spawning shared object at {transform.position} with rotation {transform.rotation}");
            _session.SpawnSharedObject("Prefabs/TiltedCubePrefab", transform.position, Quaternion.identity);
        }
    }
}
