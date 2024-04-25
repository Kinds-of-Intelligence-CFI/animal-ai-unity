using TMPro;
using UnityEngine;

/// <summary>
/// This class is used to display the build version on the screen. It's called once and it's used to display the build version on the screen automatically.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class BuildVersionControl : MonoBehaviour
{
	void Awake()
	{
		TMP_Text BuildVersionText = GetComponent<TMP_Text>();
		if (BuildVersionText != null)
		{
			BuildVersionText.text = "Version: " + Application.version;
		}
		else
		{
			Debug.LogError("TextMeshPro component not found on the GameObject.");
		}
	}
}
