using UnityEngine;
using TMPro;

public class MapButton : MonoBehaviour
{
    [Header("Map Data")]
    public string sceneName;
    public string displayName;
    public string displaySize;

    [Header("Visual")]
    public TMP_Text label;

    static MapButton currentSelected;

    void Awake()
    {
        SetSelected(false);
    }

    public void SelectMap()
    {
        // unselect previous
        if (currentSelected != null)
            currentSelected.SetSelected(false);

        currentSelected = this;
        SetSelected(true);

        // save selection
        GameSettings.mapScene = sceneName;
        GameSettings.mapName = displayName;
        GameSettings.mapSize = displaySize;

        // update UI
        FindObjectOfType<MenuController>().UpdateMapInfoUI();
    }

    void SetSelected(bool selected)
    {
        if (label == null) return;

        label.color = selected ? Color.yellow : Color.white;
    }
}
