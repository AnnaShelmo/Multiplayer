using FishNet.Object;
using UnityEngine;

public class HealthPickup : NetworkBehaviour
{
    [SerializeField] private int _healAmount = 40;

    private PickupManager _manager;
    private Vector3 _pos;

    public void Init(PickupManager m)
    {
        _manager = m;
        _pos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        var p = other.GetComponent<PlayerNetwork>();
        if (p == null) return;
        if (!p.IsAlive.Value) return;
        if (p.HP.Value >= 100) return;

        p.HP.Value = Mathf.Min(100, p.HP.Value + _healAmount);

        if (_manager != null)
            _manager.OnPickedUp(_pos);

        base.Despawn();
    }
}