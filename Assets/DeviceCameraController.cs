using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Coach;
using Barracuda;
using System.Collections.Generic;

public class DeviceCameraController : MonoBehaviour
{
    public Text predictionResult;
    private CoachModel model;

    public RawImage image;
    public RectTransform imageParent;
    public AspectRatioFitter imageFitter;

    // Device cameras
    WebCamDevice frontCameraDevice;
    WebCamDevice backCameraDevice;
    WebCamDevice activeCameraDevice;

    WebCamTexture frontCameraTexture;
    WebCamTexture backCameraTexture;
    WebCamTexture activeCameraTexture;

    // Image rotation
    Vector3 rotationVector = new Vector3(0f, 0f, 0f);

    // Image uvRect
    Rect defaultRect = new Rect(0f, 0f, 1f, 1f);
    Rect fixedRect = new Rect(0f, 1f, 1f, -1f);

    // Image Parent's scale
    Vector3 defaultScale = new Vector3(1f, 1f, 1f);
    Vector3 fixedScale = new Vector3(-1f, 1f, 1f);

    private readonly int Workers = 4;


    async void Start()
    {
        var gpuName = SystemInfo.graphicsDeviceName;
        if (gpuName != null)
        {
            Debug.Log("GPU: " + gpuName);
            int gpuNumber = 0, idx = "Apple A".Length;
            while (idx < gpuName.Length && '0' <= gpuName[idx] && gpuName[idx] <= '9')
            {
                gpuNumber = gpuNumber * 10 + gpuName[idx++] - '0';
            }
            Debug.Log("GPU COUNT: " + gpuNumber);
        }
        // Check for device cameras
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.Log("No devices cameras found");
            return;
        }

        // Get the device's cameras and create WebCamTextures with them
#if UNITY_EDITOR
        if(WebCamTexture.devices.Any(d => d.name.Contains("Remote")))
        {
            frontCameraDevice = WebCamTexture.devices.Last(d => d.name.Contains("Remote"));
            backCameraDevice = WebCamTexture.devices.First(d => d.name.Contains("Remote"));
        }
        else
        {
            frontCameraDevice = WebCamTexture.devices.Last();
            backCameraDevice = WebCamTexture.devices.First();
        }
#else
        frontCameraDevice = WebCamTexture.devices.Last();
        backCameraDevice = WebCamTexture.devices.First();
#endif

        frontCameraTexture = new WebCamTexture(frontCameraDevice.name);
        backCameraTexture = new WebCamTexture(backCameraDevice.name);

        // Set camera filter modes for a smoother looking image
        frontCameraTexture.filterMode = FilterMode.Trilinear;
        backCameraTexture.filterMode = FilterMode.Trilinear;

        // Set the camera to use by default
        SetActiveCamera(backCameraTexture);

        // Download Coach model
        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("small_flowers", workers: this.Workers);
    }

    public Texture2D GetWebcamPhoto()
    {
        if (activeCameraTexture != null)
        {
            Texture2D photo = new Texture2D(activeCameraTexture.width, activeCameraTexture.height);
            photo.SetPixels(activeCameraTexture.GetPixels());
            photo.Apply();

            return photo;
        }

        return null;
    }

    public void PredictRose() {
        var photo = Resources.Load<Texture2D>("Materials/rose");

        var prediction = model.Predict(photo);
        var best = prediction.Best();
        predictionResult.text = best.Label + ": " + best.Confidence.ToString();
        Debug.Log("Guessing: " + best.Label + ": " + best.Confidence.ToString());
        string z = "";
        foreach (var r in prediction.SortedResults)
        {
            z += r.Label + ": " + r.Confidence + ", ";
        }
        Debug.Log(z);
    }

    public void PredictWebcam() {
        //var photo = GetWebcamPhoto();
        // var prediction = model.Predict(photo);
    }

    public void Play()
    {
        if(activeCameraTexture != null)
        {
            activeCameraTexture.Play();
        }
    }

    public void Stop()
    {
        if (activeCameraTexture != null)
        {
            activeCameraTexture.Stop();
        }
    }

    // Set the device camera to use and start it
    public void SetActiveCamera(WebCamTexture cameraToUse)
    {
        if (activeCameraTexture != null)
        {
            activeCameraTexture.Stop();
        }
        
        activeCameraTexture = cameraToUse;
        activeCameraDevice = WebCamTexture.devices.FirstOrDefault(device =>
            device.name == cameraToUse.deviceName);

        image.texture = activeCameraTexture;
        image.material.mainTexture = activeCameraTexture;

        activeCameraTexture.Play();
    }

    // Switch between the device's front and back camera
    public void SwitchCamera()
    {
        SetActiveCamera(activeCameraTexture.Equals(frontCameraTexture) ?
            backCameraTexture : frontCameraTexture);
    }

    // Make adjustments to image every frame to be safe, since Unity isn't 
    // guaranteed to report correct data as soon as device camera is started

    CumulativeConfidenceResult results;
    float aTime = 0;
    void Update()
    {
        // Skip making adjustment for incorrect camera data
        if (activeCameraTexture.width < 100)
        {
            return;
        }

        // Rotate image to show correct orientation 
        rotationVector.z = -activeCameraTexture.videoRotationAngle;
        image.rectTransform.localEulerAngles = rotationVector;

        //image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x, Screen.height);

        // Set AspectRatioFitter's ratio
        float videoRatio =
            (float)activeCameraTexture.width / (float)activeCameraTexture.height;
        imageFitter.aspectRatio = videoRatio;

        // Unflip if vertically flipped
        image.uvRect =
            activeCameraTexture.videoVerticallyMirrored ? fixedRect : defaultRect;

        // Mirror front-facing camera's image horizontally to look more natural
        imageParent.localScale =
            activeCameraDevice.isFrontFacing ? fixedScale : defaultScale;

        if (model != null) {
            // Every once in a while when the workers aren't busy
            aTime += Time.deltaTime;
            if (aTime >= 0.5f && model.WorkerAvailable())
            {
                aTime = 0;
                StartCoroutine(model.PredictAsync(GetWebcamPhoto()));
            }

            /* Check for results every Update
            var modelResult = model.GetPredictionResultAsync(true);
            if (modelResult != null)
            {
                var best = modelResult.Best();

                Debug.Log("We have a result: " + best.Label + ": " + best.Confidence);
                predictionResult.text = best.Label + ": " + best.Confidence;
            }
            */

            // Example of using cumulative:
            model.CumulativeConfidenceAsync(5f, ref results, true);
            if (results.LastResult != null) {
                var best = results.LastResult.Best();
                if (results.IsPassedThreshold())
                {
                    Debug.LogWarning("Passed the threshold");

                    var result = $"{best.Label}: {best.Confidence}";
                    predictionResult.text = result;
                }
                Debug.Log("We have a result: " + best.Label + ": " + best.Confidence);
            }
        }
    }

    private void OnDestroy()
    {
        model.CleanUp();
    }
}