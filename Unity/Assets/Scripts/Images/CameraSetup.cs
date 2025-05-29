using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    private GameObject imagePlane;

    public void SetCamera()
    {
        // Create a camera
        //GameObject cameraObject = new GameObject("Camera");
        Camera camera = Camera.main;

        // Position the camera above the image plane
        camera.transform.position = new Vector3(imagePlane.transform.position.x, imagePlane.transform.position.y + 20, imagePlane.transform.position.z);

        // Rotate the camera to look down at the image plane
        camera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    public void SetPlane(GameObject p)
    {
        imagePlane = p;
    }
}
