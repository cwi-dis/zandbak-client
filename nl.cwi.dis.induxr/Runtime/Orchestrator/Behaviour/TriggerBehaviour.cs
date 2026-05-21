using System;
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

        /// <summary>
        /// Event raised when a new trigger payload is received
        /// </summary>
        /// <remarks>
        /// The <c>OnTriggerReceived</c> event is invoked with a <c>JObject</c> parameter containing
        /// the data for the received trigger. It enables external observers to perform custom operations
        /// in response to the trigger update event.
        /// </remarks>
        public event Action<JObject> OnTriggerReceived;

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

        /// <summary>
        /// Publishes a trigger event by broadcasting the given parameter to other session participants.
        /// </summary>
        /// <param name="value">The JSON object containing the trigger data to be broadcast.</param>
        public void PublishTrigger(JObject value)
        {
            _triggerObject.BroadcastUpdate(value);
        }

        private void ProcessTriggerUpdate(JObject value)
        {
            Debug.Log($"New trigger received with value: {value}");
            OnTriggerReceived?.Invoke(value);
        }
    }
}
