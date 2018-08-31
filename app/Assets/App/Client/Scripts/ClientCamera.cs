using UnityEngine;
using UnityEngine.Networking;

public class ClientCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 10.0f;
    public float height = 6.0f;
    public float damping = 5.0f;
    public bool smoothRotation = true;
    public float rotationDamping = 10.0f;

    public void Set()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject p in players)
        {
            NetworkIdentity ni = p.GetComponent<NetworkIdentity>();
            if(ni.isLocalPlayer)
            {
                target = p.transform;
                break;
            }
        }
    }

    private void Update()
    {
        if (target != null)
        {
            Vector3 wantedPosition =
                target.TransformPoint(0, height, -distance);
            transform.position = Vector3.Lerp(
                transform.position, wantedPosition, Time.deltaTime * damping);

            if (smoothRotation)
            {
                Quaternion wantedRotation =
                    Quaternion.LookRotation(target.position -
                    transform.position, target.up);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    wantedRotation, Time.deltaTime * rotationDamping);
            }

            else transform.LookAt(target, target.up);
        }
    }

}
