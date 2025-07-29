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
            _targetRenderer = GetComponent<MeshRenderer>();

            _orchestrator = OrchestratorController.Instance.Orchestrator;
            var currentSession = _orchestrator.CurrentSession;

            currentSession.OnPresentationChanged += PresentationChanged;
            currentSession.OnPresentationIsSharingChanged += SharingChanged;
            currentSession.OnPresentationSlideChanged += SlideChanged;

            if (currentSession.CurrentPresentation != null)
            {
                _currentPresentation = currentSession.CurrentPresentation;
                StartCoroutine(LoadSlide());
            }
        }

        private void OnDestroy()
        {
            if (_slideMaterial)
            {
                Destroy(_slideMaterial);
            }
        }

        private void PresentationChanged(Presentation presentation)
        {
            _currentPresentation = presentation;
        }

        private void SharingChanged(Presentation presentation)
        {
            _currentPresentation = presentation;

            if (presentation == null || presentation.IsSharing == false)
            {
                return;
            }

            StartCoroutine(LoadSlide());
        }

        private void SlideChanged(Presentation presentation)
        {
            _currentPresentation = presentation;

            if (presentation != null)
            {
                StopAllCoroutines();
                StartCoroutine(LoadSlide());
            }
        }

        private IEnumerator LoadSlide()
        {
            var baseUrl = new Uri(_currentPresentation.SlidesURL);
            var currentSlide = _currentPresentation.CurrentSlide;

            var url = new Uri(baseUrl, $"presentation-{currentSlide}.png");

            Debug.Log($"Loading current slide from {url}...");

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
                        if (_slideMaterial)
                        {
                            Destroy(_slideMaterial);
                        }

                        _slideMaterial = new Material(Shader.Find("Unlit/Texture"));

                        _slideMaterial.mainTexture = texture;
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
