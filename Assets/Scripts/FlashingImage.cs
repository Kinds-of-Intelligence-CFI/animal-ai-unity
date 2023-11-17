using UnityEngine;
using UnityEngine.UI;

public class FlashingImage : MonoBehaviour
{
    public Image imageToFlash;
    public float flashSpeed = 1.0f;
    private float flashTimer = 0.0f;
    private bool isFlashing = true;

    void Update()
    {
        if (isFlashing)
        {
            flashTimer += Time.deltaTime * flashSpeed;
            float alpha = (Mathf.Sin(flashTimer) + 1) / 2; // Ranges from 0 to 1
            imageToFlash.color = new Color(imageToFlash.color.r, imageToFlash.color.g, imageToFlash.color.b, alpha);
        }
    }

    public void StartFlashing()
    {
        isFlashing = true;
    }

    public void StopFlashing()
    {
        isFlashing = false;
        imageToFlash.color = new Color(imageToFlash.color.r, imageToFlash.color.g, imageToFlash.color.b, 1); // Reset alpha to 1
    }
}
