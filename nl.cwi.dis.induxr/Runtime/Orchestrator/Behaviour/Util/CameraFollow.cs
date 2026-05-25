using UnityEngine;

namespace Orchestrator.Behaviour.Util
{
    public class CameraFollow : MonoBehaviour
    {
        public float smoothSpeed = 0.125f;
        public Vector3 locationOffset;
        public Vector3 rotationOffset;

        private Camera _camera;

        private void Start()
        {
            // Get the scene's main camera
            _camera = Camera.main;
        }

        private void FixedUpdate()
        {
            // Return if there is no camera
            if (!_camera)
                return;

            // Get camera transform
            var cameraTransform = _camera.transform;

            // Calculate the desired new position and linearly interpolate using the current position
            var desiredPosition = transform.position + transform.rotation * locationOffset;
            var smoothedPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, smoothSpeed);
            cameraTransform.position = smoothedPosition;

            // Calculate the desired new rotation and linearly interpolate using the current rotation
            var desiredRotation = transform.rotation * Quaternion.Euler(rotationOffset);
            var smoothedRotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, smoothSpeed);
            cameraTransform.rotation = smoothedRotation;
        }
    }
}
