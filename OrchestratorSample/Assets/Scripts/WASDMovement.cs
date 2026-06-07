using UnityEngine;

public class WASDMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float rotationSpeed = 10.0f;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    private void Update()
    {
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        if (!_mainCamera)
        {
            _mainCamera = Camera.main;
            if (!_mainCamera)
                return;
        }

        // Get camera-relative directions
        var cameraForward = _mainCamera.transform.forward;
        var cameraRight = _mainCamera.transform.right;

        // Flatten directions on Y axis
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate movement direction
        var moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        if (!(moveDirection.magnitude > 0.1f))
            return;

        // Only rotate if we are not moving backwards (S key)
        // If we move backwards (vertical < 0), we keep the current orientation.
        // If we have horizontal input, we rotate towards it only if vertical is not negative.
        if (vertical >= 0)
        {
            var targetRotation = Quaternion.LookRotation(moveDirection);
            var step = rotationSpeed * Time.deltaTime;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, step);
        }

        // Move the character
        transform.Translate(moveDirection * (speed * Time.deltaTime), Space.World);
    }
}
