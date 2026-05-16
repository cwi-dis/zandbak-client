using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class SharedObject : MonoBehaviour
    {
        private string _id;
        private App.Orchestrator _orchestrator = OrchestratorController.Instance.Orchestrator;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        async void Start()
        {
            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (!_orchestrator.CurrentSession.IsAdministrator(_orchestrator.Self))
            {
                return;
            }

            var sharedObject = await _orchestrator.CurrentSession.RegisterSharedObject(gameObject);
            Debug.Log($"Registered shared object ${sharedObject.Id} for owner {sharedObject.Owner.Name} at position {sharedObject.Position}");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
