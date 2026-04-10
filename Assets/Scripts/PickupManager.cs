using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PickupManager : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 3f;

    private bool _spawned = false;

    private void Update()
    {
        if (_spawned) return;

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer)
        {
            Debug.Log("SERVER DETECTED → spawning pickups");
            SpawnAll();
            _spawned = true;
        }
    }

    private void SpawnAll()
    {
        foreach (var point in _spawnPoints)
            SpawnPickup(point.position);
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }

    private void SpawnPickup(Vector3 position)
    {
        var go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        go.GetComponent<HealthPickup>().Init(this);
        go.GetComponent<NetworkObject>().Spawn();
    }
}