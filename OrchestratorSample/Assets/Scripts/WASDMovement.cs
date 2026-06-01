using UnityEngine;

public class WASDMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime;
        transform.Translate(movement, Space.Self);
    }
}
