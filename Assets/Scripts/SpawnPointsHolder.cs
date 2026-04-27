using UnityEngine;

public class SpawnPointsHolder : MonoBehaviour
{
    public Transform[] Points;

    public static SpawnPointsHolder Instance;

    private void Awake()
    {
        Instance = this;
    }
}