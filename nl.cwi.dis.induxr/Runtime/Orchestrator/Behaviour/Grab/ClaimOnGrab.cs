using UnityEngine;

namespace Orchestrator.Behaviour.Grab
{
    [RequireComponent(typeof(Collider), typeof(SharedObjectBehaviour))]
    public class ClaimOnGrab : MonoBehaviour
    {
        private SharedObjectBehaviour _shared;
        private Camera _camera;
        private bool _dragging;
        private float _screenDepth; // camera-space z of the object at pickup time
        private Vector3 _grabOffset; // world-space offset between object centre and the hit point

        private void Awake()
        {
            _shared = GetComponent<SharedObjectBehaviour>();
            _camera = Camera.main;
        }

        // Unity calls this when the left mouse button goes down over this collider.
        private async void OnMouseDown()
        {
            // Capture drag geometry synchronously, BEFORE awaiting — the cursor may move
            // and a physics raycast in the middle of an async method is fragile.
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit) || hit.transform != transform) return;

            _screenDepth = _camera.WorldToScreenPoint(transform.position).z;
            _grabOffset = transform.position - hit.point;

            // Ask the server for ownership. This is a network round-trip — anything from
            // a few ms on LAN to a few hundred on a bad connection.
            if (!await _shared.ClaimObject())
            {
                Debug.Log($"Could not claim ownership of {name} — another client got it first.");
                return;
            }

            // The user may already have released the mouse during the round-trip.
            // If so, don't start a drag that will never end.
            if (Input.GetMouseButton(0)) _dragging = true;
        }

        // Fires every frame between OnMouseDown and OnMouseUp, regardless of cursor position.
        private void OnMouseDrag()
        {
            if (!_dragging) return;

            var screen = new Vector3(Input.mousePosition.x, Input.mousePosition.y, _screenDepth);
            transform.position = _camera.ScreenToWorldPoint(screen) + _grabOffset;
        }

        private void OnMouseUp()
        {
            _dragging = false;
            // Ownership stays with this client — no need to release. The next claimer
            // (or no one) takes over whenever.
        }
    }
}
