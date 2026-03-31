using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> Nickname =
        new NetworkVariable<FixedString32Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<int> HP =
        new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            transform.position = new Vector3(
                Random.Range(-3f, 3f),
                0,
                Random.Range(-3f, 3f));
        }

        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            nickname = "Player";

        Nickname.Value = nickname;
        HP.Value = 100;
    }
}