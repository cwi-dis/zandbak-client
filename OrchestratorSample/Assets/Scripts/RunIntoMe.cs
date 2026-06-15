using Newtonsoft.Json.Linq;
using Orchestrator.Behaviour.Shared;
using Orchestrator.App;
using TMPro;
using UnityEngine;

public class RunIntoMe : MonoBehaviour
{
    [SerializeField]
    public TMP_Text counterText;

    private TriggerBehaviour _triggerBehaviour;
    private int _counter = 0;

    private class CounterMessage
    {
        public int Counter;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _triggerBehaviour = GetComponent<TriggerBehaviour>();
        _triggerBehaviour.onTriggerReceived.AddListener(TriggerReceived);
        _triggerBehaviour.onInitialized.AddListener(OnTriggerInitialized);
    }

    private void OnTriggerInitialized(Trigger trigger)
    {
        var data = trigger.GetValue<CounterMessage>();

        if (data == null)
            return;

        _counter = data.Counter;
        Debug.Log($"Initialised counter to {_counter}");
        counterText.text = _counter.ToString();
    }

    private void TriggerReceived(Trigger trigger)
    {
        var data = trigger.GetValue<CounterMessage>();
        _counter = data.Counter;

        counterText.text = _counter.ToString();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");
        _triggerBehaviour.PublishTrigger(new CounterMessage { Counter = _counter + 1 });
    }
}
