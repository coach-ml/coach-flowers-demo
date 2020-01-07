using Coach;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum FlowerType
{
    Rose, Sunflower, Daisy
}

public class TextureSampler : MonoBehaviour
{
    public RawImage flowerImage;
    public Text label;

    private CoachModel model;

    private Texture2D rose;
    private Texture2D daisy;
    private Texture2D sunflower;

    async void Start()
    {
        label.enabled = false;

        var coach = await new CoachClient().Login("A2botdrxAn68aZh8Twwwt2sPBJdCfH3zO02QDMt0");
        model = await coach.GetModelRemote("small_flowers");

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
        Debug.Log("Setting: " + flowerType.ToString());
        if (flowerType == FlowerType.Daisy)
        {
            flowerImage.texture = this.daisy;
        } else if (flowerType == FlowerType.Rose)
        {
            flowerImage.texture = this.rose;
        } else if (flowerType == FlowerType.Sunflower)
        {
            flowerImage.texture = this.sunflower;
        }

        var photo = GetPhoto();
        if (photo != null)
        {
            var prediction = model.Predict(photo);
            var best = prediction.Best();
            label.text = best.Label;
            Debug.Log("Guessing: " + best.Label + ": " + best.Confidence.ToString());
            Debug.Log("———");
        }
    }

    public Texture2D GetPhoto()
    {
        return flowerImage.texture as Texture2D;
    }
}
