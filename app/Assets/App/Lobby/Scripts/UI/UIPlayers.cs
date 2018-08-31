using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPlayers : MonoBehaviour {

    /// <summary>
    /// Self script for static operations.
    /// </summary>
    public static UIPlayers self;

    private static List<GameObject> objects = new List<GameObject>();
    private static List<Player> players = new List<Player>();
    public static bool loaded = false;

    public GameObject _player;

    public GridLayoutGroup leftGrid;
    public GridLayoutGroup rightGrid;

    private int timer = 0;
    public Text timerText;
    public Button leaveBtn;

    public static void Load()
    {
        if(!loaded) Clear();
        if (Match.self.activeEvent == null) return;
        if (self != null)
        {
            foreach (Player p in Match.self.activeEvent.Players) self.Add(p);
            loaded = true;
        }

        // clear the ones who left
        bool exists = false;
        foreach (Player i in players)
        {
            exists = false;
            foreach(Player j in Match.self.activeEvent.Players)
            {
                // TODO make it with hashed ID
                if(i.PlayerName == j.PlayerName)
                {
                    exists = true;

                    // update the player's character
                    foreach (GameObject po in objects)
                    {
                        UIPlayer uip = po.GetComponent<UIPlayer>();
                        if (uip.player.PlayerName == j.PlayerName)
                        {
                            uip.Set(j);
                            break;
                        }
                    }

                    break;
                }
            }
            if(!exists)
            {
                self.Delete(i);
            }
        }
    }

    public static void Clear()
    {
        loaded = false;
        foreach (GameObject g in objects)
        {
            DestroyImmediate(g);
        }
        objects.Clear();
        players.Clear();
    }
    
    public void Add(Player player)
    {
        // do not add but update if it exists
        foreach (Player u in players)
        {
            // TODO do the check with ID
            if (u.PlayerName == player.PlayerName)
            {
                return;
            }
        }

        Debug.Log("Creating a player: " + player.PlayerName);
        GameObject p = Instantiate(_player,
            (player.Side == 0) ? leftGrid.transform : rightGrid.transform
            ) as GameObject;
        p.GetComponent<UIPlayer>().player = player;
        objects.Add(p);
        players.Add(player);

        p.GetComponent<UIPlayer>().Set(player);
        p.transform.Find("Character").GetComponent<Button>().onClick.
            AddListener(Character.Open);
        p.SetActive(true);
    }

    public void Delete(Player player)
    {
        foreach(GameObject o in objects)
        {
            if(o.GetComponent<UIPlayer>().player.PlayerName == 
                player.PlayerName)
            {
                players.Remove(player);
                DestroyImmediate(o);
                break;
            }
        }
    }

    public static void Set(string nick, int character)
    {
        foreach(GameObject o in objects)
        {
            if(o.GetComponent<UIPlayer>().nick.text == nick)
            {
                o.transform.Find("Character").GetComponent<Image>().sprite = 
                    Character.GetImage(character);
            }
        }
    }

    public IEnumerator Ready(int timerOverride = 0)
    {
        leaveBtn.interactable = false;
        if (timerOverride == 0) timer = Match.self.matchReadyTimer;
        else timer = timerOverride;
        while (timer > 0)
        {
            yield return new WaitForSeconds(1);
            timer--;
            timerText.text = timer.ToString();
        }
        timer = 0;
        timerText.text = "--";

        Kill2Live.Client();
    }

    private void Awake()
    {
        self = FindObjectOfType<UIPlayers>();
    }

    private void OnEnable()
    {
        Vector2 cellSize = new Vector2();
        int constraintCount = 2;
        
        switch(Match.self.activeEvent.M.Typ)
        {
            case MatchType.One:
                cellSize = new Vector2(256, 256);
                constraintCount = 2;
                break;
            case MatchType.Two:
            case MatchType.Three:
            case MatchType.Four:
                cellSize = new Vector2(200, 200);
                constraintCount = 2;
                break;
            case MatchType.Five:
                cellSize = new Vector2(140, 140);
                constraintCount = 3;
                break;
        }

        leftGrid.cellSize = cellSize;
        leftGrid.constraintCount = constraintCount;
        rightGrid.cellSize = cellSize;
        rightGrid.constraintCount = constraintCount;
    }
}
