using System.Threading.Tasks;
using Orchestrator.Data;
using Orchestrator.Util;
using Orchestrator.Wrapping;
using UnityEngine;

namespace Orchestrator.Behaviour.Shared
{
    /// <summary>
    /// Represents a behaviour associated with a shared object in a collaborative environment.
    /// </summary>
    /// <remarks>
    /// This class is designed to manage ownership and updates for a shared object in a multi-user system.
    /// It facilitates the broadcasting and interpolation of pose data based on defined update rates.
    /// </remarks>
    public class SharedObjectBehaviour : MonoBehaviour
    {
        [Tooltip("How many times a second the pose data should be broadcast to the server.")]
        public int updateRate = 10;
        [Tooltip("Rate (in Hz) at which received pose updates are interpolated. Should roughly match the sender's updateRate.")]
        public int linearInterpolationRate = 5;

        private string _id;
        private App.Orchestrator _orchestrator;
        private App.SharedObject _sharedObject;

        private Rigidbody _rb;
        private float _timer;

        private ObjectData _lastReceivedData;
        private float _lastReceiveTime;
        private Vector3 _interpStartPos;
        private Quaternion _interpStartRot;

        private async void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _orchestrator = OrchestratorController.Instance.Orchestrator;
            _id = StableObjectId.GetSceneObjectId(gameObject);
            Debug.Log($"Generated object id: {_id} for gameObject {gameObject.name}");

            if (_orchestrator.CurrentSession.IsAdministrator(_orchestrator.Self))
            {
                _sharedObject = await _orchestrator.CurrentSession.RegisterSharedObject(gameObject);
                Debug.Log($"Registered shared object ${_sharedObject.Id} for owner {_sharedObject.Owner.Name} at position {_sharedObject.Position}");
            }
            else
            {
                // If the object has a Rigidbody component, disable it so it isn't affected by physics
                if (_rb) _rb.isKinematic = true;
                _sharedObject = _orchestrator.CurrentSession.FindSharedObjectById(_id);
            }

            _sharedObject.OnObjectDataReceived += ProcessObjectUpdate;
            _sharedObject.EnableBroadcasts();
        }

        private void OnDestroy()
        {
            _sharedObject.DisableBroadcasts();
        }

        // Update is called once per frame
        private void Update()
        {
            // Only transmit updates if the current user owns the object
            if (_sharedObject.IsOwner(_orchestrator.Self))
            {
                BroadcastObjectUpdate();
            }
            else
            {
                // If the object has a Rigidbody component, disable it so it isn't affected by physics while it's not owned
                if (_rb) _rb.isKinematic = true;
                if (_lastReceivedData == null) return;

                // t advances from 0 to 1 over the expected interval between updates
                var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) * linearInterpolationRate);

                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        _interpStartPos,
                        new Vector3(_lastReceivedData.Position.X, _lastReceivedData.Position.Y, _lastReceivedData.Position.Z),
                        t
                    ),
                    Quaternion.Slerp(
                        _interpStartRot,
                        new Quaternion(_lastReceivedData.Rotation.X, _lastReceivedData.Rotation.Y, _lastReceivedData.Rotation.Z, _lastReceivedData.Rotation.W),
                        t
                    )
                );
            }
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

        private void BroadcastObjectUpdate()
        {
            _timer += Time.deltaTime;

            // Only send the transform update if the interval given by updateRate has elapsed
            if (!(_timer >= 1f / updateRate)) return;
            _timer -= 1f / updateRate;

            // Get position and rotation
            var position = gameObject.transform.position;
            var rotation = gameObject.transform.rotation;

            // Broadcast transform updates to the current session
            _sharedObject.BroadcastUpdate(
                new PositionData { X = position.x, Y = position.y, Z = position.z },
                new RotationData { X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w }
            );
        }

        private void ProcessObjectUpdate(ObjectData objectData)
        {
            // Ignore broadcasts if the current user is the owner of the object
            if (_sharedObject.IsOwner(_orchestrator.Self)) return;

            // Snapshot current visible pose so the next interpolation segment starts
            // from where the object actually is, not from the previously received target.
            // This avoids visible snaps when packets arrive earlier or later than expected.
            _interpStartPos = transform.position;
            _interpStartRot = transform.rotation;

            _lastReceivedData = objectData;
            _lastReceiveTime = Time.realtimeSinceStartup;
        }
    }
}
