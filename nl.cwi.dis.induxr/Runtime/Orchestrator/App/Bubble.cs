using System.Collections.Generic;
using System.Linq;

namespace Orchestrator.App
{
    public class Bubble
    {
        private readonly Orchestrator _orchestrator;
        private readonly Data.Bubble _bubbleData;

        public string Id => _bubbleData.Id;
        public string Name => _bubbleData.Name;
        public User Owner => Session.Users.Find((u) => u.Id == _bubbleData.Owner.Id);
        public List<User> Users => _bubbleData.Users.Select((bubbleUser) => Session.Users.Find((sessionUser) => sessionUser.Id == bubbleUser.Id)).ToList();

        public Session Session => _orchestrator.CurrentSession;

        public Bubble(Orchestrator orchestrator, Data.Bubble bubbleData)
        {
            _orchestrator = orchestrator;
            _bubbleData = bubbleData;
        }
    }
}
