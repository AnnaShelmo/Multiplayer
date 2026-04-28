using TMPro;
using UnityEngine;
using FishNet.Managing;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuRoot;

    public static string PlayerNickname { get; private set; } = "Player";

    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = FindFirstObjectByType<NetworkManager>();

        _networkManager.ClientManager.OnClientConnectionState += OnClientState;
    }

    private void OnClientState(FishNet.Transporting.ClientConnectionStateArgs obj)
    {
        if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            _menuRoot.SetActive(false);
        }
    }

    public void StartAsHost()
    {
        SaveNickname();
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
    }

    public void StartAsClient()
    {
        SaveNickname();
        _networkManager.ClientManager.StartConnection();
    }

    private void SaveNickname()
    {
        string raw = _nicknameInput != null ? _nicknameInput.text : "";
        PlayerNickname = string.IsNullOrWhiteSpace(raw) ? "Player" : raw.Trim();
    }
}