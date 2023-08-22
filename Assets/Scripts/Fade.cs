using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
	public float fadeSpeed = 0.25f;
	private int _fadeDirection = -1;
	private Image _image;
	private bool _play;
	
	public void ResetFade()
	{
		_fadeDirection = -1;
		_image.color = new Color(0, 0, 0, 0);
	}

	public void StartFade()
	{
		_fadeDirection *= -1;
		if (_play)
		{
			StartCoroutine(FadeOutEnum());
		}
		else
		{
			if (_fadeDirection < 0)
			{
				_image.color = new Color(0, 0, 0, 0);
			}
			else
			{
				_image.color = new Color(0, 0, 0, 1);
			}
		}
	}

	IEnumerator FadeOutEnum()
	{
		int localFadeDirection = _fadeDirection;
		float alpha = _image.color.a;
		while (localFadeDirection == _fadeDirection && alpha <= 1 && alpha >= 0)
		{
			alpha += _fadeDirection * Time.deltaTime * fadeSpeed;
			_image.color = new Color(0, 0, 0, Math.Max(Math.Min(1, alpha), 0));
			yield return null;
		}
	}

	void Awake()
	{
		// TODO Please note that this function (FindObjectOfType) is very slow. It is not recommended to use this function every frame. In most cases you can use the singleton pattern instead.
		_play = FindObjectOfType<AAI3EnvironmentManager>().playerMode;
		_image = gameObject.GetComponentInChildren<Image>();
		ResetFade();
	}
	
}
