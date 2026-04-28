using UnityEngine;
using System.Collections;
using FishNet.Managing;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform[] _points;
    [SerializeField] private float _delay = 3f;

    private NetworkManager _nm;

    private void Start()
    {
        _nm = FindFirstObjectByType<NetworkManager>();

        if (_nm == null)
        {
            Debug.LogError("NetworkManager not found!");
            return;
        }

        _nm.ServerManager.OnServerConnectionState += OnServer;
    }

    private void OnDestroy()
    {
        if (_nm != null)
            _nm.ServerManager.OnServerConnectionState -= OnServer;
    }

    private void OnServer(FishNet.Transporting.ServerConnectionStateArgs obj)
    {
        if (obj.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
            SpawnAll();
    }

    private void SpawnAll()
    {
        foreach (var p in _points)
            Spawn(p.position);
    }

    public void OnPickedUp(Vector3 pos)
    {
        StartCoroutine(Respawn(pos));
    }

    private IEnumerator Respawn(Vector3 pos)
    {
        yield return new WaitForSeconds(_delay);
        Spawn(pos);
    }

    private void Spawn(Vector3 pos)
    {
        var go = Instantiate(_prefab, pos, Quaternion.identity);

        var pickup = go.GetComponent<HealthPickup>();
        if (pickup != null)
            pickup.Init(this);

        _nm.ServerManager.Spawn(go);
    }
}