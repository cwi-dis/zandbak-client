using UnityEngine;

public class WASDMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;

    // Update is called once per frame
    void Update()
    {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }
    }
}
