using TMPro;
using UnityEngine;

/// <summary>
/// This class is used to display the build version on the screen. Automatically retrieves the version data from project settings.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class BuildVersionControl : MonoBehaviour
{
	void Awake()
	{
		TMP_Text BuildVersionText = GetComponent<TMP_Text>();
		if (BuildVersionText != null)
		{
			BuildVersionText.text = "Build: " + Application.version;
		}
		else
		{
			Debug.LogError("TextMeshPro component not found on the GameObject.");
		}
	}
}
