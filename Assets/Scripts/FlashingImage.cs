using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the flashing of an image.
/// </summary>
public class FlashingImage : MonoBehaviour
{
	[Header("Flashing Settings")]
	public Image imageToFlash;
	public float flashSpeed = 1.0f;
	private float flashTimer = 0.0f;
	private bool isFlashing = true; // Starting with false will flash image immediately

	void Update()
	{
		if (isFlashing)
		{
			flashTimer += Time.deltaTime * flashSpeed;
			float alpha = (Mathf.Sin(flashTimer) + 1) / 2; // Ranges from 0 to 1
			SetImageAlpha(alpha);
		}
	}

	private void SetImageAlpha(float alpha)
	{
		if (imageToFlash != null) // Safety check
		{
			imageToFlash.color = new Color(imageToFlash.color.r, imageToFlash.color.g, imageToFlash.color.b, alpha);
		}
	}

	public void StartFlashing()
	{
		isFlashing = true;
		flashTimer = 0.0f; 
	}

	public void StopFlashing()
	{
		isFlashing = false;
		SetImageAlpha(1); // Ensures the image is fully visible when not flashing
	}
}
