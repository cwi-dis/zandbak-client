using System;
using Newtonsoft.Json.Linq;
using Orchestrator.Behaviour.Shared;
using Orchestrator.Data;
using UnityEngine;

public class RunIntoMe : MonoBehaviour
{
    private TriggerBehaviour _triggerBehaviour;
    private int counter = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _triggerBehaviour = GetComponent<TriggerBehaviour>();
        _triggerBehaviour.OnTriggerReceived += TriggerReceived;
    }

    private void TriggerReceived(TriggerData data)
    {
        counter = data.Value.Value<int>("counter");
        Debug.Log($"Trigger received {counter}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");

        var data = new JObject { { "counter", counter + 1 } };
        _triggerBehaviour.PublishTrigger(data);
    }
}
