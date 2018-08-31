using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Enum types of all services.
/// </summary>
public enum UDPService
{
    Account,
    Match,
    Characters,
    Priv,
};

/// <summary>
/// All action types.
/// CAUTION! DO NOT change the order of this enum for service discovery
/// </summary>
public enum UDPAction
{
    Ping,
    Login,
    Key,
    Register,
    RegisterCode,
    Reset,
    ResetCode,
    Join,
    Players,
    Leave,
    Choose,
    Characters,
    Matches,
    Match,
    Account,
    CID
};

/// <summary>
/// UDP error types.
/// </summary>
public enum UDPErrorType
{
    None = 0,
    Undetailed = 10,
    RequestTimeout = 11,
    RequestFailed = 12,
};

/// <summary>
/// Service error types
/// </summary>
public enum ServiceError
{
    None = 0,
    Hidden = 1,
    Auth = 2,
    Sys = 3,

    AccountUnknown = 100,
    LoginFailed = 101,
    KeyFailed = 102,
    EmailExists = 103,
    RegInsert = 104,
    ResetAcc = 105,
    RegCodeVal = 106,
    RegCodeExp = 107,
    RegCodeUpd = 108,
    ResCodeVal = 109,
    ResCodeExp = 110,
    ResCodeUpd = 111,
    EmailInval = 112,
    PassInval = 113,
    NickInval = 114,

    MatchUnknown = 200,
    RoomFull = 201,
    AlreadyJoined = 202,
    JoinFailed = 203,
    NoMatchFound = 204,
    ServerReserv = 205,
    PlayerLeave = 206,
}


/// <summary>
/// UDP ports of all services.
/// </summary>
[Serializable]
public class UDPPort
{
    /// <summary>
    /// Account service handles logins, registrations and other user account
    /// based processes.
    /// </summary>
    public int account;

    /// <summary>
    /// Match service handles creating, joining, leaving a game.
    /// It does extra checks for match types, room availability.
    /// </summary>
    public int match;

    /// <summary>
    /// Characters service fetches the latest characters stats from the server
    /// </summary>
    public int characters;

    /// <summary>
    /// Private services for the internal programs
    /// </summary>
    public int priv;
};

/// <summary>
/// UDP server and client configuration.
/// </summary>
[Serializable]
public class UDPConfig
{
    /// <summary>
    /// The hostname for the server.
    /// Before you setup a host on a server, this is "127.0.0.1"
    /// </summary>
    public string host = "127.0.0.1";

    /// <summary>
    /// The request will fail after this number of miliseconds.
    /// </summary>
    public int requestTimeout = 30000;

    /// <summary>
    /// Ports of services.
    /// You must sync these values with your server configuration if you
    /// changed your server.
    /// </summary>
    public UDPPort servicePorts = new UDPPort
    {
        account = 30001,
        match = 30002,
        characters = 30003,
        priv = 30004,
    };
}

/// <summary>
/// UDP Packet structure.
/// </summary>
[Serializable]
public class UDPPacket
{
    /// <summary>
    /// ID of the request.
    /// This is a unique to find related responses.
    /// </summary>
    public string ID { get; set; }

    /// <summary>
    /// Contact service for the request.
    /// </summary>
    public UDPService Service { get; set; }

    /// <summary>
    /// Action type for the client.
    /// </summary>
    public UDPAction Action { get; set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public IPEndPoint IP { get; set; }

    /// <summary>
    /// Serialized UDPPacket data of the request.
    /// </summary>
    public string Request { get; set; }

    /// <summary>
    /// Type of the error.
    /// </summary>
    public int Error { get; set; }

    /// <summary>
    /// Exception for the error
    /// </summary>
    public Exception ErrorExc = null;

    /// <summary>
    /// Response string of the request.
    /// This is a serialized object.
    /// It will be used by other functions to deserialize with a structure.
    /// </summary>
    public string Response { get; set; }

    /// <summary>
    /// Parses the response as UDPPacket.
    /// Basically, the "Response" variable will be parsed as UDPPacket.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static UDPPacket Parse(string json)
    {
        return JsonConvert.DeserializeObject<UDPPacket>(json);
    }

    public override string ToString()
    {
        if (Error != 0 || ErrorExc != null)
        {
            if (ErrorExc != null) return ErrorExc.Message;
            else
            {
                if (Error >= 100) return ((ServiceError)Error).ToString();
                return ((UDPErrorType)Error).ToString();
            }
        }
        return base.ToString();
    }
}

/// <summary>
/// UDP listener.
/// </summary>
[DisallowMultipleComponent]
public class UDP : MonoBehaviour
{
    /// <summary>
    /// UDP client.
    /// </summary>
    UdpClient client;

