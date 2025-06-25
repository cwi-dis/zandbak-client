namespace Orchestrator.App
{
    public class User
    {
        private Data.User _userData;

        public Session Session { get; set; }
        public string Id => _userData.userId;
        public string Name => _userData.userName;

        public User(Data.User userData)
        {
            _userData = userData;
        }
    }
}
