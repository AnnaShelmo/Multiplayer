using FishNet.Object;
using TMPro;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _net;
    [SerializeField] private TMP_Text _nick;
    [SerializeField] private TMP_Text _hp;
    [SerializeField] private TMP_Text _ammo;
    [SerializeField] private TMP_Text _respawn;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Подписки
        _net.Nickname.OnChange += OnNicknameChanged;
        _net.HP.OnChange += OnHpChanged;
        _net.Ammo.OnChange += OnAmmoChanged;
        _net.RespawnTimer.OnChange += OnRespawnChanged;

        // ВАЖНО: начальная инициализация
        UpdateAllUI();

        // Показываем патроны только своему игроку
        _ammo.gameObject.SetActive(base.IsOwner);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        _net.Nickname.OnChange -= OnNicknameChanged;
        _net.HP.OnChange -= OnHpChanged;
        _net.Ammo.OnChange -= OnAmmoChanged;
        _net.RespawnTimer.OnChange -= OnRespawnChanged;
    }

    // ---------------- UI UPDATE ----------------

    private void UpdateAllUI()
    {
        OnNicknameChanged(default, _net.Nickname.Value, false);
        OnHpChanged(0, _net.HP.Value, false);
        OnAmmoChanged(0, _net.Ammo.Value, false);
        OnRespawnChanged(0, _net.RespawnTimer.Value, false);
    }

    private void OnNicknameChanged(string prev, string next, bool asServer)
    {
        _nick.text = next;
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        _hp.text = "HP: " + next;
    }

    private void OnAmmoChanged(int prev, int next, bool asServer)
    {
        if (!base.IsOwner) return;

        _ammo.text = "Ammo: " + next;
    }

    private void OnRespawnChanged(int prev, int next, bool asServer)
    {
        if (!base.IsOwner) return;

        _respawn.gameObject.SetActive(next > 0);
        _respawn.text = "Respawn: " + next;
    }
}