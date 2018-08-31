using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;

public class Client : NetworkManager
{
    public static bool ready = false;

    private Kill2Live k2l;
    private Canvas canvas;

    [HideInInspector]
    public Event e = new Event
    {
        User = null,
        M = null,
        Me = null,
        Players = null,
    };
    
    // debug elements
    public string debugAccountId = "";
    public string debugMatchId = "";
    public GameObject debugLobbyPrefab;

    // ui elements
    public GameObject uiLoading;

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();

        // debug
        if (Account.GetUser() == null && (Match.self == null ||
            Match.self.activeEvent == null) &&
            debugAccountId != "" && debugMatchId != "")
        {
            Instantiate(debugLobbyPrefab, null);
            StartCoroutine(Debugger());
        } else
        {
            // e.User = Account.GetUser();
            e = Match.self.activeEvent;
        }

        k2l = FindObjectOfType<Kill2Live>();
        StartCoroutine(Network());
    }

    private IEnumerator Debugger()
    {
        yield return new WaitUntil(() => UDP.ready == true);

        // account
        Debug.Log("Requesting debug user...");
        Event a = new Event
        {
            User = new User
            {
                ID = debugAccountId
            }
        };
        string ja = JsonConvert.SerializeObject(a);
        UDP.Request(new UDPPacket
        {
            Service = UDPService.Priv,
            Action = UDPAction.Account,
            Request = ja,
        });
    }

    public void DebugAccount(UDPPacket r)
    {
        if (r == null) return;
        if (r.Error != 0)
        {
            Debug.LogError("Debug account can not be fetched!");
            return;
        }

        Event x = JsonConvert.DeserializeObject<Event>(r.Response);
        e.User = x.User;
        e.User.Status(UserStatus.Logined);
        Debug.Log("Debug user: " + e.User.ID);

        // match
        Debug.Log("Requesting debug match...");
        Event m = new Event
        {
            M = new Game
            {
                ID = debugMatchId
            }
        };
        string jm = JsonConvert.SerializeObject(m);
        UDP.Request(new UDPPacket
        {
            Service = UDPService.Priv,
            Action = UDPAction.Match,
            Request = jm,
        });
    }

    public void DebugMatch(UDPPacket r)
    {
        if (r == null) return;
        if (r.Error != 0)
        {
            Debug.LogError("Debug match can not be fetched!");
            return;
        }

        Event x = JsonConvert.DeserializeObject<Event>(r.Response);
        e.M = x.M;
        e.Players = x.Players;
        if (e.M.Typ == MatchType.None)
        {
            Debug.LogError("Invalid match type!");
            return;
        }

        // me
        foreach(Player p in e.Players)
        {
            if (p.Account_ID != e.User.ID) continue;
            e.Me = p;
            break;
        }

        Debug.Log("Debug match: " + e.M.ID);
    }

    private IEnumerator Network()
    {
        Debug.Log("Setting the network...");
        yield return new WaitUntil(() =>
            UDP.ready == true &&
            (
                (e.User != null && e.M != null) ||
                (Account.GetUser() != null && Match.self.activeEvent != null)
            )
        );

        networkAddress = e.M.IP;
        networkPort = e.M.Port;
        
        StartClient();
        Debug.Log("Client is started: " + e.M.IP);

        Comm.Begin(null, this);

        StartCoroutine(Ready());
    }

    private void Prepare()
    {
        // disable loading
        if(uiLoading != null) uiLoading.SetActive(false);

        // display HUD
        Transform hud = canvas.transform.Find("HUD");
        Transform player = hud.Find("Player");
        Image imgChar = player.Find("Char").GetComponent<Image>();
        imgChar.sprite = Character.GetImage(e.Me.Character);
        hud.gameObject.SetActive(true);

        // load the map
        StartCoroutine(Maps.LoadAsync(e.M.Typ));
    }

    private IEnumerator Ready()
    {
        yield return new WaitForSeconds(1f);

        Prepare();

        // set camera
        ClientCamera cliCam = FindObjectOfType<ClientCamera>();
        cliCam.Set();

        ready = true;
    }

    private void Clear()
    {
        foreach(GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            DestroyImmediate(go);
        }
        DestroyImmediate(GameObject.FindGameObjectWithTag("Map"));

        k2l.WindowNormal();
        Maps.UnloadAsync(e.M.Typ);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        conn.Send(Comm.msgCID, new IntegerMessage(e.Me.CID));
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);

        Clear();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        Clear();
    }

    public override void OnClientNotReady(NetworkConnection conn)
    {
        base.OnClientNotReady(conn);

        Clear();
    }

    public void Disconnect(bool cd = false)
    {
        client.Disconnect();
        Clear();
    }
}
