using System;
using System.Collections;
using Orchestrator.Wrapping;
using Orchestrator.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Orchestrator.Behaviour.Util
{
    public class SlideLoader : MonoBehaviour
    {
        private App.Orchestrator _orchestrator;
        private Presentation _currentPresentation;

        private Renderer _targetRenderer;
        private Material _slideMaterial;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            // Get the renderer for the object this behaviour is attached to
            _targetRenderer = GetComponent<MeshRenderer>();

            _orchestrator = OrchestratorController.Instance.Orchestrator;
            var currentSession = _orchestrator.CurrentSession;

            // Attach handlers for presentation-related events
            currentSession.OnPresentationChanged += PresentationChanged;
            currentSession.OnPresentationIsSharingChanged += SharingChanged;
            currentSession.OnPresentationSlideChanged += SlideChanged;

            if (currentSession.CurrentPresentation != null)
            {
                // Load current slide
                _currentPresentation = currentSession.CurrentPresentation;
                StartCoroutine(LoadSlide());
            }
        }

        private void OnDestroy()
        {
            // Destroy slide material if it exists
            if (_slideMaterial)
            {
                Destroy(_slideMaterial);
            }
        }

        private void PresentationChanged(Presentation presentation)
        {
            // Update current presentation
            _currentPresentation = presentation;
        }

        private void SharingChanged(Presentation presentation)
        {
            _currentPresentation = presentation;

            // Return if there is no presentation, or we're not sharing any more
            if (presentation == null || presentation.IsSharing == false)
            {
                return;
            }

            // Load current slide
            StartCoroutine(LoadSlide());
        }

        private void SlideChanged(Presentation presentation)
        {
            _currentPresentation = presentation;

            if (presentation != null)
            {
                // Load current slide
                StopAllCoroutines();
                StartCoroutine(LoadSlide());
            }
        }

        private IEnumerator LoadSlide()
        {
            // If the property SlidesURL is provided for the current presentation, use it as base URL, otherwise use
            // the URL of the orchestrator socket at the path /slides.
            var baseUrl = _currentPresentation.SlidesURL switch
            {
                null => new Uri(_orchestrator.SocketUrl, "/slides/"),
                _ => new Uri(_currentPresentation.SlidesURL)
            };

            // Slides are loaded from the given url with the filename `presentation-[presentationId]-[slideNum].png`,
            // e.g. http://localhost:8090/slides/presentation-6877ed4ad845d403392410b7-10.png
            var currentSlide = _currentPresentation.CurrentSlide;
            var presentationId = _currentPresentation.Id;
            var url = new Uri(baseUrl, $"presentation-{presentationId}-{currentSlide}.png");

            Debug.Log($"Loading current slide from {url}...");

            // Make the request using built URL
            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading image: {request.error}");
                yield break;
            }

            // Get the downloaded texture
            var texture = DownloadHandlerTexture.GetContent(request);

            if (!texture)
            {
                Debug.LogError("Failed to get texture from downloaded data.");
                yield break;
            }

            if (!_targetRenderer)
            {
                Debug.LogWarning("No target Renderer or UI Image component assigned to display the image.");
                yield break;
            }

            // Destroy existing slide material if it exists
            if (_slideMaterial)
            {
                Destroy(_slideMaterial);
            }

            // Initialise a new material with the downloaded texture
            _slideMaterial = new Material(Shader.Find("Unlit/Texture"))
            {
                mainTexture = texture
            };

            // Apply material to renderer
            _targetRenderer.material = _slideMaterial;
            Debug.Log("Image successfully loaded onto 3D surface.");
        }
    }
}
