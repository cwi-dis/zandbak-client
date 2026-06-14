using Orchestrator.Wrapping;
using Orchestrator.App;
using UnityEngine;

public class SpawnOnButtonPress : MonoBehaviour
{
    private Orchestrator.App.Orchestrator _orchestrator;
    private Session _session;

    [SerializeField] private KeyCode spawnKey = KeyCode.B;
    [SerializeField] private float spawnOffset = 2.0f;

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
            var spawnPosition = transform.position + transform.forward * spawnOffset;
            spawnPosition.y = 1;

            Debug.Log($"Spawning shared object at {spawnPosition} with rotation {transform.rotation}");

            _session.SpawnSharedObject("tiltedCube", spawnPosition, Quaternion.identity);
        }
    }
}
