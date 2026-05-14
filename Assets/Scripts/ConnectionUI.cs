using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;
    [SerializeField] private GameObject _menuRoot;

    public static string PlayerNickname { get; private set; } = "Player";
    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = InstanceFinder.NetworkManager;
        if (_networkManager == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }
        _networkManager.ClientManager.OnClientConnectionState += OnClientState;

        if (Application.isBatchMode)
        {
            Debug.Log("[SERVER] Headless -> starting server");
            _networkManager.ServerManager.StartConnection();
        }
    }

    private void OnClientState(ClientConnectionStateArgs obj)
    {
        Debug.Log("Client state: " + obj.ConnectionState);
        if (obj.ConnectionState == LocalConnectionState.Started)
            _menuRoot.SetActive(false);
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