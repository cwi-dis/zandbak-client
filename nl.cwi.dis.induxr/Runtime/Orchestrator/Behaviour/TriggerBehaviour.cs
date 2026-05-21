using Newtonsoft.Json.Linq;
using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class TriggerBehaviour : MonoBehaviour
    {
        private string _id;
        private readonly App.Orchestrator _orchestrator = OrchestratorController.Instance.Orchestrator;
        private App.Trigger _triggerObject;

        private async void Start()
        {
            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (_orchestrator.CurrentSession.IsAdministrator(_orchestrator.Self))
            {
                _triggerObject = await _orchestrator.CurrentSession.RegisterTrigger(gameObject, new JObject());
                Debug.Log($"Registered shared object ${_triggerObject.Id} for owner {_triggerObject.Owner.Name} with initial value {_triggerObject.Value}");
            }
            else
            {
                _triggerObject = _orchestrator.CurrentSession.FindTriggerById(_id);
            }

            _triggerObject.OnTriggerReceived += ProcessTriggerUpdate;
            _triggerObject.EnableBroadcasts();
        }

        private void OnDestroy()
        {
            _triggerObject.OnTriggerReceived -= ProcessTriggerUpdate;
        }

        public void PublishTrigger(JObject value)
        {
            _triggerObject.BroadcastUpdate(value);
        }

        private void ProcessTriggerUpdate(JObject value)
        {
            Debug.Log($"New trigger received with value: {value}");
        }
    }
}
