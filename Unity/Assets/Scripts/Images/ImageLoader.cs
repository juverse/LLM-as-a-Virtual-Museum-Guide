using UnityEngine;
using System.IO;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ImageLoader : MonoBehaviour
{
    public GameObject imagePrefab; // Assign your plane prefab here
    public Vector3 position = new Vector3(0, 0, 0); // Position for the plane
    private GameObject planeInstance;
    private Texture2D texture;

    public void LoadImage(string imagePath)
    {
        // Check if the image file exists
        if (File.Exists(imagePath))
        {
            // Load the image file into a Texture2D
            byte[] fileData = File.ReadAllBytes(imagePath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);

            // Instantiate the prefab and set position
            planeInstance = Instantiate(imagePrefab, position, Quaternion.identity);

            // Adjust aspect ratio of the plane
            AdjustAspectRatio(planeInstance, texture);

            // Set the texture on the plane's material
            planeInstance.GetComponent<Renderer>().material.mainTexture = texture;

            // Ensure XRGrabInteractable is set up (in case it's not already in the prefab)
            if (!planeInstance.TryGetComponent(out XRGrabInteractable grabInteractable))
            {
                grabInteractable = planeInstance.AddComponent<XRGrabInteractable>();
            }
        }
        else
        {
            Debug.LogError("Image file not found at " + imagePath);
        }
    }

    void AdjustAspectRatio(GameObject plane, Texture2D texture)
    {
        float aspectRatio = (float)texture.width / texture.height;
        plane.transform.localScale = new Vector3(aspectRatio, 1, 1); // Scale to maintain aspect ratio
    }

    public GameObject GetPlaneInstance()
    {
        return planeInstance;
    }

    public Texture GetTexture()
    {
        return texture;
    }
}
