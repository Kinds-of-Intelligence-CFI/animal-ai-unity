using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class UIManager : MonoBehaviour
{
	public TMP_Dropdown arenaDropdown;
	public TextMeshProUGUI fileNameText;
	public AAI3EnvironmentManager environmentManager;

	void Start()
	{
		PopulateArenaDropdown();
	}

	void PopulateArenaDropdown()
	{
		List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();

		int totalArenas = environmentManager.GetTotalArenas();
		for (int i = 0; i < totalArenas; i++)
		{
			dropdownOptions.Add(new TMP_Dropdown.OptionData($"Arena {i + 1} of {totalArenas}")); // Format as "Arena X of Y"
		}

		arenaDropdown.ClearOptions();
		arenaDropdown.AddOptions(dropdownOptions);

		int currentArenaIndex = environmentManager.GetCurrentArenaIndex();
		arenaDropdown.value = currentArenaIndex;

		fileNameText.text = $"File: {Path.GetFileName(environmentManager.configFile)}";
	}

}
