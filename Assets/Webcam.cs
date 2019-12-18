using Coach;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Webcam : MonoBehaviour
{
    public RawImage WebcamImage;
    public Text label;

    private CoachModel model;

    private WebCamTexture _webCamTexture { get; set; }

    CumulativeConfidenceResult results; 

    public async void Start()
    {
        StartCoroutine(SetupWebcam());

        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("small_flowers");
    }

    private float aTime = 0;
    private void Update()
    {
        if (model != null)
        {
            aTime += Time.deltaTime;
            if (aTime >= 0.2f)
            {
                aTime = 0;

                // Keep updating results
                model.CumulativeConfidence(GetPhoto(), 5, ref results);

                var best = results.LastResult.Best();
                if (results.IsPassedThreshold())
                {
                    Debug.LogWarning("Passed the threshold");
                    label.text = $"You found the {best.Label}";
                } else
                {
                    var result = $"{best.Label}: {best.Confidence}";
                    label.text = result;
                }
            }
        }
    }

    public Texture2D GetPhoto()
    {
        Texture2D photo = new Texture2D(_webCamTexture.width, _webCamTexture.height);
        photo.SetPixels(_webCamTexture.GetPixels());
        photo.Apply();

        return photo;
    }

    public void Stop()
    {
        _webCamTexture.Stop();
    }

    public void Pause()
    {
        _webCamTexture.Pause();
    }

    private IEnumerator SetupWebcam()
    {
        while (_webCamTexture == null)
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length > 0)
            {
                _webCamTexture = new WebCamTexture(devices[0].name, 720, 1280);
                _webCamTexture.Play();

                yield return new WaitUntil(() => _webCamTexture.width > 10);

                WebcamImage.texture = _webCamTexture;
                WebcamImage.color = Color.white;
                bool rotated = TransformImage();
                SetImageSize(rotated);
            }
            yield return 0;
        }
    }

    private void SetImageSize(bool rotated)
    {
        var canvas = gameObject.GetComponentInParent<Canvas>();
        var canvasRect = canvas.pixelRect;
        var rotTexSize = rotated ? new Vector2(_webCamTexture.height, _webCamTexture.width) : new Vector2(_webCamTexture.width, _webCamTexture.height);
        float ratio = 1f;
        if (rotTexSize.x < canvasRect.width && rotTexSize.y > canvasRect.height)
        {
            ratio = canvasRect.width / rotTexSize.x;
        }
        else if (rotTexSize.x > canvasRect.width && rotTexSize.y < canvasRect.height)
        {
            ratio = canvasRect.height / rotTexSize.y;
        }
        else if (rotTexSize.x < canvasRect.width && rotTexSize.y < canvasRect.height)
        {
            var widthRatio = canvasRect.width / rotTexSize.x;
            var heightRatio = canvasRect.height / rotTexSize.y;
            ratio = widthRatio < heightRatio ? heightRatio : widthRatio;
        }
        else if (rotTexSize.x > canvasRect.width && rotTexSize.y > canvasRect.height)
        {
            var widthRatio = canvasRect.width / rotTexSize.x;
            var heightRatio = canvasRect.height / rotTexSize.y;
            ratio = widthRatio < heightRatio ? heightRatio : widthRatio;
        }

        var rect = WebcamImage.gameObject.GetComponent<RectTransform>();
        var texSize = new Vector2(_webCamTexture.width, _webCamTexture.height);
        rect.sizeDelta = texSize * ratio / canvas.scaleFactor;
    }
    private bool TransformImage()
    {
#if UNITY_IOS
		// iOS cam is mirrored
        image.gameObject.transform.localScale = new Vector3(-1, 1, 1);
#endif
        WebcamImage.gameObject.transform.Rotate(0.0f, 0, -_webCamTexture.videoRotationAngle);
        return _webCamTexture.videoRotationAngle != 0;
    }
}
