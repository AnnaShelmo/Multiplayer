using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private void Update()
    {
        if (!base.IsServerInitialized) return;

        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        var target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        if (target.OwnerId == OwnerId) return;

        target.HP.Value = Mathf.Max(0, target.HP.Value - _damage);

        base.Despawn();
    }
}