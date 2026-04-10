using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private void Update()
    {
        if (!IsServer) return; // движение только на сервере

        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        // не наносим урон самому себе
        if (target.OwnerClientId == OwnerClientId) return;

        target.HP.Value = Mathf.Max(0, target.HP.Value - _damage);

        NetworkObject.Despawn(true);
    }
}