using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> IsAlive = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // ПАТРОНЫ
    public NetworkVariable<int> Ammo = new(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // ТАЙМЕР РЕСПАВНА
    public NetworkVariable<int> RespawnTimer = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    [SerializeField] private GameObject _model;
    [SerializeField] private float _respawnDelay = 7f;
    [SerializeField] private int _maxAmmo = 20;
    [SerializeField] private Transform[] _spawnPoints;

    public override void OnNetworkSpawn()
    {
        _spawnPoints = SpawnPointsHolder.Instance.Points;

        if (IsOwner)
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);

        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;
    }


    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname)
            ? $"Player_{OwnerClientId}"
            : nickname.Trim();

        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next)
    {
        if (!IsServer) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        int timeLeft = Mathf.RoundToInt(_respawnDelay);

        while (timeLeft > 0)
        {
            RespawnTimer.Value = timeLeft;
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        RespawnTimer.Value = 0;

        if (_spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _spawnPoints.Length);
            var cc = GetComponent<CharacterController>();
            cc.enabled = false;

            transform.position = _spawnPoints[idx].position;

            cc.enabled = true;
        }

        HP.Value = 100;
        Ammo.Value = _maxAmmo;
        IsAlive.Value = true;
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        if (_model != null)
            _model.SetActive(next);
    }
}