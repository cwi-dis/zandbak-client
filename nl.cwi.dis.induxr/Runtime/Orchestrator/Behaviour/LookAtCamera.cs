using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class LookAtCamera : MonoBehaviour
    {
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (!_camera)
            {
                return;
            }

            transform.LookAt(_camera.transform);
            transform.Rotate(0, 180, 0);
        }
    }
}
