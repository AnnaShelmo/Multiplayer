using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private bool _useCSP = true;

    private CharacterController _cc;
    private PlayerNetwork _net;
    private Vector2 _latestServerInput; // Для режима без CSP: последний ввод от клиента (на сервере)

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _net = GetComponent<PlayerNetwork>();
    }

    public struct MoveData : IReplicateData
    {
        public float Horizontal;
        public float Vertical;

        private uint _tick;
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose() { }
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;

        private uint _tick;
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose() { }
    }

    public override void OnStartNetwork()
    {
        base.TimeManager.OnTick += OnTick;
    }

    public override void OnStopNetwork()
    {
        base.TimeManager.OnTick -= OnTick;
    }

    private void OnTick()
    {
        if (_net != null && !_net.IsAlive.Value)
            return;

        if (_useCSP)
        {
            // --- Режим CSP (предсказание) ---
            if (base.IsOwner)
            {
                MoveData md = new MoveData
                {
                    Horizontal = Input.GetAxisRaw("Horizontal"),
                    Vertical = Input.GetAxisRaw("Vertical")
                };
                Replicate(md);
            }
            else
            {
                Replicate(default);
            }

            if (base.IsServerInitialized)
            {
                ReconcileData rd = new ReconcileData
                {
                    Position = transform.position
                };
                Reconcile(rd);
            }
        }
        else
        {
            // --- Режим без CSP: сервер постоянно применяет последний ввод клиента ---
            if (base.IsServerInitialized && _latestServerInput.sqrMagnitude > 0.01f)
            {
                Vector3 move = new Vector3(_latestServerInput.x, 0, _latestServerInput.y).normalized;
                _cc.Move(move * _speed * (float)base.TimeManager.TickDelta);
            }
        }
    }

    [Replicate]
    private void Replicate(
        MoveData md,
        ReplicateState state = ReplicateState.Invalid,
        Channel channel = Channel.Unreliable)
    {
        Vector3 move = new Vector3(md.Horizontal, 0f, md.Vertical).normalized;

        _cc.Move(move * _speed * (float)base.TimeManager.TickDelta);
    }

    [Reconcile]
    private void Reconcile(
        ReconcileData rd,
        Channel channel = Channel.Unreliable)
    {
        if (Vector3.Distance(transform.position, rd.Position) > 0.1f)
        {
            _cc.enabled = false;
            transform.position = rd.Position;
            _cc.enabled = true;
        }
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position
        };

        Reconcile(rd);
    }

    private void Update()
    {
        // Если CSP включен — движение идет через OnTick/Replicate
        if (_useCSP) return;
        if (!base.IsOwner || !_net.IsAlive.Value) return;

        // Отправляем ввод на сервер каждый кадр (включая нулевой при отпускании клавиш)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveServerRpc(h, v);
    }

    [ServerRpc]
    private void MoveServerRpc(float h, float v)
    {
        if (!base.IsServerInitialized) return;
        // Сохраняем последний ввод от клиента (сервер будет применять его в OnTick)
        _latestServerInput = new Vector2(h, v);
    }

    // Метод для переключения CSP (для демонстрации)
    // Не забудь вручную переключить Owner Enabled в NetworkTransform на префабе
    public void ToggleCSP()
    {
        _useCSP = !_useCSP;
    }
}