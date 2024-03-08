using System;
using UnityEngine;
using TMPro;
using System.IO;

public class UIManager : MonoBehaviour
{
	public TMP_Dropdown arenaDropdown;
	public TextMeshProUGUI fileNameText;
	public AAI3EnvironmentManager environmentManager;
	private bool isDropdownVisible = false;


	void Awake()
	{
		AAI3EnvironmentManager.OnArenaChanged += UpdateArenaUI;
	}

	void OnDestroy()
	{
		AAI3EnvironmentManager.OnArenaChanged -= UpdateArenaUI;
		arenaDropdown.gameObject.SetActive(false);
	}

	private void Start()
	{
		UpdateArenaUI(environmentManager.GetCurrentArenaIndex(), environmentManager.GetTotalArenas());
	}

	private void UpdateArenaUI(int currentArenaIndex, int totalArenas)
	{
		arenaDropdown.ClearOptions();
		for (int i = 0; i < totalArenas; i++)
		{
			arenaDropdown.options.Add(new TMP_Dropdown.OptionData($"Arena {i + 1} of {totalArenas}"));
		}
		arenaDropdown.value = currentArenaIndex;
		arenaDropdown.RefreshShownValue();

		fileNameText.text = $"File: {Path.GetFileName(environmentManager.configFile)}";
	}

	public void ToggleDropdown()
	{
		if (arenaDropdown != null)
		{
			arenaDropdown.Show();
		}
	}

	public void ToggleDropdownVisibility()
	{
		isDropdownVisible = !isDropdownVisible;
		arenaDropdown.gameObject.SetActive(isDropdownVisible);
	}


}
