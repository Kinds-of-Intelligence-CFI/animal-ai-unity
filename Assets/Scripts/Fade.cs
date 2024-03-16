using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the fade in and out of the screen.
/// </summary>
public class Fade : MonoBehaviour
{
	public float fadeSpeed = 0.25f;
	private int _fadeDirection = -1;
	private Image _image;
	private bool _play;

	void Awake()
	{
		var envManager = FindObjectOfType<AAI3EnvironmentManager>();
		_play = envManager ? envManager.PlayerMode : false;
		_image = GetComponentInChildren<Image>();
		ResetFade();
	}

	public void ResetFade()
	{
		_fadeDirection = -1;
		_image.color = new Color(0, 0, 0, 0);
	}

	public void StartFade()
	{
		StopAllCoroutines();
		_fadeDirection *= -1;
		if (_play)
		{
			StartCoroutine(FadeOutEnum());
		}
		else
		{
			_image.color = _fadeDirection < 0 ? new Color(0, 0, 0, 0) : new Color(0, 0, 0, 1);
		}
	}

	IEnumerator FadeOutEnum()
	{
		int localFadeDirection = _fadeDirection;
		float alpha = _image.color.a;
		while (localFadeDirection == _fadeDirection && alpha <= 1 && alpha >= 0)
		{
			alpha += _fadeDirection * Time.deltaTime * fadeSpeed;
			_image.color = new Color(0, 0, 0, Mathf.Clamp(alpha, 0, 1));
			yield return null;
		}
	}
}
