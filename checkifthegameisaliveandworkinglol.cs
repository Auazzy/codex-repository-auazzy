// GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public void Start()
    {
        Debug.Log("Map loaded: " + GameSettings.mapName);
        Debug.Log("Gamemode: " + GameSettings.gamemode);

        GamemodeManager.Instance.StartGamemode();
    }
}
