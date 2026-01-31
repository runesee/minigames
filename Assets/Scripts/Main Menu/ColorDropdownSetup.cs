using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Dropdown))]
public class ColorDropdownSetup : MonoBehaviour
{
    private void Start()
    {
        Dropdown dropdown = GetComponent<Dropdown>();
        
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        
        for (int i = 0; i < PlayerColorManager.ColorNames.Length; i++)
        {
            options.Add(new Dropdown.OptionData(PlayerColorManager.ColorNames[i]));
        }
        
        dropdown.options = options;
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }
}
