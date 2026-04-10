using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;

    [SerializeField] private TMP_Text _ammoText;          
    [SerializeField] private TMP_Text _respawnText;       

    public override void OnNetworkSpawn()
    {
        _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged += OnHpChanged;
        _playerNetwork.Ammo.OnValueChanged += OnAmmoChanged;
        _playerNetwork.RespawnTimer.OnValueChanged += OnRespawnTimerChanged;

        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
        OnAmmoChanged(0, _playerNetwork.Ammo.Value);
    }

    public override void OnNetworkDespawn()
    {
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
        _playerNetwork.Ammo.OnValueChanged -= OnAmmoChanged;
        _playerNetwork.RespawnTimer.OnValueChanged -= OnRespawnTimerChanged;
    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _nicknameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        _hpText.text = "HP: " + newValue;
    }

    private void OnAmmoChanged(int oldValue, int newValue)
    {
        if (IsOwner)
            _ammoText.text = "Ammo: " + newValue;
    }

    private void OnRespawnTimerChanged(int oldValue, int newValue)
    {
        if (!IsOwner) return;

        _respawnText.gameObject.SetActive(newValue > 0);
        _respawnText.text = "Respawn in: " + newValue;
    }
}