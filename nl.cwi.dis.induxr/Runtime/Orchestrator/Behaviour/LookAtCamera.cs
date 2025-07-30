using UnityEngine;

namespace Orchestrator.Behaviour
{
    public class LookAtCamera : MonoBehaviour
    {
        public float baseYRotation = 0;
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
            transform.Rotate(0, baseYRotation, 0);
        }
    }
}
