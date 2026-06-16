using System.Collections;
using Orchestrator.App;
using Orchestrator.Behaviour.Shared;
using Orchestrator.Wrapping;
using UnityEngine;

public class DespawnAfterTime : MonoBehaviour
{
    [SerializeField]
    public float secondsUntilDespawn = 10;

    private SharedObject _sharedObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        var behaviour = GetComponent<SharedObjectBehaviour>();
        if (!behaviour)
        {
            Debug.LogWarning("GameObject is missing SharedObjectBehaviour component.");
            return;
        }

        _sharedObject = behaviour.SharedObject;
        StartCoroutine(DespawnObject());
    }

    private IEnumerator DespawnObject()
    {
        yield return new WaitForSeconds(secondsUntilDespawn);

        Debug.Log($"Despawning object with ID: {_sharedObject.Id}");
        _sharedObject.Destroy();
    }
}
