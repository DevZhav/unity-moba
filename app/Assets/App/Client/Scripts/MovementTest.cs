using UnityEngine;

public class MovementTest : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawn;

    public float speedRotate = 150f;
    public float speedForward = 10f;
    public float speedBullet = 30f;

    void Update()
    {
        var x = Input.GetAxis("Horizontal") * Time.deltaTime * speedRotate;
        var z = Input.GetAxis("Vertical") * Time.deltaTime * speedForward;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);

        if (Input.GetButtonDown("Fire"))
        {
            Fire();
        }
    }

    void Fire()
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

        // Destroy the bullet after 2 seconds
        Destroy(bullet, 2.0f);
    }
}