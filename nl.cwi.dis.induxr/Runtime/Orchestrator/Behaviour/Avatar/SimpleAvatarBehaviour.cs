using System.Collections.Generic;
using Orchestrator.App;
using Orchestrator.Data;
using UnityEngine;
using User = Orchestrator.App.User;

namespace Orchestrator.Behaviour.Avatar
{
    public class SimpleAvatarBehaviour : AvatarBehaviour
    {
        [Header("General Options")]
        [Tooltip("A reference to the object to be used as notification icon. If given, the notification icon will be shown when the user's hand is raised.")]
        public GameObject notification;
        [Tooltip("A reference to a text object to be used for displaying the user's username.")]
        public TextMesh usernamePlaque;

        [Header("Local Options")]
        [Tooltip("How many times a second the pose data should be broadcast to the server.")]
        public int updateRate = 10;

        [Header("Remote Options (Smoothing)")]
        public bool withSmoothing = false;
        public int linearInterpolationRate = 10;

        private const string TransformBoneName = "root";

        private User _user;
        private bool _isLocal;

        // Local state
        private float _updateTimer;

        // Remote state
        private AvatarMovementData _previousReceivedData;
        private AvatarMovementData _lastReceivedData;
        private float _lastReceiveTime;

        public override void Initialize(User user)
        {
            _user = user;
            _user.Avatar = gameObject;
            _isLocal = user is SelfUser;
        }

        private void Start()
        {
            if (_user == null)
            {
                Debug.LogError("User is null. Make sure to call Initialize()");
                return;
            }

            if (usernamePlaque != null)
            {
                usernamePlaque.text = _user.Name;
            }

            if (notification != null)
            {
                _user.OnHandRaised += (isRaised) => notification.SetActive(isRaised);
            }

            if (_isLocal)
            {
                // hide username plaque for the local user
                Debug.Log("Hiding username plaque for local user");
                usernamePlaque?.gameObject.SetActive(false);
            }
            else
            {
                // Remote specific setup
                _lastReceiveTime = Time.realtimeSinceStartup;

                if (_user.Transform != null)
                {
                    UpdatePosition(_user.Transform);
                }

                _user.OnAvatarMovementReceived += PositionReceived;
                _user.OnIsSpeaking += (isSpeaking) => Debug.Log($"{_user.Name} is speaking: {isSpeaking}");

                // Disable all other behaviours for remote avatars
                foreach (var comp in GetComponents<UnityEngine.Behaviour>())
                {
                    if (comp != this) comp.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (_user == null) return;

            if (_isLocal)
            {
                BroadcastPosition();
            }
        }

        private void BroadcastPosition()
        {
            _updateTimer += Time.deltaTime;

            if (_updateTimer >= 1f / updateRate)
            {
                _updateTimer -= 1f / updateRate;
                var data = GetPositionData();
                ((SelfUser)_user).BroadcastAvatarMovement(data);
            }
        }

        private AvatarMovementData GetPositionData()
        {
            var positionData = new Dictionary<string, BoneData>
            {
                {
                    TransformBoneName,
                    new BoneData
                    {
                        Pos = new PositionData { X = transform.position.x, Y = transform.position.y, Z = transform.position.z },
                        Rot = new RotationData { X = transform.rotation.x, Y = transform.rotation.y, Z = transform.rotation.z, W = transform.rotation.w }
                    }
                }
            };

            return new AvatarMovementData {
                UserId = _user.Id,
                Timestamp = Time.time,
                Transforms = positionData
            };
        }

        private void PositionReceived(AvatarMovementData movement)
        {
            if (withSmoothing)
            {
                UpdatePositionWithSmoothing(movement);
            }
            else
            {
                UpdatePosition(movement);
            }
        }

        private void UpdatePositionWithSmoothing(AvatarMovementData movement)
        {
            _previousReceivedData = _lastReceivedData;
            _lastReceivedData = movement;

            if (_previousReceivedData == null) return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));
            _lastReceiveTime = Time.realtimeSinceStartup;

            if (_lastReceivedData.Transforms.TryGetValue(TransformBoneName, out var lastFoundBone) && _previousReceivedData.Transforms.TryGetValue(TransformBoneName, out var prevFoundBone))
            {
                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        new Vector3(prevFoundBone.Pos.X, prevFoundBone.Pos.Y, prevFoundBone.Pos.Z),
                        new Vector3(lastFoundBone.Pos.X, lastFoundBone.Pos.Y, lastFoundBone.Pos.Z),
                        t
                    ),
                    Quaternion.Slerp(
                        new Quaternion(prevFoundBone.Rot.X, prevFoundBone.Rot.Y, prevFoundBone.Rot.Z, prevFoundBone.Rot.W),
                        new Quaternion(lastFoundBone.Rot.X, lastFoundBone.Rot.Y, lastFoundBone.Rot.Z, lastFoundBone.Rot.W),
                        t
                    )
                );
            }
        }

        private void UpdatePosition(AvatarMovementData movement)
        {
            if (movement.Transforms.TryGetValue(TransformBoneName, out var foundBone)) {
                transform.SetPositionAndRotation(new Vector3(
                    foundBone.Pos.X,
                    foundBone.Pos.Y,
                    foundBone.Pos.Z
                ), new Quaternion(
                    foundBone.Rot.X,
                    foundBone.Rot.Y,
                    foundBone.Rot.Z,
                    foundBone.Rot.W
                ));
            }
        }
    }
}
