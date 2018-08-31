using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public class Comm : MonoBehaviour {

    public class MBChar : MessageBase
    {
        public int connection;
        public string playerName;
        public int characterID;
    }

    public static short msgCID = 1001;
    public static short msgChar = 1002;

    private static Client cli = null;
    private static Server srv = null;

	public static void Begin (Server s, Client c) {
        srv = s;
        cli = c;
        if (cli != null)
        {
            cli.client.RegisterHandler(msgCID, OnMsgCID);
            cli.client.RegisterHandler(msgChar, OnMsgChar);
        } else {
            NetworkServer.RegisterHandler(msgCID, OnMsgCID);
            NetworkServer.RegisterHandler(msgChar, OnMsgChar);
        }
	}
    
    public static void OnMsgCID(NetworkMessage msg)
    {
        IntegerMessage m = msg.ReadMessage<IntegerMessage>();

        if(srv != null)
        {
            srv.OnClientConnect(m.value);
        }
    }

    public static void OnMsgChar(NetworkMessage msg)
    {
        MBChar m = msg.ReadMessage<MBChar>();
        foreach(CharBase cb in FindObjectsOfType<CharBase>())
        {
            if(cb.cid == m.connection)
            {
                Debug.LogWarning("nID is found");
            }
        }
        Debug.LogWarning("Clients are notified");
    }
}