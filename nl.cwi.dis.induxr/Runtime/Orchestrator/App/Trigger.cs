using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.App
{
    public class Trigger
    {
        private readonly Orchestrator _orchestrator;
        private Data.Trigger _triggerData;
        private bool _broadcastsEnabled = false;

        public Data.Trigger TriggerData
        {
            set => _triggerData = value;
        }

        public string Id => _triggerData.Id;
        public User Owner => Session.Users.Find((u) => u.Id == _triggerData.Owner.Id);
        public JObject Value => _triggerData.Value;
        public Session Session => _orchestrator.CurrentSession;

        public Action<TriggerData> OnTriggerReceived;

        public Trigger(Orchestrator orchestrator, Data.Trigger triggerData)
        {
            _orchestrator = orchestrator;
            _triggerData = triggerData;
        }

        /// <summary>
        /// Determines if the specified user is the owner of the trigger object.
        /// </summary>
        /// <param name="user">The user to check ownership against.</param>
        /// <returns>True if the specified user is the owner of the trigger object; otherwise, false.</returns>
        public bool IsOwner(User user) => _triggerData.Owner.Id == user.Id;

        /// <summary>
        /// Enables broadcasting of updates related to the trigger object within the current session.
        /// </summary>
        public void EnableBroadcasts()
        {
            _broadcastsEnabled = true;
            _orchestrator.CurrentSession.OnBroadcastDataReceived += BroadcastReceived;
        }

        /// <summary>
        /// Disables broadcasting of updates related to the trigger object within the current session.
        /// </summary>
        public void DisableBroadcasts()
        {
            _broadcastsEnabled = false;
            _orchestrator.CurrentSession.OnBroadcastDataReceived -= BroadcastReceived;
        }

        /// <summary>
        /// Broadcasts trigger updates to all users in the current session.
        /// </summary>
        /// <param name="data">The trigger data to be broadcast to the session.</param>
        public void BroadcastUpdate(JObject data)
        {
            if (!_broadcastsEnabled) return;

            _orchestrator.CurrentSession?.BroadcastData("trigger", new TriggerData
            {
                Id = Id,
                Timestamp = Time.time,
                Value = data
            }, true);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            if (!_broadcastsEnabled) return;
            if (data.Channel != "trigger") return;

            var triggerData = JsonConvert.DeserializeObject<TriggerData>(data.Data);
            if (triggerData.Id != Id) return;

            _triggerData.Value = triggerData.Value;
            OnTriggerReceived?.Invoke(triggerData);
        }
    }
}
