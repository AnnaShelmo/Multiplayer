using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private int _damage = 10;

    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryAttackNearest();
        }
    }

    private void TryAttackNearest()
    {
        PlayerNetwork[] players = FindObjectsOfType<PlayerNetwork>();

        foreach (var target in players)
        {
            if (target == _playerNetwork)
                continue;

            DealDamageServerRpc(target.NetworkObjectId, _damage);
            break;
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetObjectId, int damage)
    {
        if (!NetworkManager.SpawnManager.SpawnedObjects
            .TryGetValue(targetObjectId, out NetworkObject targetObject))
            return;

        PlayerNetwork target = targetObject.GetComponent<PlayerNetwork>();

        if (target == null || target == _playerNetwork)
            return;

        int nextHp = Mathf.Max(0, target.HP.Value - damage);
        target.HP.Value = nextHp;
    }
}