using System.Collections;
using Orchestrator.Behaviour.Shared;
using Orchestrator.App;
using TMPro;
using UnityEngine;

public class RunIntoMe : MonoBehaviour
{
    [SerializeField]
    public TMP_Text counterText;

    [Header("Particle System")]
    [SerializeField]
    public ParticleSystem particleEmitter;
    public float particleDuration = 1f;

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

        particleEmitter.Stop();
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
        StartCoroutine(PlayParticles());
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered");
        _triggerBehaviour.PublishTrigger(new CounterMessage { Counter = _counter + 1 });
    }

    private IEnumerator PlayParticles()
    {
        if (!particleEmitter)
            yield return null;

        particleEmitter.Play();
        yield return new WaitForSeconds(particleDuration);
        particleEmitter.Stop();
    }
}
