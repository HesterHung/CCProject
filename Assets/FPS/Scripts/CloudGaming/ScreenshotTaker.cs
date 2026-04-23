using UnityEngine;
using UnityEngine.InputSystem; // Add this line

public class ScreenshotTaker : MonoBehaviour
{
    void Update()
    {
        // New Input System check for the 'G' key
        if (Keyboard.current.gKey.wasPressedThisFrame) 
        {
            // Saves to the project root folder (next to Assets)
            string fileName = "Ground_Truth_1080p.png";
            ScreenCapture.CaptureScreenshot(fileName);
            
            Debug.Log("📸 Ground Truth Saved to Project Root: " + fileName);
        }
    }
}