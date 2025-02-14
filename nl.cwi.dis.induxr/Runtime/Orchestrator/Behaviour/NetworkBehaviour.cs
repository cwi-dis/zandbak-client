using Orchestrator.Responses;

namespace Orchestrator.Behaviours {
    public interface INetworkBehaviour
    {
        void SendPositionData();
        void OnBroadcastReceived(BroadcastData data);
    }
}
