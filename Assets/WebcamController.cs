using UnityEngine;
using UnityEngine.UI;
using Coach;

public class WebcamController : MonoBehaviour
{
    public RawImage preview;
    public Text label;

    private CoachModel model;
    private WebCamTexture webcamTexture;

    async void Start()
    {
        webcamTexture = new WebCamTexture();
        preview.texture = webcamTexture;
        preview.material.mainTexture = webcamTexture;

        webcamTexture.Play();

        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("small-flowers");
        //model = coach.GetModel(System.IO.Path.Combine(Application.persistentDataPath, "flowers"));
        //model = coach.GetModel(System.IO.Path.Combine(Application.persistentDataPath, "mobilenet"));
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
            var prediction = model.Predict(GetTexture(), inputName: "input_1", outputName: "out_relu/Relu6").Results;
            //var prediction = model.Predict(GetTexture(), inputName: "input", outputName: "output").Results;
            foreach (var p in prediction)
            {
                Debug.Log($"{p.Label}: {p.Confidence}");
            }
            Debug.Log("-----------");

            var best = prediction[0];

            var result = $"{best.Label}: {best.Confidence}";
            label.text = result;
        }
    }

    void OnDestroy()
    {
        model.CleanUp();
    }
}