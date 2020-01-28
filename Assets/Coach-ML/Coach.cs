using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Barracuda;
using UnityEngine.Networking;
using System.Net;
using System.Collections;

namespace Coach
{
    static class UnityWebRequestExtensions
    {
        public static bool IsSuccessStatusCode(this UnityWebRequest webRequest)
        {
            return !webRequest.isHttpError && !webRequest.isNetworkError;
        }

        public static HttpStatusCode StatusCode(this UnityWebRequest webRequest)
        {
            return (HttpStatusCode)webRequest.responseCode;
        }

        public static Task SendWebRequestAsync(this UnityWebRequest webRequest)
        {
            var op = webRequest.SendWebRequest();
            var source = new TaskCompletionSource<UnityWebRequest>();
            op.completed += (_) =>
            {
                source.SetResult(webRequest);
            };
            return source.Task;
        }
    }

    public static class Networking
    {
        public static async Task<byte[]> GetContentAsync(string url, string authHeader)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, "GET");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("X-Api-Key", authHeader);
            webRequest.SetRequestHeader("Accept", "");
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");

            await webRequest.SendWebRequestAsync();

            if (webRequest.IsSuccessStatusCode())
            {
                var content = webRequest.downloadHandler.data;
                return content;
            }

            throw new Exception("Request failed");

        }
        public static async Task<string> GetTextAsync(string url, string authHeader)
        {
            UnityWebRequest webRequest = new UnityWebRequest(url, "GET");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("X-Api-Key", authHeader);

            await webRequest.SendWebRequestAsync();

            if (webRequest.IsSuccessStatusCode())
            {
                var content = webRequest.downloadHandler.text;
                return content;
            }

            throw new Exception("Request failed");
        }
    }

    public struct ImageDims
    {
        public int InputSize { get; set; }
        public int ImageMean { get; set; }
        public float ImageStd { get; set; }

        public ImageDims(int inputSize, int imageMean, float imageStd)
        {
            this.InputSize = inputSize;
            this.ImageMean = imageMean;
            this.ImageStd = imageStd;
        }
    }

    public static class ImageUtil
    {
        /// <summary>
        /// Scales the texture data of the given texture.
        /// </summary>
        /// <param name="tex">Texure to scale</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        /// <param name="mode">Filtering mode</param>
        private static Texture2D Scale(this Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear)
        {
            Rect texR = new Rect(0, 0, width, height);
            RenderTexture rt = GpuScale(tex, width, height, mode);

            // Update new texture
            tex.Resize(width, height);
            tex.ReadPixels(texR, 0, 0, true);
            tex.Apply(true);

            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }

        // Internal unility that renders the source texture into the RTT - the scaling method itself.
        private static RenderTexture GpuScale(Texture2D src, int width, int height, FilterMode fmode)
        {
            //We need the source texture in VRAM because we render with it
            src.filterMode = fmode;
            src.Apply(true);

            //Using RTT for best quality and performance. Thanks, Unity 5
            var rtt = RenderTexture.GetTemporary(width, height, 32);

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);

            //Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);

            //Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);

            return rtt;
        }

        private static Tensor ToTensor(this Texture2D tex, ImageDims dims)
        {
            var pic = tex.GetPixels32();

            int INPUT_SIZE = dims.InputSize;
            int IMAGE_MEAN = dims.ImageMean;
            float IMAGE_STD = dims.ImageStd;
            
            float[] floatValues = new float[(INPUT_SIZE * INPUT_SIZE) * 3];

            for (int i = 0; i < pic.Length; i++)
            {
                var color = pic[i];

                floatValues[i * 3] = (color.r - IMAGE_MEAN) / IMAGE_STD;
                floatValues[i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
                floatValues[i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
            }

            var shape = new TensorShape(1, INPUT_SIZE, INPUT_SIZE, 3);
            return new Tensor(shape, floatValues);
        }

        public static Texture2D TextureFromFile(string filePath)
        {
            return TextureFromBytes(File.ReadAllBytes(filePath));
        }

        public static Texture2D TextureFromBytes(byte[] input)
        {
            Texture2D tex = new Texture2D(0, 0);
            tex.LoadImage(input);

            return tex;
        }

        public static Tensor TensorFromTexture(Texture2D image, ImageDims dims)
        {
            image = image.Scale(dims.InputSize, dims.InputSize);
            return image.ToTensor(dims);
        }
    }
    
    public class CoachResult
    {
        ///<summary>
        //Unsorted prediction results
        ///</summary>
        public LabelProbability[] Results { get; private set; }

        ///<summary>
        //Sorted prediction results, descending in Confidence
        ///</summary>
        public LabelProbability[] SortedResults { get; private set; }

        public CoachResult(string[] labels, Tensor output)
        {
            Results = new LabelProbability[labels.Length];

            for (var i = 0; i < labels.Length; i++)
            {
                string label = labels[i];
                float probability = output[i];

                Results[i] = new LabelProbability()
                {
                    Label = label,
                    Confidence = probability
                };
            }
            SortedResults = Results.OrderByDescending(r => r.Confidence).ToArray();
        }

        ///<summary>
        ///Most Confident result
        ///</summary>
        public LabelProbability Best()
        {
            return SortedResults.FirstOrDefault();
        }

        ///<summary>
        ///Least Confident result
        ///</summary>
        public LabelProbability Worst()
        {
            return SortedResults.LastOrDefault();
        }
    }

    public struct CumulativeConfidenceResult
    {
        /// <summary>
        /// Threshold to consider a success
        /// </summary>
        public float Threshhold;
        public CoachResult LastResult;

        /// <summary>
        /// Our current, cumulative progress
        /// </summary>
        public float CumulativeConfidence;

        public bool IsPassedThreshold()
        {
            return CumulativeConfidence >= Threshhold;
        }
    }

    public struct LabelProbability
    {
        public string Label { get; set; }
        /// <summary>
        /// Confidence score of our prediction, 0.0 = 0%, 1.0 = 100%
        /// </summary>
        public float Confidence { get; set; }
    }

    public class CoachWorker
    {
        private IWorker Worker { get; set; }
        private Texture2D Texture { get; set; }
        private Tensor Tensor { get; set; }
        private CoachModel Model { get; set; }

        public CoachWorker(CoachModel model)
        {
            this.Model = model;

            this.Worker = WorkerFactory.CreateComputeWorker(model.Model);
            this.Worker.PrepareForInput(new Dictionary<string, TensorShape>()
            {
                { model.InputName, new TensorShape(1, model.ImageDims.InputSize, model.ImageDims.InputSize, 3) }
            });
        }

        public void Execute(Texture2D texture)
        {
            this.Texture = texture;
            this.Tensor = ImageUtil.TensorFromTexture(texture, Model.ImageDims);
            this.Worker.Execute(this.Tensor);
        }

        public void Execute(Tensor tensor)
        {
            this.Tensor = tensor;
            this.Worker.Execute(this.Tensor);
        }

        public IEnumerator ExecuteAsync(Texture2D texture)
        {
            this.Texture = texture;
            this.Tensor = ImageUtil.TensorFromTexture(texture, Model.ImageDims);
            return this.Worker.ExecuteAsync(this.Tensor);
        }

        public IEnumerator ExecuteAsync(Tensor tensor)
        {
            this.Tensor = tensor;
            return this.Worker.ExecuteAsync(this.Tensor);
        }

        public float GetAsyncProgress()
        {
            return this.Worker.GetAsyncProgress();
        }

        public bool IsAvailable()
        {
            return GetAsyncProgress() == 0f;
        }

        public bool IsDone()
        {
            return GetAsyncProgress() == 1f;
        }

        public bool IsBusy()
        {
            return GetAsyncProgress() > 0f;
        }

        public Tensor PeekOutput()
        {
            return Worker.PeekOutput(Model.OutputName);
        }

        public void Reset(bool destroyTexture = false)
        {
            if (destroyTexture && Texture != null)
            {
                Texture2D.Destroy(Texture);
            }
            if (Tensor != null)
            {
                Tensor.Dispose();
            }
            Worker.ResetAsyncProgress();
        }

        public void Dispose(bool destroyTexture = false)
        {
            Reset(destroyTexture);
            Worker.Dispose();
        }
    }

    public class CoachModel
    {
        private readonly float COACH_VERSION = 2f;

        public string[] Labels { get; private set; }
        public ImageDims ImageDims { get; private set; }

        public string InputName { get; private set; }
        public string OutputName { get; private set; }

        /// <summary>
        /// Barracuda model
        /// </summary>
        public Model Model { get; private set; }

        private List<CoachWorker> Workers = new List<CoachWorker>();

        ///<summary>
        ///<param>Model graph</param>
        ///<param>Model labels</param>
        ///<param>Base module used for training</param>
        ///<param>Model SDK version</param>
        ///<paramref name="workers">Number of workers to spawn</paramref>
        ///</summary>
        public CoachModel(Model model, string[] labels, string module, float coachVersion, int workers = 1, string inputName = "input", string outputName = "output")
        {
            if (COACH_VERSION != coachVersion)
            {
                throw new Exception($"Coach model v{coachVersion} incompatible with SDK version {COACH_VERSION}");
            }

            this.InputName = inputName;
            this.OutputName = outputName;
            this.Labels = labels;

            int size = int.Parse(module.Substring(module.Length - 3, 3));
            this.ImageDims = new ImageDims(size, 0, 255);
            this.Model = model;

            if (workers <= 0)
            {
                throw new Exception($"Must be a minimum of 1 worker");
            }
            for (int i = 0; i < workers; i++)
            {
                this.Workers.Add(new CoachWorker(this));
            }
        }

        public string WorkerSummary()
        {
            string r = "";
            for (var i = 0; i < Workers.Count(); i++)
            {
                var worker = Workers[i];
                r += $"{i}: {worker.GetAsyncProgress()}\n";
            }
            return r;
        }

        /// <summary>
        /// Whether or not all workers are currently busy
        /// </summary>
        public bool AllWorkersBusy()
        {
            return this.Workers.Select(w => w.IsAvailable()).Contains(false);
        }

        /// <summary>
        /// Whether or not there is an available worker
        /// </summary>
        public bool WorkerAvailable()
        {
            return this.Workers.Select(w => w.IsAvailable()).Contains(true);
        }

        private CoachWorker GetFirstAvailableWorker()
        {
            return this.Workers.FirstOrDefault(w => w.IsAvailable());
        }

        private Tensor ReadTensorFromBytes(byte[] image)
        {
            var texture = ImageUtil.TextureFromBytes(image);
            return ImageUtil.TensorFromTexture(texture, this.ImageDims);
        }

        private Tensor ReadTensorFromFile(string filePath)
        {
            var texture = ImageUtil.TextureFromFile(filePath);
            return ImageUtil.TensorFromTexture(texture, this.ImageDims);
        }

        ///<summary>
        ///Parses the specified Texture2D as a Tensor and runs it through the loaded model
        ///<param>Path to the sample image</param>
        ///<param>Name of the input in the graph</param>
        ///<param>Name of the output in the graph</param>
        ///<paramref name="destroyTexture">Optional param to destroy the associated Texture2D when prediction is finished</paramref>
        ///</summary>
        public CoachResult Predict(Texture2D texture, bool destroyTexture = false)
        {
            return GetModelResult(texture, destroyTexture);
        }

        ///<summary>
        ///Parses the specified image from path as a Tensor and runs it through the loaded model
        ///<param>Path to the sample image</param>
        ///<param>Name of the input in the graph</param>
        ///<param>Name of the output in the graph</param>
        ///</summary>
        public CoachResult Predict(string image)
        {
            var imageTensor = ReadTensorFromFile(image);
            return GetModelResult(imageTensor);
        }

        ///<summary>
        /// Parses the specified image bytes as a Tensor and runs it through the loaded model
        ///<param>Image as byte array</param>
        ///<param>Name of the input in the graph</param>
        ///<param>Name of the output in the graph</param>
        ///</summary>
        public CoachResult Predict(byte[] image)
        {
            var imageTensor = ReadTensorFromBytes(image);
            return GetModelResult(imageTensor);
        }

        /// <summary>
        /// Runs prediction on input Texture2D async with first available worker in queue.
        /// Used in tandem with GetPredictionResultAsync() to get prediction results.
        /// </summary>
        /// <param name="image">Input Texture2D to predict</param>
        /// <returns>Coroutine</returns>
        public IEnumerator PredictAsync(Texture2D image)
        {
            var worker = GetFirstAvailableWorker();
            if (worker != null)
            {
                yield return worker.ExecuteAsync(image);
            }
            yield return null;
        }

        /// <summary>
        /// Runs prediction on input image async with first available worker in queue.
        /// Used in tandem with GetPredictionResultAsync() to get prediction results.
        /// </summary>
        /// <param name="image">Path of image to run prediction on</param>
        /// <returns>Coroutine</returns>
        public IEnumerator PredictAsync(string image)
        {
            var imageTensor = ReadTensorFromFile(image);
            var worker = GetFirstAvailableWorker();
            if (worker != null)
            {
                yield return worker.ExecuteAsync(imageTensor);
            }
            yield return null;
        }

        /// <summary>
        /// Runs prediction on input image async with first available worker in queue.
        /// Used in tandem with GetPredictionResultAsync() to get prediction results.
        /// </summary>
        /// <param name="image">Image as byte array to run prediction on</param>
        /// <returns>Coroutine</returns>
        public IEnumerator PredictAsync(byte[] image)
        {
            var imageTensor = ReadTensorFromBytes(image);
            var worker = GetFirstAvailableWorker();
            if (worker != null)
            {
                yield return worker.ExecuteAsync(imageTensor);
            }
            yield return null;
        }

        /// <summary>
        /// Runs prediction until the consecutive, cumulative score matches or goes above the specified threshold.
        /// When a prediction does not match the previous prediction, the counter is reset.
        /// </summary>
        /// <param name="image">Input Texture2D to predict</param>
        /// <param name="threshhold">Score to consider</param>
        /// <param name="result">The output CumulativeConfidenceResult to update</param>
        public void CumulativeConfidence(Texture2D image, float threshhold, ref CumulativeConfidenceResult result)
        {
            string prevLabel = null;
            if (result.LastResult != null)
                prevLabel = result.LastResult.Best().Label;
            var prediction = Predict(image);

            result.LastResult = prediction;
            result.Threshhold = threshhold;

            if (prevLabel != null && prevLabel != prediction.Best().Label)
                result.CumulativeConfidence = 0;
            else if (result.CumulativeConfidence <= threshhold)
                result.CumulativeConfidence += prediction.Best().Confidence;
        }

        /// <summary>
        /// Runs prediction until the consecutive, cumulative score matches or goes above the specified threshold.
        /// When a prediction does not match the previous prediction, the counter is reset. To be used with PredictAsync
        /// </summary>
        /// <param name="threshhold">Score to consider</param>
        /// <param name="result">The output CumulativeConfidenceResult to update</param>
        /// <param name="destroyTexture">Optional flag to destroy Texture2D once prediction is finished</param>
        public void CumulativeConfidenceAsync(float threshhold, ref CumulativeConfidenceResult result, bool destroyTexture = false)
        {
            string prevLabel = null;
            if (result.LastResult != null)
                prevLabel = result.LastResult.Best().Label;

            var prediction = GetPredictionResultAsync(destroyTexture);
            if (prediction != null)
            {
                result.LastResult = prediction;
                result.Threshhold = threshhold;

                if (prevLabel != null && prevLabel != prediction.Best().Label)
                    result.CumulativeConfidence = 0;
                else if (result.CumulativeConfidence <= threshhold)
                    result.CumulativeConfidence += prediction.Best().Confidence;
            }
        }
        
        private CoachResult GetModelResult(Texture2D texture, bool destroyTexture = false)
        {
            var worker = GetFirstAvailableWorker();
            if (worker != null)
            {
                worker.Execute(texture);

                // Get the output
                var output = worker.PeekOutput();
                worker.Reset(destroyTexture);
                return new CoachResult(Labels, output);
            }

            return null;
        }

        private CoachResult GetModelResult(Tensor imageTensor)
        {
            var worker = GetFirstAvailableWorker();
            if (worker != null)
            {
                worker.Execute(imageTensor);

                // Get the output
                var output = worker.PeekOutput();
                worker.Reset();
                return new CoachResult(Labels, output);
            }

            return null;
        }

        /// <summary>
        /// Gets the result of an async prediction
        /// </summary>
        /// <param name="destroyTexture">Optional param to destroy the input Texute2D when the prediction results are found</param>
        /// <returns>CoachResult when there is a result, otherwise null</returns>
        public CoachResult GetPredictionResultAsync(bool destroyTexture = false)
        {
            foreach (var worker in this.Workers)
            {
                if (worker.IsDone())
                {
                    var output = worker.PeekOutput();
                    worker.Reset(destroyTexture);
                    return new CoachResult(Labels, output);
                }
            }
            return null;
        }

        /// <summary>
        /// Disposes of all workers and associated resources
        /// </summary>
        public void CleanUp()
        {
            foreach (var worker in Workers)
            {
                worker.Dispose(true);
            }
        }
    }

    [Serializable]
    public class StatusDef
    {
        public string _short;
        public string _long;
    }

    [Serializable]
    public class ModelDef
    {
        public string name;
        // public StatusDef status;
        public int version;
        public string module;
        public string[] labels;
        public float coachVersion;

        public static ModelDef FromJson(string jsonString)
        {
            return JsonUtility.FromJson<ModelDef>(jsonString);
        }
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class Profile
    {
        public string bucket;
        public ModelDef[] models;

        public static Profile FromJson(string jsonString)
        {
            return JsonUtility.FromJson<Profile>(jsonString);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public enum ModelType
    {
        Frozen, Unity, Mobile
    }

    public class CoachClient
    {
        public bool IsDebug { get; private set; }
        private Profile Profile { get; set; }
        private string ApiKey { get; set; }

        ///<summary>
        ///<para>If true, additional logs will be displayed</para>
        ///</summary>
        public CoachClient(bool isDebug = false)
        {
            this.IsDebug = isDebug;
        }

        ///<summary>
        ///Authenticates with Coach service and allows for model caching. Accepts API Key as its only parameter
        ///<param>Your API key</param>
        ///</summary>
        public async Task<CoachClient> Login(string apiKey)
        {
            if (apiKey == String.Empty)
            {
                throw new Exception("Invalid API Key");
            }
            this.ApiKey = apiKey;
            this.Profile = await GetProfile();

            return this;
        }

        private bool IsAuthenticated()
        {
            return this.Profile != null;
        }

        private ModelDef ReadManifest(string path)
        {
            var json = File.ReadAllText(path);
            return ModelDef.FromJson(json);
        }

        private async Task<Profile> GetProfile()
        {
            var id = this.ApiKey.Substring(0, 5);
            var url = $"https://x27xyu10z1.execute-api.us-east-1.amazonaws.com/latest/profile?id={id}";

            var responseBody = await Networking.GetTextAsync(url, ApiKey);

            var profile = Profile.FromJson(responseBody);
            return profile;
        }

        ///<summary>
        ///Downloads model from Coach service to disk
        ///<param>Name of model</param>
        ///<param>Path to cache model</param>
        ///<param>If true, the download will be skipped if the model of the same filename already exists</param>
        ///<param>The type of model to be cached, default is Unity</param>
        ///</summary>
        public async Task CacheModel(string modelName, string path = ".", bool skipMatch = true, ModelType modelType = ModelType.Unity)
        {
            if (path == ".")
            {
                path = Application.persistentDataPath;
            }

            if (!IsAuthenticated())
                throw new Exception("User is not authenticated");

            ModelDef model = this.Profile.models.SingleOrDefault(m => m.name == modelName);
            if (model == null)
                throw new Exception($"{modelName} is an invalid model");

            int version = model.version;

            string modelDir = Path.Combine(path, modelName);
            string profileManifest = Path.Combine(modelDir, "manifest.json");

            if (File.Exists(profileManifest))
            {
                // Load existing model manifest
                ModelDef manifest = ReadManifest(profileManifest);

                int manifestVersion = manifest.version;
                int profileVersion = model.version;

                if (profileVersion == manifestVersion && skipMatch)
                {
                    if (this.IsDebug)
                    {
                        Console.WriteLine("Version match, skipping model download");
                    }
                    return;
                }
            }
            else if (!Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }

            // Write downloaded manifest
            var json = model.ToJson();
            File.WriteAllText(profileManifest, json);

            var baseUrl = $"https://la41byvnkj.execute-api.us-east-1.amazonaws.com/prod/{this.Profile.bucket}/model-bin?object=trained/{modelName}/{version}/model";

            string modelFile = String.Empty;
            if (modelType == ModelType.Frozen)
            {
                modelFile = "frozen.pb";
            }
            else if (modelType == ModelType.Unity)
            {
                modelFile = "unity.bytes";

            }
            else if (modelType == ModelType.Mobile)
            {
                modelFile = "mobile.tflite";
            }

            var modelUrl = $"{baseUrl}/{modelFile}";

            byte[] modelBytes = await Networking.GetContentAsync(modelUrl, ApiKey);

            var writePath = Path.Combine(path, modelName, modelFile);
            File.WriteAllBytes(writePath, modelBytes);
        }

        ///<summary>
        ///Loads model into memory
        ///<param>Path to the model</param>
        ///</summary>
        public CoachModel GetModel(string path, int workers = 1, string input = "input", string output = "output")
        {
            var modelPath = Path.Combine(path, "unity.bytes");
            var labelPath = Path.Combine(path, "manifest.json");

            // Load the model
            Model model = ModelLoader.LoadFromStreamingAssets(modelPath);

            var manifest = ReadManifest(labelPath);

            string[] labels = manifest.labels;
            string baseModule = manifest.module;
            float coachVersion = manifest.coachVersion;

            return new CoachModel(model, labels, baseModule, coachVersion, workers, input, output);
        }

        ///<summary>
        ///Downloads model from Coach service to disk, and loads it into memory
        ///<param>Name of model</param>
        ///<param>Path to cache the model. Application.persistentDataPath by default</param>
        ///</summary>
        public async Task<CoachModel> GetModelRemote(string modelName, string path = ".", int workers = 1, string input = "input", string output = "output")
        {
            if (path == ".")
            {
                path = Application.persistentDataPath;
            }

            await CacheModel(modelName, path);
            return GetModel(Path.Combine(path, modelName), workers, input, output);
        }
    }
}