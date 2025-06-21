namespace Orchestrator.App
{
    public class User
    {
        private Data.User _userData;
        public Session Session { get; set; } = null;

        public User(Data.User userData)
        {
            _userData = userData;
        }
    }
}
