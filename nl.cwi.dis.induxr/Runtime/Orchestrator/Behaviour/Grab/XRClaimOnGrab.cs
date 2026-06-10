using Orchestrator.Behaviour.Shared;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Orchestrator.Behaviour.Grab
{
    /// <summary>
    /// Equivalent of ClaimOnGrab for XR interactions.
    /// Claims ownership of a SharedObjectBehaviour when the object is grabbed via XR.
    /// </summary>
    [RequireComponent(typeof(SharedObjectBehaviour))]
    public class XRClaimOnGrab : MonoBehaviour
    {
        private SharedObjectBehaviour _shared;
        private XRBaseInteractable _interactable;

        private void Awake()
        {
            _shared = GetComponent<SharedObjectBehaviour>();
            _interactable = GetComponent<XRBaseInteractable>();

            if (!_interactable)
                Debug.LogError($"XRClaimOnGrab on {name} requires an XRBaseInteractable component (e.g., XRGrabInteractable).", this);
        }

        private void OnEnable()
        {
            if (_interactable)
                _interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            if (_interactable)
                _interactable.selectEntered.RemoveListener(OnSelectEntered);
        }

        private async void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Ask the server for ownership.
            // Note: We don't prevent the grab itself from starting locally, but SharedObjectBehaviour
            // will only start broadcasting updates once ownership is confirmed.
            if (await _shared.ClaimObject()) return;

            Debug.Log($"Could not claim ownership of {name} — another client got it first.", this);

            // If claim fails, we force the interaction manager to cancel the selection.
            if (args.manager)
            {
                args.manager.CancelInteractableSelection(args.interactableObject);
            }
        }
    }
}
