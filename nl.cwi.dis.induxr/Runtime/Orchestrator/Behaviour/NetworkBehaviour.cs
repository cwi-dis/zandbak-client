using UnityEngine;
using Orchestrator.Responses;
using Orchestrator.Wrapping;
using Newtonsoft.Json;

namespace Orchestrator.Behaviours {
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        public string Id;
        public bool IsLocal = false;
        public int UpdateRate = 10;

        private float timer = 0;

        protected void Initialize()
        {
            if (!IsLocal)
            {
                Debug.Log("Listening for broadcasts");
                OrchestratorController.Instance.OnBroadcastReceivedEvent += OnBroadcastReceived;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Only send transform broadcasts if we're the local player
            if (!IsLocal)
            {
                return;
            }

            timer += Time.deltaTime;

            if (timer >= 1 / UpdateRate)
            {
                timer -= 1 / UpdateRate;

                var data = SendPositionData();
                Broadcast(data);
            }
        }

        private void Broadcast(object data) { 
            if (OrchestratorController.Instance.CurrentSession != null)
            {
                OrchestratorController.Instance.Broadcast("transform", JsonConvert.SerializeObject(data));
            }
        }

        public abstract object SendPositionData();
        public abstract void OnBroadcastReceived(BroadcastData data);
    }
}
