using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.App
{
    public class SharedObject
    {
        private readonly Orchestrator _orchestrator;
        private Data.SharedObject _sharedObjectData;

        public Session Session => _orchestrator.CurrentSession;

        private bool _broadcastsEnabled = false;

        public Data.SharedObject SharedObjectData
        {
            set => _sharedObjectData = value;
        }

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
        }

        public bool IsOwner(User user) => _sharedObjectData.Owner.Id == user.Id;

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
        /// Attempts to claim ownership of the shared object for the current user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result indicates whether ownership was successfully claimed.</returns>
        public Task<bool> ClaimOwnership()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.ClaimObjectOwnership(Id, (response, result) =>
            {
                if (response.Error == 0)
                {
                    _sharedObjectData = result;
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetResult(false);
                }
            });

            return tcs.Task;
        }

        /// <summary>
        /// Broadcasts transform data to all users in the current session.
        /// </summary>
        /// <param name="data">The movement data of the avatar, including user ID, bone data, and timestamp.</param>
        public void BroadcastUpdate(ObjectData data)
        {
            if (!_broadcastsEnabled) return;

            _orchestrator.CurrentSession?.BroadcastTransform("objectTransform", data);
        }

        private void BroadcastReceived(BroadcastData data)
        {
            if (!_broadcastsEnabled) return;
            if (data.Channel != "objectTransform") return;

            var objectTransform = JsonConvert.DeserializeObject<ObjectData>(data.Data);

            if (objectTransform.Id != Id) return;
            OnObjectDataReceived?.Invoke(objectTransform);
        }
    }
}
