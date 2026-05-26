using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Orchestrator.Data;
using Orchestrator.Wrapping;
using UnityEditor;
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

        public string PrefabName => _sharedObjectData.PrefabName;

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

        /// <summary>
        /// Determines if the specified user is the owner of the shared object.
        /// </summary>
        /// <param name="user">The user to check ownership against.</param>
        /// <returns>True if the specified user is the owner of the shared object; otherwise, false.</returns>
        public bool IsOwner(User user) => _sharedObjectData.Owner.Id == user.Id;

        /// <summary>
        /// Enables broadcasting of updates related to the shared object within the current session.
        /// </summary>
        public void EnableBroadcasts()
        {
            _broadcastsEnabled = true;
            _orchestrator.CurrentSession.OnBroadcastDataReceived += BroadcastReceived;
        }

        /// <summary>
        /// Disables broadcasting of updates related to the shared object within the current session.
        /// </summary>
        public void DisableBroadcasts()
        {
            _broadcastsEnabled = false;
            _orchestrator.CurrentSession.OnBroadcastDataReceived -= BroadcastReceived;
        }

        public Task<bool> Destroy()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.DestroySharedObject(_sharedObjectData, (status, sharedObjectData) =>
            {
                if (status.IsOk)
                {
                    Session.SharedObjects.Remove(this);
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
        /// Attempts to claim ownership of the shared object for the current user.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result indicates whether ownership was successfully claimed.</returns>
        public Task<bool> ClaimOwnership()
        {
            var tcs = new TaskCompletionSource<bool>();

            OrchestratorController.Instance.Wrapper.ClaimObjectOwnership(Id, (response, result) =>
            {
                if (response.IsOk)
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
        /// Broadcasts the updated position and rotation of the shared object to the current session.
        /// </summary>
        /// <param name="position">The new position data of the shared object.</param>
        /// <param name="rotation">The new rotation data of the shared object.</param>
        public void BroadcastUpdate(PositionData position, RotationData rotation)
        {
            if (!_broadcastsEnabled) return;

            _orchestrator.CurrentSession?.BroadcastData("objectTransform", new ObjectData
            {
                Id = Id,
                Timestamp = Time.time,
                Position = position,
                Rotation = rotation
            });
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
