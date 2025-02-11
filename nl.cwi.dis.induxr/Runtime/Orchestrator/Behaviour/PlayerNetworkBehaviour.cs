using System;
using UnityEngine;
using Orchestrator.Wrapping;

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
}

public class PlayerNetworkBehaviour : MonoBehaviour
{
    public string id;
    public int updateRate = 10;

    private CharacterController controller;
    private float timer = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        id = Guid.NewGuid().ToString();
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 1 / updateRate) {
            timer -= 1 / updateRate;
            SendPositionData();
            Debug.Log("Send position data");
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
            }
        };

        OrchestratorController.Instance.Broadcast("transform", JsonUtility.ToJson(data));
    }
}
