using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class SharedObject : MonoBehaviour
    {
        [Tooltip("How many times a second the pose data should be broadcast to the server.")]
        public int updateRate = 10;

        private string _id;
        private App.Orchestrator _orchestrator = OrchestratorController.Instance.Orchestrator;
        private App.SharedObject _sharedObject;

        private Rigidbody _rb;
        private float _timer;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private async void Start()
        {
            _rb = GetComponent<Rigidbody>();

            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (_orchestrator.CurrentSession.IsAdministrator(_orchestrator.Self))
            {
                _sharedObject = await _orchestrator.CurrentSession.RegisterSharedObject(gameObject);
                Debug.Log($"Registered shared object ${_sharedObject.Id} for owner {_sharedObject.Owner.Name} at position {_sharedObject.Position}");
            }
            else
            {
                if (_rb) _rb.isKinematic = true;

                _sharedObject = _orchestrator.CurrentSession.FindSharedObjectById(_id);
                _sharedObject.OnObjectDataReceived += ProcessObjectUpdate;
            }

            _sharedObject.EnableBroadcasts();
        }

        private void OnDestroy()
        {
            _sharedObject.DisableBroadcasts();
        }

        // Update is called once per frame
        private void Update()
        {
            _timer += Time.deltaTime;

            // Only send the transform update if the interval given by updateRate has elapsed
            if (!(_timer >= 1f / updateRate)) return;
            _timer -= 1f / updateRate;

            // Only transmit updates if the current user owns the object
            if (!_sharedObject.IsOwner(_orchestrator.Self))
            {
                // Disable rigidbody and return
                if (_rb) _rb.isKinematic = true;
                return;
            }

            var position = gameObject.transform.position;
            var rotation = gameObject.transform.rotation;

            // Broadcast transform updates to the current session
            _sharedObject.BroadcastUpdate(new ObjectData
            {
                Id = _id,
                Timestamp = Time.time,
                Position = new PositionData { X = position.x, Y = position.y, Z = position.z },
                Rotation = new RotationData { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w },
            });
        }

        /// <summary>
        /// Attempts to claim ownership of the shared object for the current user.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result indicates whether the ownership of the object
        /// was successfully claimed or not.
        /// </returns>
        public Task<bool> ClaimObject()
        {
            return _sharedObject.ClaimOwnership();
        }

        private void ProcessObjectUpdate(ObjectData objectData)
        {
            // Ignore broadcasts if the current user is the owner of the object
            if (_sharedObject.IsOwner(_orchestrator.Self)) return;

            var position = new Vector3
            {
                x = objectData.Position.X,
                y = objectData.Position.X,
                z = objectData.Position.X
            };

            var rotation = new Quaternion
            {
                x = objectData.Rotation.X,
                y = objectData.Rotation.Y,
                z = objectData.Rotation.Z,
                w = objectData.Rotation.W
            };

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
