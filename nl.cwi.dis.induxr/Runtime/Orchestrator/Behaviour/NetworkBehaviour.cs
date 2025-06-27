using UnityEngine;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using Newtonsoft.Json;

namespace Orchestrator.Behaviour {
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        public string id;
        public bool isLocal;
        public int updateRate = 10;

        private float _timer;

        protected void Initialize()
        {
            if (!isLocal)
            {
                Debug.Log("Listening for broadcasts");
                OrchestratorController.Instance.OnBroadcastReceivedEvent += OnBroadcastReceived;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Only send transform broadcasts if we're the local player
            if (!isLocal)
            {
                return;
            }

            _timer += Time.deltaTime;

            if (_timer >= 1f / updateRate)
            {
                _timer -= 1f / updateRate;

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
