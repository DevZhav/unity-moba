using UnityEngine;
using UnityEngine.Networking;

public class Movement : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawn;

    public float speedRotate = 150f;
    public float speedForward = 10f;
    public float speedBullet = 30f;

    private Vector3 spawnPoint;

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
        
        var x = Input.GetAxis("Horizontal") * Time.deltaTime * speedRotate;
        var z = Input.GetAxis("Vertical") * Time.deltaTime * speedForward;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        if (Input.GetButtonDown("Fire"))
        {
            CmdFire();
        }
    }

    [Command]
    void CmdFire()
    {
        if (bulletPrefab == null || bulletSpawn == null) return;

        // Create the Bullet from the Bullet Prefab
        var bullet = Instantiate(
            bulletPrefab,
            bulletSpawn.position,
            bulletSpawn.rotation);

        // Add velocity to the bullet
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward
            * speedBullet;

        // Spawn the bullet on the Clients
        NetworkServer.Spawn(bullet);

        // Destroy the bullet after 2 seconds
        Destroy(bullet, 2.0f);
    }

}