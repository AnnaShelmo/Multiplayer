using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _cooldown = 0.4f;

    private uint _lastShotTick;
    private float _clientCooldownTimer;
    private PlayerNetwork _playerNetwork;
    private GameManager _gm;

    public override void OnStartNetwork()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
        _gm = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (!base.IsOwner) return;
        if (!_playerNetwork.IsAlive.Value) return;
        if (_gm != null && _gm.CurrentState.Value != GameManager.GameState.InProgress) return;

        if (_clientCooldownTimer > 0)
            _clientCooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && _clientCooldownTimer <= 0)
        {
            _clientCooldownTimer = _cooldown;
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir)
    {
        if (!_playerNetwork.IsAlive.Value) return;
        if (_playerNetwork.Ammo.Value <= 0) return;

        uint currentTick = base.TimeManager.Tick;
        if (currentTick - _lastShotTick < (uint)(_cooldown / (float)base.TimeManager.TickDelta)) return;

        _lastShotTick = currentTick;
        _playerNetwork.Ammo.Value--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        var projectile = go.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Initialize(_playerNetwork);
        base.Spawn(go, Owner);
    }
}