using System;
using System.Collections;
using Orchestrator.Wrapping;
using Orchestrator.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Orchestrator.Behaviour
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
            // Get the renderer for the object this behavior is attached to
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

            // Return if there is no presentation, or we're not sharing anymore
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
            // Build URL from base URL and current slide
            var baseUrl = new Uri(_currentPresentation.SlidesURL);
            var currentSlide = _currentPresentation.CurrentSlide;

            // Slides are loaded from the given url with the filename `presentation-[slideNum].png` (e.g. http://localhost:8090/slides/presentation-10.png)
            var url = new Uri(baseUrl, $"presentation-{currentSlide}.png");

            Debug.Log($"Loading current slide from {url}...");

            // Make the request using built URL
            var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading image: {request.error}");
            }
            else
            {
                // Get the downloaded texture
                var texture = DownloadHandlerTexture.GetContent(request);

                if (texture)
                {
                    // Apply the texture to the target Renderer's material
                    if (_targetRenderer)
                    {
                        // Destroy existing slide material if it exists
                        if (_slideMaterial)
                        {
                            Destroy(_slideMaterial);
                        }

                        // Initialize a new material with the downloaded texture
                        _slideMaterial = new Material(Shader.Find("Unlit/Texture"))
                        {
                            mainTexture = texture
                        };

                        // Apply material to renderer
                        _targetRenderer.material = _slideMaterial;
                        Debug.Log("Image successfully loaded onto 3D surface.");
                    }
                    else
                    {
                        Debug.LogWarning("No target Renderer or UI Image component assigned to display the image.");
                    }
                }
                else
                {
                    Debug.LogError("Failed to get texture from downloaded data.");
                }
            }
        }
    }
}
