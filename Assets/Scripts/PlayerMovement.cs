using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;

    private CharacterController _cc;
    private PlayerNetwork _net;

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

        // 👇 Владелец отправляет ввод
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

        // 👇 Сервер шлёт состояние
        if (base.IsServerInitialized)
        {
            ReconcileData rd = new ReconcileData
            {
                Position = transform.position
            };

            Reconcile(rd);
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
        _cc.enabled = false;
        transform.position = rd.Position;
        _cc.enabled = true;
    }

    public override void CreateReconcile()
    {
        ReconcileData rd = new ReconcileData
        {
            Position = transform.position
        };

        Reconcile(rd);
    }
}