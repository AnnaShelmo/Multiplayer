using Unity.Netcode;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;

    private float _lastShotTime;
    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!_playerNetwork.IsAlive.Value) return;

        if (Input.GetKeyDown(KeyCode.Space))
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir,
                                ServerRpcParams rpc = default)
    {
        if (!_playerNetwork.IsAlive.Value) return;
        if (_playerNetwork.Ammo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _playerNetwork.Ammo.Value--;


        var go = Instantiate(_projectilePrefab,
                             pos + dir * 1.2f,
                             Quaternion.LookRotation(dir));

        go.GetComponent<NetworkObject>()
          .SpawnWithOwnership(rpc.Receive.SenderClientId);
    }

}