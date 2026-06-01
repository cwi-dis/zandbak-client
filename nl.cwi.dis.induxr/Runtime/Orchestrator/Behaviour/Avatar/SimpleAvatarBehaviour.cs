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
        public int linearInterpolationRate = 5;

        private const string TransformBoneName = "root";

        private User _user;
        private bool _isLocal;

        // Local state
        private float _updateTimer;

        // Remote state
        private AvatarMovementData _lastReceivedData;
        private float _lastReceiveTime;
        private Vector3 _interpStartPos;
        private Quaternion _interpStartRot;

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
            else
            {
                ApplyPosition();
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

        private void ApplyPosition()
        {
            if (_lastReceivedData == null)
                return;

            var t = Mathf.Clamp01((Time.realtimeSinceStartup - _lastReceiveTime) / (1.0f / linearInterpolationRate));

            if (_lastReceivedData.Transforms.TryGetValue(TransformBoneName, out var lastFoundBone))
            {
                transform.SetPositionAndRotation(
                    Vector3.Lerp(
                        _interpStartPos,
                        new Vector3(lastFoundBone.Pos.X, lastFoundBone.Pos.Y, lastFoundBone.Pos.Z),
                        t
                    ),
                    Quaternion.Slerp(
                        _interpStartRot,
                        new Quaternion(lastFoundBone.Rot.X, lastFoundBone.Rot.Y, lastFoundBone.Rot.Z, lastFoundBone.Rot.W),
                        t
                    )
                );
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
            _interpStartPos = transform.position;
            _interpStartRot = transform.rotation;

            _lastReceivedData = movement;
            _lastReceiveTime = Time.realtimeSinceStartup;
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
