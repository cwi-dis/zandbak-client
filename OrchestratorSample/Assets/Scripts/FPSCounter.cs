using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public float refreshTime = 0.5f;

    private int _frameCounter;
    private float _timeCounter;
    private float _lastFramerate;
    private TMP_Text _showFPSText;

    private void Start()
    {
        _showFPSText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (_timeCounter < refreshTime)
        {
            _timeCounter += Time.deltaTime;
            _frameCounter++;
        }
        else
        {
            _lastFramerate = _frameCounter / _timeCounter;
            _frameCounter = 0;
            _timeCounter = 0.0f;

            _showFPSText.SetText("FPS: " + Mathf.RoundToInt(_lastFramerate));
        }
    }
}
