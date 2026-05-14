using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 60f;
    [SerializeField] private float _resultShowDuration = 5f;
    [SerializeField] private float _countdownDuration = 5f;

    [Header("Game State")]
    public readonly SyncVar<GameState> CurrentState = new(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new(0);
    public readonly SyncVar<float> MatchTimer = new(0f);
    public readonly SyncVar<float> CountdownTimer = new(0f);
    public readonly SyncVar<string> ResultsText = new(string.Empty);

    public static event System.Action<string> OnLocalResultsTextChanged;

    public enum GameState
    {
        WaitingForPlayers,
        StartingSoon,
        InProgress,
        ShowingResults
    }

    public static GameManager Instance { get; private set; }

    // UI
    public delegate void GameStateChangedHandler(GameState newState);
    public static event GameStateChangedHandler OnLocalGameStateChanged;

    public delegate void ConnectedPlayersChangedHandler(int players);
    public static event ConnectedPlayersChangedHandler OnLocalConnectedPlayersChanged;

    public delegate void MatchTimerChangedHandler(float time);
    public static event MatchTimerChangedHandler OnLocalMatchTimerChanged;

    public delegate void CountdownTimerChangedHandler(float time);
    public static event CountdownTimerChangedHandler OnLocalCountdownTimerChanged;

    private bool _countdownInProgress = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnStartNetwork()
    {
        Debug.Log($"[GameManager] OnStartNetwork - IsServer: {base.IsServerInitialized}, IsClient: {base.IsClientInitialized}");

        if (!base.IsServerInitialized)
        {
            Debug.Log("[GameManager] Running on client - not subscribing to server events");
            return;
        }

        base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
        UpdateConnectedPlayersCount();
        Debug.Log($"[GameManager] Server started. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");
    }

    public override void OnStartServer()
    {
        Debug.Log("[GameManager] OnStartServer - subscribing to server events");

        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.OnChange += OnMatchTimerChanged;
        CountdownTimer.OnChange += OnCountdownTimerChanged;
        ResultsText.OnChange += OnResultsTextChanged;

        base.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
        UpdateConnectedPlayersCount();
        Debug.Log($"[GameManager] Server started. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
        MatchTimer.OnChange -= OnMatchTimerChanged;
        CountdownTimer.OnChange -= OnCountdownTimerChanged;

        if (base.IsServerInitialized)
            base.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
    }

    public override void OnStartClient()
    {
        Debug.Log("[GameManager] OnStartClient - subscribing to client events");
        CurrentState.OnChange += OnGameStateChanged;
        ConnectedPlayers.OnChange += OnConnectedPlayersChanged;
        MatchTimer.OnChange += OnMatchTimerChanged;
        CountdownTimer.OnChange += OnCountdownTimerChanged;
        ResultsText.OnChange += OnResultsTextChanged;
    }

    public override void OnStopClient()
    {
        CurrentState.OnChange -= OnGameStateChanged;
        ConnectedPlayers.OnChange -= OnConnectedPlayersChanged;
        MatchTimer.OnChange -= OnMatchTimerChanged;
        CountdownTimer.OnChange -= OnCountdownTimerChanged;
        ResultsText.OnChange -= OnResultsTextChanged;
    }

    private void UpdateConnectedPlayersCount()
    {
        if (base.IsServerInitialized)
            ConnectedPlayers.Value = base.ServerManager.Clients.Count;
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (!base.IsServerInitialized) return;

        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            UpdateConnectedPlayersCount();
            if (CurrentState.Value == GameState.WaitingForPlayers
                && ConnectedPlayers.Value >= _requiredPlayers
                && !_countdownInProgress)
            {
                StartCountdown();
            }
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            StartCoroutine(DelayedUpdatePlayersCount());
        }
    }

    private IEnumerator DelayedUpdatePlayersCount()
    {
        yield return new WaitForSeconds(0.5f);
        UpdateConnectedPlayersCount();
        if (CurrentState.Value == GameState.StartingSoon
            && ConnectedPlayers.Value < _requiredPlayers)
        {
            CancelCountdown();
        }
    }

    private void CancelCountdown()
    {
        _countdownInProgress = false;
        CountdownTimer.Value = 0f;
        CurrentState.Value = GameState.WaitingForPlayers;
    }

    private void Update()
    {
        // На клиенте Update не нужен для таймеров - сервер обновляет SyncVar
        if (!base.IsServerInitialized)
            return;

        GameState state;
        try
        {
            state = CurrentState.Value;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameManager] Error getting state: {e.Message}");
            return;
        }

        if (state == GameState.StartingSoon)
        {
            if (ConnectedPlayers.Value < _requiredPlayers)
            {
                CancelCountdown();
                return;
            }
            CountdownTimer.Value -= Time.deltaTime;
            if (CountdownTimer.Value <= 0f)
            {
                CountdownTimer.Value = 0f;
                StartMatch();
            }
        }
        else if (state == GameState.InProgress)
        {
            MatchTimer.Value -= Time.deltaTime;
            if (MatchTimer.Value <= 0f)
            {
                MatchTimer.Value = 0f;
                EndMatch();
            }
        }
    }

    private void StartCountdown()
    {
        _countdownInProgress = true;
        CurrentState.Value = GameState.StartingSoon;
        CountdownTimer.Value = _countdownDuration;
        Debug.Log($"[GameManager] Countdown started: {_countdownDuration}s");
    }

    private void StartMatch()
    {
        _countdownInProgress = false;
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;

        // (HP, Ammo)
        ResetAllPlayers();

        Debug.Log("[GameManager] Match started!");
    }

    private void ResetAllPlayers()
    {
        if (!base.IsServerInitialized) return;

        // SpawnPointsHolder
        Transform[] spawnPoints = SpawnPointsHolder.Instance?.Points;
        int spawnCount = spawnPoints?.Length ?? 0;

        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    pn.HP.Value = 100;
                    pn.IsAlive.Value = true;
                    pn.Ammo.Value = 20; // 

                    
                    if (spawnCount > 0)
                    {
                        int idx = Random.Range(0, spawnCount);
                        CharacterController cc = nob.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        nob.transform.position = spawnPoints[idx].position;
                        if (cc != null) cc.enabled = true;
                    }
                }
            }
        }
    }

    private void EndMatch()
    {
        CurrentState.Value = GameState.ShowingResults;

        string results = GenerateResultsText();
        ResultsText.Value = results;

        Debug.Log("[GameManager] Match ended. Showing results...");
        Debug.Log(results);
        Invoke(nameof(ResetToLobby), _resultShowDuration);
    }

    private string GenerateResultsText()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== РЕЗУЛЬТАТЫ ===");

        var scores = new System.Collections.Generic.List<(string name, int score)>();

        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    scores.Add((pn.Nickname.Value, pn.Score.Value));
                }
            }
        }

        scores.Sort((a, b) => b.score.CompareTo(a.score));

        for (int i = 0; i < scores.Count; i++)
        {
            string prefix = (i == 0) ? "👑 " : $"{i + 1}. ";
            sb.AppendLine($"{prefix}{scores[i].name}: {scores[i].score} очков");
        }

        return sb.ToString();
    }

    private void ResetToLobby()
    {
        MatchTimer.Value = 0f;
        CountdownTimer.Value = 0f;
        _countdownInProgress = false;

        foreach (var conn in base.ServerManager.Clients.Values)
        {
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                    pn.ResetScore();
            }
        }

        CurrentState.Value = GameState.WaitingForPlayers;
        Debug.Log($"[GameManager] Return to lobby. Players: {ConnectedPlayers.Value}/{_requiredPlayers}");

        if (ConnectedPlayers.Value >= _requiredPlayers)
            StartCountdown();
        else
            Debug.Log("[GameManager] Waiting for more players...");
    }

    private void OnGameStateChanged(GameState oldValue, GameState newValue, bool asServer)
    {
        Debug.Log($"[GameManager] State: {oldValue} -> {newValue}");
        OnLocalGameStateChanged?.Invoke(newValue);
    }

    private void OnConnectedPlayersChanged(int oldValue, int newValue, bool asServer)
    {
        OnLocalConnectedPlayersChanged?.Invoke(newValue);
    }

    private void OnMatchTimerChanged(float oldValue, float newValue, bool asServer)
    {
        OnLocalMatchTimerChanged?.Invoke(newValue);
    }

    private void OnCountdownTimerChanged(float oldValue, float newValue, bool asServer)
    {
        OnLocalCountdownTimerChanged?.Invoke(newValue);
    }

    private void OnResultsTextChanged(string oldValue, string newValue, bool asServer)
    {
        if (!string.IsNullOrEmpty(newValue))
            OnLocalResultsTextChanged?.Invoke(newValue);
    }
}