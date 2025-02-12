using System;
using UnityEngine;
using Orchestrator.Wrapping;
using Orchestrator.Responses;

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

public class PlayerNetworkBehaviour : MonoBehaviour
{
    public string id;
    public bool isLocal = false;
    public int updateRate = 10;

    private CharacterController controller;
    private float timer = 0;

    private MovementData PreviousReceivedData;
    private MovementData LastReceivedData;
    private float LastReceiveTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        id = Guid.NewGuid().ToString();
        controller = GetComponent<CharacterController>();

        if (!isLocal) {
            Debug.Log("Listening for broadcasts");
            OrchestratorController.Instance.OnBroadcastReceivedEvent += OnBroadcastReceived;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Only send transform broadcasts if we're the local player
        if (!isLocal) {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= 1 / updateRate) {
            timer -= 1 / updateRate;
            SendPositionData();
        }

    }

    void SendPositionData() {
        var position = controller.transform.position;
        var rotation = controller.transform.rotation;

        var data = new MovementData()
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

        if (OrchestratorController.Instance.CurrentSession != null) {
          OrchestratorController.Instance.Broadcast("transform", JsonUtility.ToJson(data));
        }
    }

    void OnBroadcastReceived(BroadcastData data) {
        if (data.channel == "transform") {
            var movement = JsonUtility.FromJson<MovementData>(data.data);

            if (movement.userId == id) {
                PreviousReceivedData = LastReceivedData;
                LastReceivedData = movement;
                LastReceiveTime = Time.realtimeSinceStartup;

                if (PreviousReceivedData != null) {
                    float t = Mathf.Clamp01((Time.realtimeSinceStartup - LastReceiveTime) / (1.0f / updateRate));

                    controller.transform.SetPositionAndRotation(Vector3.Lerp(
                        new Vector3(PreviousReceivedData.position.x, PreviousReceivedData.position.y, PreviousReceivedData.position.z),
                        new Vector3(LastReceivedData.position.x, LastReceivedData.position.y, LastReceivedData.position.z),
                        t
                    ), Quaternion.Slerp(
                        new Quaternion(PreviousReceivedData.rotation.x, PreviousReceivedData.rotation.y, PreviousReceivedData.rotation.z, PreviousReceivedData.rotation.w),
                        new Quaternion(LastReceivedData.rotation.x, LastReceivedData.rotation.y, LastReceivedData.rotation.z, LastReceivedData.rotation.w),
                        t
                    ));
                }
            }
        }
    }
}