    /// <summary>
    /// Endpoint address of the server.
    /// Client accepts messages from this endpoint.
    /// </summary>
    IPEndPoint endPoint;

    /// <summary>
    /// Listener thread.
    /// This is not working in Unity main thread
    /// </summary>
    Thread listener;

    /// <summary>
    /// Holds the packet queue
    /// </summary>
    Queue pQueue = Queue.Synchronized(new Queue());

    /// <summary>
    /// Holds the requests list to control timeouts and deactivated objects
    /// </summary>
    Dictionary<string, UDPPacket> requests = 
        new Dictionary<string, UDPPacket>();

    /// <summary>
    /// All UDP configuration
    /// </summary>
    public UDPConfig config;

    /// <summary>
    /// Usage for static functions
    /// </summary>
    protected static UDP udp = null;

    /// <summary>
    /// Main game object
    /// </summary>
    private Kill2Live main;

    /// <summary>
    /// Account component
    /// </summary>
    private Account account;

    /// <summary>
    /// Match component
    /// </summary>
    private Match match;

    /// <summary>
    /// Character component
    /// </summary>
    private Character character;

    /// <summary>
    /// Server component
    /// </summary>
    private Server server;

    /// <summary>
    /// Client component
    /// </summary>
    private Client cli;


    public static bool ready = false;

    public GameObject loadingFull;
    public GameObject loadingSmall;


    /// <summary>
    /// Resolves the host name and starts the listener on a different thread
    /// </summary>
    void Start()
    {
        // load scripts
        main = GetComponent<Kill2Live>();
        account = GetComponent<Account>();
        match = GetComponent<Match>();
        character = GetComponent<Character>();
        server = GetComponent<Server>();
        cli = FindObjectOfType<Client>();

        endPoint = new IPEndPoint(IPAddress.Any, 0);
        if(config == null)
        {
            return;
        }

        // reserve a port for client
        int port = 0;
        do
        {
            port = UnityEngine.Random.Range(10000, 60000);
            try
            {
                client = new UdpClient(port);
                client.Client.SendTimeout = config.requestTimeout;
            }
            catch (Exception exc)
            {
                port = 0;
                Debug.LogError(exc);
            }
        } while (port == 0);

        if (client != null)
        {
            Debug.Log("UDP Listening @ " + port + " ...");
            listener = new Thread(new ThreadStart(Translate));
            listener.IsBackground = true;
            listener.Start();
            ready = true;
        }
    }

