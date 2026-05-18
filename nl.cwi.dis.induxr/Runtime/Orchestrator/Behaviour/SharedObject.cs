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
        [Header("Smoothing options")]
        public bool withSmoothing = true;
        public int linearInterpolationRate = 10;

        private string _id;
        private App.Orchestrator _orchestrator = OrchestratorController.Instance.Orchestrator;
        private App.SharedObject _sharedObject;

        private Rigidbody _rb;
        private float _timer;

        private ObjectData _previousReceivedData;
        private ObjectData _lastReceivedData;
        private float _lastReceiveTime;

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

            if (withSmoothing)
            {
                UpdateObjectWithSmoothing(objectData);
            }
            else
            {
                UpdateObject(objectData);
            }
        }

        private void UpdateObject(ObjectData objectData)
        {
            var position = new Vector3
            {
                x = objectData.Position.X,
                y = objectData.Position.Y,
                z = objectData.Position.Z
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

        private void UpdateObjectWithSmoothing(ObjectData objectData)
        {
            // Keep track of last received movement data for linear interpolation
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = objectData;

            // Do nothing on the first frame
            if (_previousReceivedData == null) return;

            // Compute the value of t used in linear interpolation
            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            transform.SetPositionAndRotation(
                Vector3.Lerp(
                    new Vector3(_previousReceivedData.Position.X, _previousReceivedData.Position.Y, _previousReceivedData.Position.Z),
                    new Vector3(_lastReceivedData.Position.X, _lastReceivedData.Position.Y, _lastReceivedData.Position.Z),
                    t
                ),
                Quaternion.Slerp(
                    new Quaternion(_previousReceivedData.Rotation.X, _previousReceivedData.Rotation.Y, _previousReceivedData.Rotation.Z, _previousReceivedData.Rotation.W),
                    new Quaternion(_lastReceivedData.Rotation.X, _lastReceivedData.Rotation.Y, _lastReceivedData.Rotation.Z, _lastReceivedData.Rotation.W),
                    t
                )
            );
        }
    }
}
