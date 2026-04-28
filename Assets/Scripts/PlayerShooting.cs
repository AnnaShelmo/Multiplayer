using FishNet.Object;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;

    private float _lastShotTime;
    private PlayerNetwork _playerNetwork;

    public override void OnStartNetwork()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!base.IsOwner) return;
        if (!_playerNetwork.IsAlive.Value) return;

        if (Input.GetKeyDown(KeyCode.Space))
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (!_playerNetwork.IsAlive.Value) return;
        if (_playerNetwork.Ammo.Value <= 0) return;
        if (Time.time < _lastShotTime + _cooldown) return;

        _lastShotTime = Time.time;
        _playerNetwork.Ammo.Value--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        base.Spawn(go, Owner);
    }
}