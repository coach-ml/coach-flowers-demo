using Barracuda;
using Coach;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HackSampler : MonoBehaviour
{
    public RawImage flowerImage;
    public Text label;
    
    private CoachClient client;
    private CoachModel model;
    
    private Texture2D rose;
    private Texture2D daisy;
    private Texture2D sunflower;
    
    async void Start()
    {
        label.enabled = false;
        
        client = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await client.GetModelRemote("small_flowers");
        
        Debug.Log(model.Labels);
        
        this.daisy = Resources.Load<Texture2D>("Materials/daisy");
        this.rose = Resources.Load<Texture2D>("Materials/rose");
        this.sunflower = Resources.Load<Texture2D>("Materials/sunflower");
        
        label.enabled = true;
        
        Debug.LogError(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    public void SetDaisy() { SetPhoto(FlowerType.Daisy); }
    public void SetRose() { SetPhoto(FlowerType.Rose); }
    public void SetSunflower() { SetPhoto(FlowerType.Sunflower); }
    
    public void SetPhoto(FlowerType flowerType)
    {
        ran = false;
        Debug.Log("Setting: " + flowerType.ToString());
        if (flowerType == FlowerType.Daisy)
        {
            // var image = ImageUtil.Scale(this.daisy, 224, 224);
            flowerImage.texture = this.daisy;

        }
        else if (flowerType == FlowerType.Rose)
        {
            flowerImage.texture = this.rose;
        }
        else if (flowerType == FlowerType.Sunflower)
        {
            flowerImage.texture = this.sunflower;
        }
        
        var photo = GetPhoto();
        if (photo != null)
        {
            var imageTensor = model.ReadTensorFromTexture(photo);
            StartCoroutine(model.Worker.ExecuteAsync(imageTensor));
        }
    }
    bool ran = false;
    async void Update()
    {
        /*
        flowerImage.texture = this.daisy;
        
        var photo = GetPhoto(
        var imageTensor = model.ReadTensorFromTexture(photo);
        */
        // StartCoroutine(model.Worker.ExecuteAsync(imageTensor));
        // model.Worker.WaitForCompletion(imageTensor);
        
        /*
        if (model.Worker.GetAsyncProgress() > 0)
            Debug.Log(model.Worker.GetAsyncProgress());
        */
        
        if (model.Worker.GetAsyncProgress() == 1.0f && ran == false)
        {
            var output = model.Worker.Fetch("output");
            var prediction = new CoachResult(model.Labels, output);
            
            var best = prediction.Best();
            label.text = best.Label;
            Debug.Log("Guessing: " + best.Label + ": " + best.Confidence.ToString());
            string z = "";
            foreach (var r in prediction.SortedResults)
            {
                z += r.Label + ": " + r.Confidence + ", ";
            }
            Debug.Log(z);
            Debug.Log("———");
            
            ran = true;
        }
    }
    
    public Texture2D GetPhoto()
    {
        return flowerImage.texture as Texture2D;
    }
}