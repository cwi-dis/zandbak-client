using UnityEngine.PlayerLoop;

namespace Orchestrator.App
{
    public class Session
    {
        private Data.Session _sessionData;

        public string Id => _sessionData.sessionId;

        public Session(Data.Session sessionData)
        {
            _sessionData = sessionData;
        }

        public void Update(Data.Session sessionData)
        {
            _sessionData = sessionData;
        }
    }
}
