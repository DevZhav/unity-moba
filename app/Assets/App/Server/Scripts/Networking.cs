using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class Networking : NetworkManager {

    private Server server;
    private ServerCamera cam = null;

    IEnumerator Start()
    {
        server = GetComponent<Server>();
        yield return new WaitUntil(() => server.initialized == true);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        // TODO handle disconnection with CID
        int remove = -1;
        for (int i = 0; i < server.e.Players.Length; i++)
        {
            remove++;
            if (server.e.Players[i].CID != conn.connectionId) continue;
            server.OnClientDisconnect(conn.connectionId);
            Debug.Log(server.e.Players[i].Account_ID + " is disconnected.");
            ServerInfo.SetPlayer(server.e.Players[i].Account_ID, false);

            base.OnServerDisconnect(conn);

            StartCoroutine(DestroyPlayer(server.e.Players[i].Account_ID));

            if (SystemInfo.graphicsDeviceID != 0) Debugger();
            break;
        }
        server.e.Players = server.e.Players.RemoveAt(remove);
    }

    public override void OnServerAddPlayer(NetworkConnection conn,
        short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);

        StartCoroutine(server.CreatePlayer(conn));
    }

    public void Debugger()
    {
        if (cam == null) cam = FindObjectOfType<ServerCamera>();
        if (cam == null) return;
        cam.Refresh();
    }

    IEnumerator DestroyPlayer(string accountID)
    {
        yield return new WaitForSeconds(0.2f);
        GameObject userObj = GameObject.Find(accountID);
        if (userObj != null)
        {
            DestroyImmediate(userObj);
            ServerCamera cam = FindObjectOfType<ServerCamera>();
            if(cam != null) cam.Refresh();
            Debug.LogWarning("User object is manually destroyed");
        }
    }
}
