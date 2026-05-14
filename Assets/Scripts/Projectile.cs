using FishNet.Object;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private PlayerNetwork _shooterNet;

    public void Initialize(PlayerNetwork shooter)
    {
        _shooterNet = shooter;
    }

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

        int damage = _damage;
        int newHp = target.HP.Value - damage;
        target.HP.Value = Mathf.Max(0, newHp);

        if (newHp <= 0 && _shooterNet != null)
        {
            _shooterNet.AddScore(1);
        }

        base.Despawn();
    }
}