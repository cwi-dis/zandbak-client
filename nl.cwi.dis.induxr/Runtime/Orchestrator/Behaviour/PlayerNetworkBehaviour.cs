using UnityEngine;
using Orchestrator.Data;

namespace Orchestrator.Behaviour
{
    [System.Serializable]
    public class MovementData
    {
        public string userId;
        public PositionData position;
        public RotationData rotation;
        public float timestamp;
    }

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class RotationData
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

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
                userId = id,
                timestamp = Time.time,
                position = new PositionData()
                {
                    x = position.x,
                    y = position.y,
                    z = position.z,
                },
                rotation = new RotationData()
                {
                    x = rotation.x,
                    y = rotation.y,
                    z = rotation.z,
                    w = rotation.w
                }
            };
        }

        public override void OnBroadcastReceived(BroadcastData data)
        {
            if (data.channel == "transform")
            {
                var movement = JsonUtility.FromJson<MovementData>(data.data);

                if (movement.userId == id)
                {
                    _previousReceivedData = _lastReceivedData;
                    _lastReceivedData = movement;
                    _lastReceiveTime = Time.realtimeSinceStartup;

                    if (_previousReceivedData != null)
                    {
                        float t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / updateRate));

                        _controller.transform.SetPositionAndRotation(Vector3.Lerp(
                            new Vector3(_previousReceivedData.position.x, _previousReceivedData.position.y, _previousReceivedData.position.z),
                            new Vector3(_lastReceivedData.position.x, _lastReceivedData.position.y, _lastReceivedData.position.z),
                            t
                        ), Quaternion.Slerp(
                            new Quaternion(_previousReceivedData.rotation.x, _previousReceivedData.rotation.y, _previousReceivedData.rotation.z, _previousReceivedData.rotation.w),
                            new Quaternion(_lastReceivedData.rotation.x, _lastReceivedData.rotation.y, _lastReceivedData.rotation.z, _lastReceivedData.rotation.w),
                            t
                        ));
                    }
                }
            }
        }
    }
}
