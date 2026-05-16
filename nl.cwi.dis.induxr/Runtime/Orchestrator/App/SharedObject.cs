using System;
using Newtonsoft.Json;
using Orchestrator.Data;
using UnityEngine;

namespace Orchestrator.App
{
    public class SharedObject
    {
        private readonly Orchestrator _orchestrator;
        private Data.SharedObject _sharedObjectData;

        public Session Session => _orchestrator.CurrentSession;

        public string Id => _sharedObjectData.Id;
        public User Owner => Session.Users.Find((u) => u.Id == _sharedObjectData.Owner.Id);

        public Action<ObjectData> OnObjectDataReceived;

        public Vector3 Position => new(
            _sharedObjectData.Transform.Position.X,
            _sharedObjectData.Transform.Position.Y,
            _sharedObjectData.Transform.Position.Z
            );

        public Quaternion Rotation => new(
            _sharedObjectData.Transform.Rotation.X,
            _sharedObjectData.Transform.Rotation.Y,
            _sharedObjectData.Transform.Rotation.Z,
            _sharedObjectData.Transform.Rotation.W
            );

        public SharedObject(Orchestrator orchestrator, Data.SharedObject sharedObjectData)
        {
            _orchestrator = orchestrator;
            _sharedObjectData = sharedObjectData;

            _orchestrator.CurrentSession.OnBroadcastDataReceived += BroadcastReceived;
        }

        public bool IsOwner(User user) => _sharedObjectData.Owner.Id == user.Id;

        /// <summary>
        /// Broadcasts transform data to all users in the current session.
        /// </summary>
        /// <param name="data">The movement data of the avatar, including user ID, bone data, and timestamp.</param>
        public void BroadcastUpdate(ObjectData data)
        {
            _orchestrator.CurrentSession?.BroadcastTransform("objectTransform", data);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            if (data.Channel != "objectTransform") return;
            var objectTransform = JsonConvert.DeserializeObject<ObjectData>(data.Data);

            if (objectTransform.Id != Id) return;
            OnObjectDataReceived?.Invoke(objectTransform);
        }
    }
}
