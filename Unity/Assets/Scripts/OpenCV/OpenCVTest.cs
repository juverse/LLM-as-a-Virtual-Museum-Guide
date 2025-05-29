using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;
using System.Collections.Generic;
using OpenCvSharp.XFeatures2D;

public class WebCamImageDetection : MonoBehaviour
{
    public RawImage cameraView; // Display the webcam feed
    //public Text detectionResult; // Display the detection result
    public Texture2D[] predefinedImages; // Reference images

    private WebCamTexture webcamTexture;
    private List<KeyPoint[]> imageKeypoints = new List<KeyPoint[]>();
    private List<Mat> imageDescriptors = new List<Mat>();
    private List<string> imageNames = new List<string>();
    private SIFT sift;
    private BFMatcher matcher;

    void Start()
    {
        // Initialize ORB and matcher
        sift = SIFT.Create();
        matcher = new BFMatcher(NormTypes.L2, crossCheck: false);

        // Load predefined images and compute features
        foreach (var texture in predefinedImages)
        {
            Mat imgMat = TextureToMat(texture);
            Mat grayImage = new Mat();
            Cv2.CvtColor(imgMat, grayImage, ColorConversionCodes.BGR2GRAY);

            var keypoints = sift.Detect(grayImage);
            var descriptors = new Mat();
            sift.Compute(grayImage, ref keypoints, descriptors);

            imageKeypoints.Add(keypoints);
            imageDescriptors.Add(descriptors);
            imageNames.Add(texture.name);
        }

        // Initialize webcam
        if (WebCamTexture.devices.Length > 0)
        {
            webcamTexture = new WebCamTexture();
            webcamTexture.Play();
            cameraView.texture = webcamTexture;
        }
        else
        {
            Debug.LogError("No webcam detected.");
        }
    }

    void Update()
    {
        if (webcamTexture == null || !webcamTexture.isPlaying || !webcamTexture.didUpdateThisFrame)
            return;

        // Convert webcam frame to OpenCvSharp Mat
        Mat cameraFrame = TextureToMat(webcamTexture);
        Mat grayFrame = new Mat();
        Cv2.CvtColor(cameraFrame, grayFrame, ColorConversionCodes.BGR2GRAY);

        // Detect features in the current frame
        var frameKeypoints = sift.Detect(grayFrame);
        Mat frameDescriptors = new Mat();
        sift.Compute(grayFrame, ref frameKeypoints, frameDescriptors);

        // Match with predefined images
        string detectedImage = null;
        double bestConfidence = 0.0;

        for (int i = 0; i < imageDescriptors.Count; i++)
        {
            var matches = matcher.KnnMatch(imageDescriptors[i], frameDescriptors, k: 2);

            // Apply ratio test
            var goodMatches = new List<DMatch>();
            foreach (var match in matches)
            {
                if (match.Length >= 2 && match[0].Distance < 0.75 * match[1].Distance)
                {
                    goodMatches.Add(match[0]);
                }
            }

            // Calculate confidence
            double confidence = goodMatches.Count / (double)imageKeypoints[i].Length;
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                detectedImage = imageNames[i];
            }
        }

        // Display detection result
        if (bestConfidence > 0.1) // Adjust threshold as needed
        {
            Debug.Log($"Detected: {detectedImage} with confidence: {bestConfidence:F2}");
        }
        else
        {
            Debug.Log("No match detected.");
        }
    }

    private Mat TextureToMat(Texture2D texture)
    {
        return OpenCvSharp.Unity.TextureToMat(texture);
    }

    private Mat TextureToMat(WebCamTexture webcam)
    {
        Texture2D texture = new Texture2D(webcam.width, webcam.height, TextureFormat.RGBA32, false);
        texture.SetPixels(webcam.GetPixels());
        texture.Apply();
        return OpenCvSharp.Unity.TextureToMat(texture);
    }

    void OnDestroy()
    {
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
}
