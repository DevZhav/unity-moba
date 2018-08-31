using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Server))]
public class ServerInfo : MonoBehaviour {

    private Server server;
    
    public GameObject match, players;
    public GameObject player;

    private static Transform playerContent = null;


    IEnumerator Start () {
        server = GetComponent<Server>();
        yield return new WaitUntil(() => server.initialized == true);
        if (SystemInfo.graphicsDeviceID == 0)
        {
            Destroy(this);
            yield break;
        }

        match.SetActive(true);
        players.SetActive(true);

        match.transform.Find("IDVal").GetComponent<Text>().text = 
            server.e.M.ID.ToString();
        match.transform.Find("TypVal").GetComponent<Text>().text =
            server.e.M.Typ.ToString();
        match.transform.Find("StatusVal").GetComponent<Text>().text =
            server.e.M.Status.ToString();
        match.transform.Find("IPVal").GetComponent<Text>().text =
            server.e.M.IP.ToString();
        match.transform.Find("PortVal").GetComponent<Text>().text =
            server.e.M.Port.ToString();

        playerContent = players.transform.Find("Viewport").Find(
            "Content");
        foreach(Player p in server.e.Players)
        {
            if (p.Status != PlayerStatus.Playing) continue;

            GameObject i = Instantiate(player, playerContent) as GameObject;
            i.transform.Find("IDVal").GetComponent<Text>().text =
                p.Account_ID.ToString();
            i.transform.Find("NameVal").GetComponent<Text>().text =
                p.PlayerName.ToString();
            i.transform.Find("StatusVal").GetComponent<Text>().text =
                p.Status.ToString();
            i.transform.Find("SideVal").GetComponent<Text>().text =
                p.Side.ToString();
            i.transform.Find("CharacterVal").GetComponent<Text>().text =
                p.Character.ToString();
            i.transform.Find("ConnectedVal").GetComponent<Text>().text =
                "No";
            i.transform.Find("ConnectedVal").GetComponent<Text>().color = 
                Color.red;
        }
    }

    public static void SetPlayer(string accountID, bool conn)
    {
        if (playerContent == null)
        {
            Debug.LogWarning("UI player container is not set yet!");
            return;
        }
        foreach (Transform t in playerContent)
        {
            Transform to = t.Find("IDVal");
            if (to == null)
            {
                Debug.LogWarning("UI player ID object is not found!");
                continue;
            }
            Text txt = to.gameObject.GetComponent<Text>();
            if (txt == null)
            {
                Debug.LogWarning("UI player ID Text object is not found!");
                continue;
            }
            if (txt.text != accountID)
            {
                continue;
            }
            to = t.Find("ConnectedVal");
            if (to == null)
            {
                Debug.LogWarning("UI player ID connected label is not found!");
                continue;
            }
            txt = to.gameObject.GetComponent<Text>();
            if (txt == null)
            {
                Debug.LogWarning("UI player ID connection value is not found!");
                continue;
            }
            txt.text = (conn) ? "Yes" : "No";
            txt.color = (conn) ? Color.green : Color.red;
            return;
        }
        Debug.LogWarning("UI player object is not found!");
    }

}
