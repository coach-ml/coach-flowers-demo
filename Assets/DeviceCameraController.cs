using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
using Coach;

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


    async void Start()
    {
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
        model = await coach.GetModelRemote("small_flowers");
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
        predictionResult.text = best.Label;
        Debug.Log("Guessing: " + best.Label + ": " + best.Confidence.ToString());
        string z = "";
        foreach (var r in prediction.SortedResults)
        {
            z += r.Label + ": " + r.Confidence + ", ";
        }
        Debug.Log(z);
    }

    public void PredictWebcam() {
        var photo = GetWebcamPhoto();

        var prediction = model.Predict(photo);
        var best = prediction.Best();
        predictionResult.text = best.Label;
        Debug.Log("Guessing: " + best.Label + ": " + best.Confidence.ToString());
        string z = "";
        foreach (var r in prediction.SortedResults)
        {
            z += r.Label + ": " + r.Confidence + ", ";
        }
        Debug.Log(z);
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
            // Do something maybe
        }
    }
}