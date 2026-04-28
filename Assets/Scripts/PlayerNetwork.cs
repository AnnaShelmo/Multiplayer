using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    public readonly SyncVar<string> Nickname = new("Player");
    public readonly SyncVar<int> HP = new(100);
    public readonly SyncVar<bool> IsAlive = new(true);
    public readonly SyncVar<int> Ammo = new(20);
    public readonly SyncVar<int> RespawnTimer = new(0);

    [SerializeField] private GameObject _model;
    [SerializeField] private float _respawnDelay = 7f;
    [SerializeField] private int _maxAmmo = 20;

    private Transform[] _spawnPoints;

    public override void OnStartNetwork()
    {
        if (SpawnPointsHolder.Instance != null)
            _spawnPoints = SpawnPointsHolder.Instance.Points;

        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (base.IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public override void OnStopNetwork()
    {
        HP.OnChange -= OnHpChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
    }

    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        Nickname.Value = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{OwnerId}"
            : nickname.Trim();
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (!base.IsServerInitialized) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        if (_model != null)
            _model.SetActive(next);
    }

    private IEnumerator RespawnRoutine()
    {
        int t = Mathf.RoundToInt(_respawnDelay);

        while (t > 0)
        {
            RespawnTimer.Value = t;
            yield return new WaitForSeconds(1f);
            t--;
        }

        RespawnTimer.Value = 0;

        if (_spawnPoints.Length > 0)
        {
            int i = Random.Range(0, _spawnPoints.Length);

            var cc = GetComponent<CharacterController>();
            cc.enabled = false;
            transform.position = _spawnPoints[i].position;
            cc.enabled = true;
        }

        HP.Value = 100;
        Ammo.Value = _maxAmmo;
        IsAlive.Value = true;
    }
}