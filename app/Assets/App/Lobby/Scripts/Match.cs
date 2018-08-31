using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Match type.
/// </summary>
public enum MatchType
{
    None,
    One,
    Two,
    Three,
    Four,
    Five,
    DM,
    Battle,
};

/// <summary>
/// Match status.
/// </summary>
public enum MatchStatus
{
    Reserved,
    Ready,
    Play,
    End,
    Released,
}

public enum PlayerStatus
{
    Joined,
    Playing,
    Left,
    Finished,
}

static class PlayerLimits
{
    public static int GetLimit(this MatchType mt)
    {
        switch(mt)
        {
            case MatchType.One: return 2;
            case MatchType.Two: return 4;
            case MatchType.Three: return 6;
            case MatchType.Four: return 8;
            case MatchType.Five: return 10;
            default: return 0;
        }
    }
}

[Serializable]
public class Game
{
    public string ID { get; set; }
    public int Creation { get; set; }
    public MatchType Typ { get; set; }
    public MatchStatus Status { get; set; }
    public int Counter { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public string Key { get; set; }
}

/// <summary>
/// Player structure.
/// </summary>
[Serializable]
public class Player
{
    public string ID { get; set; }
    public string Match_ID { get; set; }
    public string Account_ID { get; set; }
    public string PlayerName { get; set; }
    public PlayerStatus Status { get; set; }
    public int Side { get; set; }
    public int Character { get; set; }
    public string IP { get; set; }
    public int Port { get; set; }
    public int CID { get; set; }

    public string SpawnPoint = "";
}


/// <summary>
/// User structure.
/// </summary>
[Serializable]
public class Event
{
    public User User { get; set; }
    public Game M { get; set; }
    public Player[] Players { get; set; }
    public Player Me { get; set; }
}


/// <summary>
/// Match main class of an account.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Account))]
public class Match : MonoBehaviour {

    /// <summary>
    /// Self script for static operations.
    /// </summary>
    public static Match self = null;

    /// UI panels for all match services.
    public GameObject uiJoin;
    public GameObject uiPlay;
    public UIPlayers uiPlayers;

    /// <summary>
    /// Current match info.
    /// </summary>
    [HideInInspector]
    public Event activeEvent = null;
    
    /// <summary>
    /// List of players for the current match.
    /// </summary>
    private List<Player> players = new List<Player>();

    /// <summary>
    /// Refresh rate for players
    /// </summary>
    public float refresh = 2f;

    /// <summary>
    /// When a match is ready, this is the countdown (seconds)
    /// </summary>
    public int matchReadyTimer = 10;

    /// <summary>
    /// Is invoking in repeat for player refresh
    /// </summary>
    private bool refreshing = false;

    /// <summary>
    /// Fake constructor.
    /// </summary>
    private void Awake()
    {
        self = this;
        UIPlayers.self = uiPlayers;
    }

    /// <summary>
    /// Reset all variables.
    /// </summary>
    private void Reset()
    {
        activeEvent = null;
        players.Clear();
        refreshing = false;
        CancelInvoke();
    }

    /// <summary>
    /// Find and join into a game.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="r"></param>
    public bool Join(MatchType typ = MatchType.None, UDPPacket r = null)
    {
        if (r == null)
        {
            // if the user is not logged in or match type is none, restart
            if (!Account.IsLoggedIn())
            {
                Kill2Live.Restart();
                return false;
            }
            Debug.Log("Finding a match for " + typ.ToString() + " ...");

            Event e = new Event
            {
                User = Account.GetUser(),
                M = new Game
                {
                    Typ = typ,
                }
            };

            // request
            string j = JsonConvert.SerializeObject(e);
            UDP.Request(new UDPPacket
            {
                Service = UDPService.Match,
                Action = UDPAction.Join,
                Request = j,
            });
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                if (r.Error == (int)ServiceError.AlreadyJoined)
                {
                    // continue
                    // no return here because of this situation
                }
                else if(r.Error >= (int)ServiceError.MatchUnknown)
                {
                    UIMessageGlobal.Open(Language.v["joinfailed"],
                        Language.v["joinfaileddesc"]);
                    Debug.LogWarning(r.ToString());
                    return false;
                }
            }
            Debug.Log("Joining: " + r.Response);

            Event e = JsonConvert.DeserializeObject<Event>(r.Response);
            if (e.M.Typ == MatchType.None) return false;
            activeEvent = e;

            if (e.M.Status == MatchStatus.Reserved) // still creating a match
            {
                if (!refreshing)
                {
                    refreshing = true;
                    InvokeRepeating("PlayersLoop", 0f, refresh);
                }
            } else
            {
                if (e.M.Status == MatchStatus.Ready ||
                e.M.Status == MatchStatus.Play)
                {
                    Players();
                }
                else if (e.M.Status == MatchStatus.End ||
                    e.M.Status == MatchStatus.Released)
                {
                    activeEvent = null;
                    UIPlayers.Clear();
                    return false;
                }
            }
            return true;
        }

    }
    public void JoinUI(int type) { Join((MatchType)type); }

    /// <summary>
    /// Refreshes player list
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public bool Players(UDPPacket r = null)
    {
        if(r == null)
        {
            // if the user is not logged in or there is no event, skip
            if (!Account.IsLoggedIn() || activeEvent == null)
            {
                return false;
            }
            
            Event e = new Event
            {
                User = Account.GetUser(),
                M = new Game
                {
                    ID = activeEvent.M.ID,
                    Key = activeEvent.M.Key
                }
            };

            // request
            string j = JsonConvert.SerializeObject(e);
            UDP.Request(new UDPPacket
            {
                Service = UDPService.Match,
                Action = UDPAction.Players,
                Request = j,
            });
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                Debug.LogWarning(r.ToString());
                return false;
            }

            Event e = JsonConvert.DeserializeObject<Event>(r.Response);
            activeEvent.M.Status = e.M.Status;
            activeEvent.Players = e.Players;
            UIPlayers.loaded = false;
            UIPlayers.Load();

            if (e.M.Status == MatchStatus.Ready || 
                e.M.Status == MatchStatus.Play)
            {
                CancelInvoke();
                StartCoroutine(UIPlayers.self.Ready());
            }

            return true;
        }
    }
    public void PlayersLoop() { Players(); }

    
    public bool Leave(UDPPacket r = null, bool quit=false)
    {
        if (r == null)
        {
            // if the user is not logged in, restart
            if (!Account.IsLoggedIn())
            {
                if(!quit) Kill2Live.Restart();
                return false;
            }
            Debug.Log("Leaving the match...");

            Reset();
            uiPlay.SetActive(false);
            uiJoin.SetActive(true);

            Event e = new Event
            {
                User = Account.GetUser(),
            };

            // request
            string j = JsonConvert.SerializeObject(e);
            UDP.Request(new UDPPacket
            {
                Service = UDPService.Match,
                Action = UDPAction.Leave,
                Request = j,
            });
            return false;
        } else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.MatchUnknown)
                {
                    UIMessageGlobal.Open(Language.v["leavefailed"],
                        Language.v["leavefaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            return true;
        }

    }
    public void LeaveUI() { Leave(); }

    private void OnApplicationQuit()
    {
        // do not leave the game on exit
        // the player may want to restart the client
    }

    public static void Clear()
    {
        if (self == null) return;
        self.activeEvent = null;
    }

}
