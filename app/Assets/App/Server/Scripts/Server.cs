using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public class Server : MonoBehaviour
{
    public string debugMatchID;

    [HideInInspector]
    public bool initialized = false;

    [HideInInspector]
    public string mid = "";

    private NetworkManager manager;

    [HideInInspector]
    public Event e;

    private List<Player> awaitingConnections = new List<Player>();
    private List<int> connectedClients = new List<int>();

    private void Awake()
    {
        // quality settings
        QualitySettings.SetQualityLevel(0, true);
        Application.targetFrameRate = 20;
        Application.runInBackground = true;
        Screen.SetResolution(1024, 576, false);

        // get command line arguments for port
        string[] args = Environment.GetCommandLineArgs();
        mid = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-id" && args.Length > i + 1)
            {
                mid = args[i + 1];
            }
        }
        if (mid == "")
        {
            mid = debugMatchID;
            Debug.Log("Debugging match: " + mid);
        }
        else
        {
            Debug.Log("Running server for match: " + mid);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => UDP.ready == true);

        if (mid != "")
        {
            Event x = new Event
            {
                M = new Game
                {
                    ID = mid
                }
            };
            string j = JsonConvert.SerializeObject(x);
            
            UDP.Request(new UDPPacket
            {
                Service = UDPService.Priv,
                Action = UDPAction.Match,
                Request = j,
            });
        }
    }

    public void Run(UDPPacket r = null)
    {
        if (r.Error != 0)
        {
            Debug.LogError("Invalid response from the service!");
            Application.Quit();
        }
        
        if(r == null || r.Response == null || r.Response == "")
        {
            Debug.LogError("No response from the priv service!");
            Application.Quit();
            return;
        }
        
        e = JsonConvert.DeserializeObject<Event>(r.Response);

        // no port >>> running in the editor
        if (e.M.Port == 0)
        {
            e.M.Port = UnityEngine.Random.Range(30000, 65000);
            e.M.IP = "localhost";
        }

        manager = GetComponent<NetworkManager>();
        manager.networkAddress = e.M.IP;
        manager.networkPort = e.M.Port;
        manager.StartServer();

        Comm.Begin(this, null);

        initialized = true;

        StartCoroutine(Maps.LoadAsync(e.M.Typ));
    }

    private void OnApplicationQuit()
    {
        NetworkServer.Shutdown();
    }

    public void Connect(UDPPacket r = null)
    {
        if (r.Error != 0)
        {
            Debug.LogError("Invalid response from the service!");
            Application.Quit();
        }

        if (r == null || r.Response == null || r.Response == "")
        {
            Debug.LogError("No response from the priv service!");
            Application.Quit();
            return;
        }

        e = JsonConvert.DeserializeObject<Event>(r.Response);

        for (int i = 0; i < e.Players.Length; i++)
        {
            if (e.Me.CID != e.Players[i].CID)
                continue;

            awaitingConnections.Add(e.Players[i]);
            Debug.Log(e.Players[i].Account_ID + " is connected.");
            ServerInfo.SetPlayer(e.Players[i].Account_ID, true);

            if (SystemInfo.graphicsDeviceID != 0)
                GetComponent<Networking>().Debugger();
            break;
        }
    }

    public IEnumerator CreatePlayer(NetworkConnection conn)
    {
        yield return new WaitUntil(() => awaitingConnections.Count > 0);

        List<string> reservedSpawnPoints = new List<string>();
        string tempSP = "";

        List<Player> pr = new List<Player>();
        foreach (Player p in awaitingConnections)
        {
            pr.Add(p);
            Player player = null;

            for (int i = 0; i < e.Players.Length; i++)
            {
                if (e.Players[i].SpawnPoint != "") reservedSpawnPoints.
                        Add(e.Players[i].SpawnPoint);
                if (p.CID != e.Players[i].CID) continue;
                player = e.Players[i];

                for (int y = 1; y <= 100; y++)
                {
                    tempSP = "SpawnPoint" + player.Side.ToString() + "_" + 
                        y.ToString();
                    if (reservedSpawnPoints.Contains(tempSP)) continue;
                    e.Players[i].SpawnPoint = tempSP;
                    player.SpawnPoint = tempSP;
                    break;
                }
                break;
            }

            if (player == null) continue;

            foreach (PlayerController obj in conn.playerControllers)
            {
                CharBase cbase = obj.gameObject.GetComponent<CharBase>();
                Health health = obj.gameObject.GetComponent<Health>();
                //Movement movement = obj.gameObject.GetComponent<Movement>();

                obj.gameObject.name = player.Account_ID;
                cbase.cid = conn.connectionId;
                health.playerName = player.PlayerName;
                obj.gameObject.transform.Find("Namebar").Find("Name").
                    GetComponent<Text>().text = player.PlayerName;

                // server debug to see the char
                obj.gameObject.transform.Find("DebugServer").gameObject.
                    SetActive(true);

                health.selectedSpawnPoint = player.SpawnPoint;
                health.characterID = player.Character;
                
                break;
            }
        }

        foreach(Player p in pr)
        {
            awaitingConnections.Remove(p);
        }

        conn.Send(Comm.msgCID, new IntegerMessage(1));


        // notify all clients
        NetworkInstanceId n = new NetworkInstanceId();
        foreach (CharBase cb in FindObjectsOfType<CharBase>())
        {
            if (cb.isMe) n = cb.netId;
        }
        Comm.MBChar c = new Comm.MBChar
        {
            connection = conn.connectionId,
            playerName = Account.GetUser().Nickname,
            characterID = Match.self.activeEvent.Me.Character,
        };
        NetworkServer.SendToAll(Comm.msgChar, c);
    }

    public void OnClientConnect(int cid)
    {
        connectedClients.Add(cid);

        Event x = new Event
        {
            M = new Game
            {
                ID = mid
            },
            Me = new Player
            {
                CID = cid
            }
        };
        string j = JsonConvert.SerializeObject(x);

        UDP.Request(new UDPPacket
        {
            Service = UDPService.Priv,
            Action = UDPAction.CID,
            Request = j,
        });
    }

    public void OnClientDisconnect(int cid)
    {
        connectedClients.Remove(cid);
    }
    
}