    /// <summary>
    /// Handles the items in queue
    /// </summary>
    void Update()
    {

#if UNITY_EDITOR
        // special case for the editor
        // if it is running on the editor, a compilation on play mode will
        // make Unity crash
        if(Application.isEditor && EditorApplication.isCompiling)
        {
            if(listener.IsAlive) listener.Abort();
            if (client != null) client.Close();
        }
#endif

        lock (pQueue.SyncRoot)
        {
            if (pQueue.Count > 0)
            {
                UDPPacket p = (UDPPacket)pQueue.Dequeue();
                Debug.Log("Processing: " + p.ID);
                requests.Remove(p.ID);
                if(loadingFull != null) loadingFull.SetActive(false);
                if(loadingSmall != null) loadingSmall.SetActive(false);
                if (p.Error != 0 && p.Error < 100)
                {
                    UIMessageGlobal.Open(
                        (server == null) ? Language.v["requesttimeout"] : 
                        "Request Timeout");
                }
                Action(p);
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        if (Application.isPlaying)
        {
            EditorApplication.isPlaying = false;
            Debug.Log("Play mode is deactivated due to script updates.\n" + 
                "UDP listener and some scripts can't work after re-compile.");
        }
    }
#endif

    /// <summary>
    /// Terminates UDP socket on quit
    /// </summary>
    void OnApplicationQuit()
    {
        if(client != null) client.Close();
    }

    /// <summary>
    /// Listens, translates a received packet from a service to UDPPacket
    /// structure and puts it in the queue
    /// </summary>
    void Translate()
    {
        Byte[] data = new byte[0];
        while (!Kill2Live.quiting)
        {
            try
            {
                data = client.Receive(ref endPoint);
            }
            catch
            {
                // client.Close();
                return;
            }
            string json = Encoding.ASCII.GetString(data);
            pQueue.Enqueue(UDPPacket.Parse(json));
        }
    }

    /// <summary>
    /// Sends a packet to given service
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private IEnumerator Send(UDPPacket p)
    {
        if (Kill2Live.quiting) yield return 0;
        
        int port = 0;
        switch(p.Service)
        {
            case UDPService.Account:
                port = config.servicePorts.account;
                break;
            case UDPService.Match:
                port = config.servicePorts.match;
                break;
            case UDPService.Characters:
                port = config.servicePorts.characters;
                break;
            case UDPService.Priv:
                port = config.servicePorts.priv;
                break;
        }

        IPEndPoint service = new IPEndPoint(
            Dns.GetHostAddresses(config.host)[0], port);

        try
        {
            string json = JsonConvert.SerializeObject(p);
            byte[] data = Encoding.UTF8.GetBytes(json);
            if (client != null)
            {
                client.Send(data, data.Length, service);
            }
        }
        catch(Exception exc)
        {
            p.Error = (int)UDPErrorType.RequestFailed;
            p.ErrorExc = exc;
            Debug.LogError(p.ErrorExc);
        }

        yield return new WaitForSeconds(config.requestTimeout / 1000);

        if(client != null && requests.ContainsKey(p.ID))
        {
            p.Error = (int)UDPErrorType.RequestTimeout;
            Debug.LogWarning(p.ToString());
            requests.Remove(p.ID);
            if(loadingFull != null) loadingFull.SetActive(false);
            if(loadingSmall != null) loadingSmall.SetActive(false);
            if (main != null) UIMessageGlobal.Open(
                 (server == null) ? Language.v["requesttimeout"] : 
                 "Request Timeout");
            Action(p);
        }
    }

    /// <summary>
    /// Starts a coroutine to send a packet
    /// </summary>
    /// <param name="p"></param>
    /// <param name="wait"></param>
    private void Comm(UDPPacket p, bool wait = true)
    {
        p.ID = Guid.NewGuid().ToString("N");
        requests.Add(p.ID, p);
        if (
            p.Action != UDPAction.Players
        )
        {
            if (loadingFull != null) loadingFull.SetActive(true);
            if (loadingSmall != null) loadingSmall.SetActive(true);
        }
        StartCoroutine(Send(p));
        Debug.Log("UDP Request: " + p.ID);
    }

    /// <summary>
    /// Static call to send a UDP request
    /// </summary>
    /// <param name="p"></param>
    public static void Request(UDPPacket p, bool wait=true)
    {
        if(udp == null)
        {
            udp = FindObjectOfType<UDP>();
        }
        if (udp == null) return;
        udp.Comm(p, wait);
    }

    /// <summary>
    /// An action which is based on the response
    /// </summary>
    /// <param name="p"></param>
    public void Action(UDPPacket p)
    {
        switch (p.Action)
        {
            case UDPAction.Login:
                if (account.Login(p))
                {
                    main.uiLanguage.SetActive(false);
                    account.uiLogin.SetActive(false);
                    account.uiProfile.SetActive(true);
                    match.uiJoin.SetActive(true);

                    // auto-connect to existing  game
                    match.Join(MatchType.None);
                }
                break;
            case UDPAction.Register:
                if(account.Register(p))
                {
                    account.uiRegister.SetActive(false);
                    account.uiRegisterCode.SetActive(true);
                }
                break;
            case UDPAction.RegisterCode:
                if(account.RegisterCode(p))
                {
                    account.uiRegisterCode.SetActive(false);
                    account.uiLogin.SetActive(true);
                }
                break;
            case UDPAction.Reset:
                if(account.Reset(p))
                {
                    account.uiReset.SetActive(false);
                    account.uiResetCode.SetActive(true);
                }
                break;
            case UDPAction.ResetCode:
                if(account.ResetCode(p))
                {
                    account.uiResetCode.SetActive(false);
                    account.uiLogin.SetActive(true);
                }
                break;
            case UDPAction.Join:
                if (match.Join(0, p))
                {
                    match.uiJoin.SetActive(false);
                    match.uiPlay.SetActive(true);
                }
                break;
            case UDPAction.Players:
                if(match.Players(p))
                {

                }
                break;
            case UDPAction.Leave:
                match.Leave(p);
                break;
            case UDPAction.Choose:
                character.Choose(0, p);
                break;
            case UDPAction.Characters:
                character.Load(p);
                break;
            case UDPAction.Match:
                if (server != null) server.Run(p);
                else if(cli != null) cli.DebugMatch(p);
                break;
            case UDPAction.Account:
                if (server != null) { }
                else if(cli != null) cli.DebugAccount(p);
                break;
            case UDPAction.CID:
                if (server != null) server.Connect(p);
                break;
        }
    }

    public void Stop()
    {
        if (listener.IsAlive) listener.Abort();
        if (client != null) client.Close();
    }
    public static void Clear()
    {
        UDP udp = FindObjectOfType<UDP>();
        if(udp != null) udp.Stop();
    }
}