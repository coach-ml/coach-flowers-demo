using UnityEngine;
using UnityEngine.UI;
using Coach;
using System.IO;

public class WebcamController : MonoBehaviour
{
    public RawImage preview;
    public Text label;

    private CoachModel model;
    private WebCamTexture webcamTexture;

    async void Start()
    {
        Debug.Log(Application.persistentDataPath);  
        webcamTexture = new WebCamTexture();
        preview.texture = webcamTexture;
        preview.material.mainTexture = webcamTexture;

        webcamTexture.Play();

        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("flowers");
    }

    Texture2D GetTexture()
    {
        Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        return photo;
    }

    void Update()
    {
        // Wait until our model is loaded
        if (model != null)
        {
            var prediction = model.Predict(GetTexture()).Best();

            var result = $"{prediction.Label}: {prediction.Confidence}";
            label.text = result;
        }
    }

    private void OnDestroy()
    {
        model.CleanUp();
    }
}