using System.IO;
using System;
using UnityEngine;

public class ScreenshotCapture : MonoBehaviour
{
    public void TakeScreenshot()
    {
        StartCoroutine(CaptureAndSaveScreenshot());
    }

    private System.Collections.IEnumerator CaptureAndSaveScreenshot()
    {
        // Wait until the end of the frame to capture the screenshot
        yield return new WaitForEndOfFrame();

        // Capture the screenshot as a Texture2D
        Texture2D screenshotTexture = ScreenCapture.CaptureScreenshotAsTexture();

        // Encode texture to JPG format
        byte[] jpgData = screenshotTexture.EncodeToJPG();

        // Create a unique filename using a timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Format: YYYYMMDD_HHMMSS
        string filename = $"Screenshot_{timestamp}.jpg";

        // Define the path to save the screenshot
        string filePath = Path.Combine(Application.persistentDataPath, filename);

        // Save the JPG file to the path
        File.WriteAllBytes(filePath, jpgData);
        Debug.Log($"Screenshot saved to: {filePath}");

        // Clean up the texture from memory
        Destroy(screenshotTexture);
    }
}