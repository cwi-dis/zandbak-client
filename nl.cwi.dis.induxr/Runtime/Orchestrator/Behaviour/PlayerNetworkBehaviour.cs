using System;
using UnityEngine;
using Orchestrator.Wrapping;
using Orchestrator.Responses;

namespace Orchestrator.Behaviours
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
        private CharacterController controller;
        private MovementData previousReceivedData;
        private MovementData lastReceivedData;
        private float lastReceiveTime;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            // Initialize networked object
            Initialize();
            controller = GetComponent<CharacterController>();
        }

        public override object SendPositionData()
        {
            var position = controller.transform.position;
            var rotation = controller.transform.rotation;

            return new MovementData()
            {
                userId = Id,
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

                if (movement.userId == Id)
                {
                    previousReceivedData = lastReceivedData;
                    lastReceivedData = movement;
                    lastReceiveTime = Time.realtimeSinceStartup;

                    if (previousReceivedData != null)
                    {
                        float t = Mathf.Clamp01((Time.realtimeSinceStartup - lastReceiveTime) / (1.0f / UpdateRate));

                        controller.transform.SetPositionAndRotation(Vector3.Lerp(
                            new Vector3(previousReceivedData.position.x, previousReceivedData.position.y, previousReceivedData.position.z),
                            new Vector3(lastReceivedData.position.x, lastReceivedData.position.y, lastReceivedData.position.z),
                            t
                        ), Quaternion.Slerp(
                            new Quaternion(previousReceivedData.rotation.x, previousReceivedData.rotation.y, previousReceivedData.rotation.z, previousReceivedData.rotation.w),
                            new Quaternion(lastReceivedData.rotation.x, lastReceivedData.rotation.y, lastReceivedData.rotation.z, lastReceivedData.rotation.w),
                            t
                        ));
                    }
                }
            }
        }
    }
}
