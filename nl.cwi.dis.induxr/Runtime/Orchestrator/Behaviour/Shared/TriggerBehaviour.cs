using Newtonsoft.Json.Linq;
using Orchestrator.Data;
using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;
using UnityEngine.Events;

namespace Orchestrator.Behaviour.Shared
{
    public class TriggerBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Callback invoked when the shared object is initialized and ready for use.")]
        public UnityEvent<App.Trigger> onInitialized;

        [SerializeField]
        [Tooltip("Callback invoked when a trigger value is received from the server.")]
        public UnityEvent<App.Trigger> onTriggerReceived;

        private string _id;
        private App.Orchestrator _orchestrator;
        private App.Trigger _triggerObject;

        public TriggerData Data => _triggerObject?.Data;
        public App.Trigger TriggerObject => _triggerObject;

        private async void Start()
        {
            _orchestrator = OrchestratorController.Instance.Orchestrator;
            var session = _orchestrator.CurrentSession;

            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (!session.HasTrigger(_id) && session.IsAdministrator(_orchestrator.Self))
            {
                _triggerObject = await _orchestrator.CurrentSession.RegisterTrigger(gameObject, new JObject());
                Debug.Log($"Registered trigger object ${_triggerObject.Id} for owner {_triggerObject.Owner.Name} with initial value {_triggerObject.Data}");
            }
            else
            {
                Debug.Log($"Attempting to find trigger object with id {_id}");
                _triggerObject = await _orchestrator.CurrentSession.GetTrigger(_id);

                if (_triggerObject == null)
                {
                    Debug.LogWarning("No trigger object found");
                    return;
                }
            }

            _triggerObject.OnTriggerReceived += ProcessTriggerUpdate;
            _triggerObject.EnableBroadcasts();

            onInitialized?.Invoke(_triggerObject);
        }

        private void OnDestroy()
        {
            _triggerObject.OnTriggerReceived -= ProcessTriggerUpdate;
            _triggerObject.DisableBroadcasts();
        }

        /// <summary>
        /// Publishes a trigger event by broadcasting the given parameter to other session participants.
        /// </summary>
        /// <param name="value">The data containing the trigger data to be broadcast.</param>
        public void PublishTrigger<T>(T value)
        {
            _triggerObject.BroadcastUpdate(value);
        }

        private void ProcessTriggerUpdate(TriggerData triggerData)
        {
            onTriggerReceived?.Invoke(_triggerObject);
        }
    }
}
