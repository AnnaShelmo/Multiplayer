using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;

    public static string PlayerNickname { get; private set; } = "Player";
    [SerializeField] private GameObject _menuRoot;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
    }

    private void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _menuRoot.SetActive(false);
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        NetworkManager.Singleton.StartHost();
    }

    public void StartAsClient()
    {
        SaveNickname();
        NetworkManager.Singleton.StartClient();
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue)
            ? "Player"
            : rawValue.Trim();
    }
}