namespace Orchestrator.App
{
    public class Session
    {
        private readonly Data.Session _sessionData;
        
        public string Id => _sessionData.sessionId;

        public Session(Data.Session sessionData)
        {
            _sessionData = sessionData;
        }
    }
}