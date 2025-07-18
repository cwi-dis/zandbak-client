namespace Orchestrator.App
{
    public class User
    {
        private Data.User _userData;

        public Session Session { get; set; }
        public string Id => _userData.Id;
        public string Name => _userData.Username;
        /// <summary>
        /// Indicates whether the user is currently speaking
        /// </summary>
        public bool IsSpeaking => _userData.IsSpeaking;
        /// <summary>
        /// Returns the type of user (e.g. presenter, moderator, user)
        /// </summary>
        public string Type => _userData.UserType;
        /// <summary>
        /// Indicates whether the user has their hand raised currently
        /// </summary>
        public bool HasHandRaised => _userData.HasHandRaised;
        /// <summary>
        /// Returns the type of device that the user has used to connect
        /// </summary>
        public string DeviceType => _userData.DeviceType;

        public User(Data.User userData)
        {
            _userData = userData;
        }
    }
}
