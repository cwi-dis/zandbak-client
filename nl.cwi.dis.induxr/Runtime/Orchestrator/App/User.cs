namespace Orchestrator.App
{
    public class User
    {
        private Data.User _userData;

        public Session Session { get; set; }
        public string Id => _userData.Id;
        public string Name => _userData.Username;
        public bool IsSpeaking => _userData.IsSpeaking;

        public User(Data.User userData)
        {
            _userData = userData;
        }
    }
}
