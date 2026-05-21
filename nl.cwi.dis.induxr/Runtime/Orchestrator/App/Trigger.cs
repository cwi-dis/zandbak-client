using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchestrator.Data;

namespace Orchestrator.App
{
    public class Trigger
    {
        private readonly Orchestrator _orchestrator;
        private Data.Trigger _triggerData;

        public Session Session => _orchestrator.CurrentSession;

        private bool _broadcastsEnabled = false;

        public Data.Trigger TriggerData
        {
            set => _triggerData = value;
        }

        public string Id => _triggerData.Id;
        public User Owner => Session.Users.Find((u) => u.Id == _triggerData.Owner.Id);
        public JObject Value => _triggerData.Value;

        public Action<JObject> OnTriggerReceived;

        public Trigger(Orchestrator orchestrator, Data.Trigger triggerData)
        {
            _orchestrator = orchestrator;
            _triggerData = triggerData;
        }

        public bool IsOwner(User user) => _triggerData.Owner.Id == user.Id;

        public void EnableBroadcasts()
        {
            _broadcastsEnabled = true;
            _orchestrator.CurrentSession.OnBroadcastDataReceived += BroadcastReceived;
        }

        public void DisableBroadcasts()
        {
            _broadcastsEnabled = false;
            _orchestrator.CurrentSession.OnBroadcastDataReceived -= BroadcastReceived;
        }

        /// <summary>
        /// Broadcasts transform data to all users in the current session.
        /// </summary>
        /// <param name="data">The movement data of the avatar, including user ID, bone data, and timestamp.</param>
        public void BroadcastUpdate(JObject data)
        {
            if (!_broadcastsEnabled) return;

            _orchestrator.CurrentSession?.BroadcastTransform("trigger", data);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            if (!_broadcastsEnabled) return;
            if (data.Channel != "objectTransform") return;

            var triggerData = JsonConvert.DeserializeObject<JObject>(data.Data);

            if (triggerData.Value<string>("id") != Id) return;
            OnTriggerReceived?.Invoke(triggerData);
        }
    }
}
