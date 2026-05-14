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
    [SerializeField] private TMP_Text _score;

    private GameManager _gm;

    public override void OnStartClient()
    {
        base.OnStartClient();

        _gm = FindFirstObjectByType<GameManager>();

        _net.Nickname.OnChange += OnNicknameChanged;
        _net.HP.OnChange += OnHpChanged;
        _net.Ammo.OnChange += OnAmmoChanged;
        _net.RespawnTimer.OnChange += OnRespawnChanged;
        _net.Score.OnChange += OnScoreChanged;

        UpdateAllUI();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        _net.Nickname.OnChange -= OnNicknameChanged;
        _net.HP.OnChange -= OnHpChanged;
        _net.Ammo.OnChange -= OnAmmoChanged;
        _net.RespawnTimer.OnChange -= OnRespawnChanged;
        _net.Score.OnChange -= OnScoreChanged;
    }

    private void UpdateAllUI()
    {
        OnNicknameChanged(default, _net.Nickname.Value, false);
        OnHpChanged(0, _net.HP.Value, false);
        OnAmmoChanged(0, _net.Ammo.Value, false);
        OnRespawnChanged(0, _net.RespawnTimer.Value, false);
        OnScoreChanged(0, _net.Score.Value, false);
    }

    private void OnNicknameChanged(string prev, string next, bool asServer)
    {
        _nick.text = next;
    }

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        if (_gm != null && _gm.CurrentState.Value != GameManager.GameState.InProgress)
        {
            _hp.gameObject.SetActive(false);
            return;
        }
        _hp.gameObject.SetActive(true);
        _hp.text = "HP: " + next;
    }

    private void OnAmmoChanged(int prev, int next, bool asServer)
    {
        if (_gm != null && _gm.CurrentState.Value != GameManager.GameState.InProgress)
        {
            _ammo.gameObject.SetActive(false);
            return;
        }
        _ammo.gameObject.SetActive(true);
        _ammo.text = "Ammo: " + next;
    }

    private void Update()
    {
        if (!base.IsOwner) return;

        if (_gm != null && _gm.CurrentState.Value == GameManager.GameState.InProgress)
        {
            _hp.text = "HP: " + _net.HP.Value;
            _ammo.text = "Ammo: " + _net.Ammo.Value;
        }
    }

    private void OnRespawnChanged(int prev, int next, bool asServer)
    {
        if (!base.IsOwner) return;

        if (_gm != null && _gm.CurrentState.Value != GameManager.GameState.InProgress)
        {
            _respawn.gameObject.SetActive(false);
            return;
        }
        _respawn.gameObject.SetActive(next > 0);
        _respawn.text = "Respawn: " + next;
    }

    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        if (_score != null)
            _score.text = "Score: " + next;
    }
}