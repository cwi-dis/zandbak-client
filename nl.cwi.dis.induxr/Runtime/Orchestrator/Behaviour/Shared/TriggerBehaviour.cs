using System;
using Newtonsoft.Json.Linq;
using Orchestrator.Data;
using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    public class TriggerBehaviour : MonoBehaviour
    {
        private string _id;
        private App.Orchestrator _orchestrator;
        private App.Trigger _triggerObject;

        /// <summary>
        /// Event raised when a new trigger payload is received
        /// </summary>
        /// <remarks>
        /// The <c>OnTriggerReceived</c> event is invoked with a <c>JObject</c> parameter containing
        /// the data for the received trigger. It enables external observers to perform custom operations
        /// in response to the trigger update event.
        /// </remarks>
        public event Action<TriggerData> OnTriggerReceived;

        private async void Start()
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            var session = _orchestrator.CurrentSession;

            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (!session.HasTrigger(_id) && session.IsAdministrator(_orchestrator.Self))
            {
                _triggerObject = await _orchestrator.CurrentSession.RegisterTrigger(gameObject, new JObject());
                Debug.Log($"Registered trigger object ${_triggerObject.Id} for owner {_triggerObject.Owner.Name} with initial value {_triggerObject.Value}");
            }
            else
            {
                Debug.Log($"Attempting to find trigger object with id {_id}");
                _triggerObject = _orchestrator.CurrentSession.FindTriggerById(_id);

                if (_triggerObject == null)
                {
                    Debug.LogWarning("No trigger object found");
                    return;
                }
            }

            _triggerObject.OnTriggerReceived += ProcessTriggerUpdate;
            _triggerObject.EnableBroadcasts();
        }

        private void OnDestroy()
        {
            _triggerObject.OnTriggerReceived -= ProcessTriggerUpdate;
            _triggerObject.DisableBroadcasts();
        }

        /// <summary>
        /// Publishes a trigger event by broadcasting the given parameter to other session participants.
        /// </summary>
        /// <param name="value">The JSON object containing the trigger data to be broadcast.</param>
        public void PublishTrigger(JObject value)
        {
            _triggerObject.BroadcastUpdate(value);
        }

        private void ProcessTriggerUpdate(TriggerData value)
        {
            Debug.Log($"New trigger received with value: {value}");
            OnTriggerReceived?.Invoke(value);
        }
    }
}
