using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Unity.Collections;


public class Health : NetworkBehaviour
{

    public const int maxHealth = 100;
    public bool destroyOnDeath;

    private Client cli;
    private NetworkStartPosition[] spawnPoints;

    [ReadOnly]
    [SyncVar]
    public string selectedSpawnPoint = "";

    [ReadOnly]
    public int characterID = 0;
    private bool isCreated = false;

    [ReadOnly]
    [SyncVar(hook = "OnChangeHealth")]
    public int currentHealth = maxHealth;

    [ReadOnly]
    [SyncVar(hook = "OnChangeName")]
    public string playerName = "???";

    public RectTransform healthBar;
    public Image healthCircle;
    public Text playerNameText;

    void Start()
    {
        if (isServer)
        {
            Debug.Log("Spawning the character on the server...");
            RpcRespawn();
        } else
        {
            cli = FindObjectOfType<Client>();
        }
    }

    public void TakeDamage(int amount)
    {
        if (!isServer) return;
        GetComponent<CharBase>().animator.SetTrigger("Damage");

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                currentHealth = maxHealth;

                // called on the Server, invoked on the Clients
                RpcRespawn();
            }
        }
    }

    void OnChangeHealth(int currentHealth)
    {
        healthBar.sizeDelta = 
            new Vector2(currentHealth, healthBar.sizeDelta.y);

        if (healthCircle != null)
        {
            healthCircle.fillAmount = currentHealth / 100f;
        }
    }

    void OnChangeName(string playerName)
    {
        playerNameText.text = playerName;
        gameObject.name = playerName;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        yield return new WaitUntil(() => Client.ready == true);
        yield return new WaitUntil(() => Maps.ready == true);
        yield return new WaitUntil(() => selectedSpawnPoint != "");
        yield return new WaitUntil(() => characterID > 0);

        if (!isCreated)
        {
            Debug.Log("First creation of the client character...");

            // object name
            gameObject.name = cli.e.Me.Account_ID;

            // find spawn points
            spawnPoints = FindObjectsOfType<NetworkStartPosition>();

            // Set HUD
            Canvas canvas = GameObject.FindGameObjectWithTag("Canvas").
                GetComponent<Canvas>();
            healthCircle = canvas.transform.Find("HUD").Find("Player").
                Find("Health").Find("Bar").GetComponent<Image>();

            playerName = cli.e.Me.PlayerName;

            // animation
            GetComponent<CharBase>().Load();
        }

        Debug.Log("Moving to the spawn point > " + selectedSpawnPoint);
        // Set the spawn point to origin as a default value
        Vector3 spawnPoint = Vector3.zero;
        Quaternion spawnRotate = Quaternion.identity;

        // If there is a spawn point array and the array is not empty,
        // pick one at random
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            if (selectedSpawnPoint != "")
            {
                Transform sp = GameObject.Find(selectedSpawnPoint).transform;
                spawnPoint = sp.position;
                spawnRotate = sp.rotation;   
            }
            else
            {
                spawnPoint = spawnPoints[
                    Random.Range(0, spawnPoints.Length)
                ].transform.position;
            }
        }

        // Set the player’s position to the chosen spawn point
        transform.position = spawnPoint;
        transform.rotation = spawnRotate;

        isCreated = true;
    }
}