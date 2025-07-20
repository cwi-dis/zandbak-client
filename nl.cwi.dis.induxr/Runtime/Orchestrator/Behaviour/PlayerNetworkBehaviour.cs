using System;
using UnityEngine;
using Orchestrator.Data;

namespace Orchestrator.Behaviour
{
    [Obsolete]
    public class PlayerNetworkBehaviour : NetworkBehaviour
    {
        private CharacterController _controller;
        private MovementData _previousReceivedData;
        private MovementData _lastReceivedData;
        private float _lastReceiveTime;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Initialize networked object
            Initialize();
            _controller = GetComponent<CharacterController>();
        }

        public override object SendPositionData()
        {
            var position = _controller.transform.position;
            var rotation = _controller.transform.rotation;

            return new MovementData()
            {
                UserId = id,
                Timestamp = Time.time,
                Position = new PositionData()
                {
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                },
                Rotation = new RotationData()
                {
                    X = rotation.x,
                    Y = rotation.y,
                    Z = rotation.z,
                    W = rotation.w
                }
            };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            if (data.Channel == "transform")
            {
                var movement = JsonUtility.FromJson<MovementData>(data.Data);

                if (movement.UserId == id)
                {
                    _previousReceivedData = _lastReceivedData;
                    _lastReceivedData = movement;
                    _lastReceiveTime = Time.realtimeSinceStartup;

                    if (_previousReceivedData != null)
                    {
                        float t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / updateRate));

                        _controller.transform.SetPositionAndRotation(Vector3.Lerp(
                            new Vector3(_previousReceivedData.Position.X, _previousReceivedData.Position.Y, _previousReceivedData.Position.Z),
                            new Vector3(_lastReceivedData.Position.X, _lastReceivedData.Position.Y, _lastReceivedData.Position.Z),
                            t
                        ), Quaternion.Slerp(
                            new Quaternion(_previousReceivedData.Rotation.X, _previousReceivedData.Rotation.Y, _previousReceivedData.Rotation.Z, _previousReceivedData.Rotation.W),
                            new Quaternion(_lastReceivedData.Rotation.X, _lastReceivedData.Rotation.Y, _lastReceivedData.Rotation.Z, _lastReceivedData.Rotation.W),
                            t
                        ));
                    }
                }
            }
        }
    }
}
