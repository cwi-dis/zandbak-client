using UnityEngine;

public class MoveSharedObject : MonoBehaviour
{
    [SerializeField]
    public float speed = 10;

    [SerializeField]
    public float minRange = -9;
    [SerializeField]
    public float maxRange = 9;

    private Vector3 _direction = Vector3.right;

    // Update is called once per frame
    private void Update()
    {
        var xPos = gameObject.transform.position.x;

        if (xPos < minRange)
        {
            _direction = Vector3.right;
        }
        else if (xPos > maxRange)
        {
            _direction = Vector3.left;
        }

        gameObject.transform.Translate(_direction * Time.deltaTime * speed);
    }
}
