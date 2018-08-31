using UnityEngine;
using UnityEngine.Networking;

public class SpawnPoint : NetworkStartPosition {

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1);
    }
}
