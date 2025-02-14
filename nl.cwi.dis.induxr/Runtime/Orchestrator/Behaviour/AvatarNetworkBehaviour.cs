using UnityEngine;
using Orchestrator.Responses;

namespace Orchestrator.Behaviours
{
    public class AvatarNetworkBehaviour : NetworkBehaviour
    {
        private SkinnedMeshRenderer mesh;

        // Use this for initialization
        void Start()
        {
            Initialize();
            mesh = GetComponent<SkinnedMeshRenderer>();
        }

        public override object SendPositionData()
        {
            return new { };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            Debug.Log(data.channel + " " + data.data);
        }
    }
}
