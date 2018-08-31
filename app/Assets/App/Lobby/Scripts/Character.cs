using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Character structure
/// </summary>
[Serializable]
public class Char
{
    /// <summary>
    /// Unique ID of the character.
    /// </summary>
    public int ID { get; set; }

    /// <summary>
    /// Tag of the character.
    /// This is used for object and sprite naming.
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// Health of the character
    /// </summary>
    public int Health { get; set; }

    /// <summary>
    /// Stamina of the character
    /// </summary>
    public int Stamina { get; set; }

    /// <summary>
    /// Movement speed of the character
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Sword hit power of the character
    /// </summary>
    public int Sword { get; set; }

    /// <summary>
    /// Arrow hit power of the character
    /// </summary>
    public int Bow { get; set; }

    /// <summary>
    /// Defence power of the character
    /// </summary>
    public int Shield { get; set; }
}

[Serializable]
public class CharImage
{
    public int ID;
    public Sprite Sprite;
}

/// <summary>
/// Character class
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Match))]
public class Character : MonoBehaviour
{
    /// <summary>
    /// All loaded characters from the server.
    /// </summary>
    private static List<Char> characters = new List<Char>();

    /// <summary>
    /// Self script for static operations.
    /// </summary>
    public static Character self;

    /// <summary>
    /// Current character info.
    /// </summary>
    [HideInInspector]
    public Char activeChar = null;

    /// UI panels for all character services.
    public GameObject uiChoose;

    // All character images
    public CharImage[] images;

    /// <summary>
    /// Is characters already loaded from the server?
    /// </summary>
    private bool loaded = false;

    /// <summary>
    /// UI elements of characters
    /// </summary>
    private UICharacter[] uics;

    /// <summary>
    /// Fake constructor.
    /// </summary>
    private void Awake()
    {
        self = this;
    }

    /// <summary>
    /// Load characters from the server
    /// </summary>
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => UDP.ready == true);

        Debug.Log("Fetching characters...");

        // request
        UDP.Request(new UDPPacket
        {
            Service = UDPService.Characters,
            Action = UDPAction.Characters,
            Request = {},
        });
    }

    public static void Open()
    {
        self.uiChoose.SetActive(true);
        self.LoadUI();
    }

    /// <summary>
    /// Chooses a character for the match
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public bool Choose(int cid, UDPPacket r = null)
    {
        if (r == null)
        {
            // if the user is not logged in or match type is none, restart
            if (!Account.IsLoggedIn() || cid == 0)
            {
                Kill2Live.Restart();
                return false;
            }
            Debug.Log("Choosing a character: " + cid.ToString() + " ...");

            Event e = Match.self.activeEvent;
            e.User = Account.GetUser();
            e.Me = new Player
            {
                Character = cid
            };

            // request
            string j = JsonConvert.SerializeObject(e);
            UDP.Request(new UDPPacket
            {
                Service = UDPService.Match,
                Action = UDPAction.Choose,
                Request = j,
            });
            return false;
        }
        else
        {
            if (r.Error != 0 || r.Response == null)
            {
                UIMessageGlobal.Open(Language.v["choosefailed"],
                    Language.v["choosefaileddesc"]);
                Debug.LogWarning(r.ToString());
                return false;
            }
            Debug.Log("Chosen: " + r.Response);

            Event e = JsonConvert.DeserializeObject<Event>(r.Response);
            Match.self.activeEvent.Me.Character = e.Me.Character;

            UIPlayers.Set(Account.GetUser().Nickname, e.Me.Character);

            // TODO update ui
            uiChoose.SetActive(false);

            return true;
        }
    }
    public void ChooseUI(int cid)
    {
        Choose(cid);
    }

    public bool Load(UDPPacket r = null)
    {
        if (r != null && r.Error == 0)
        {
            loaded = false;
            characters.Clear();
            characters = JsonConvert.DeserializeObject<List<Char>>(r.Response);
            Debug.Log(characters.Count + " characters are loaded.");
            return true;
        }
        return false;
    }

    private void LoadUI()
    {
        if (loaded) return;

        if (uics == null || uics.Length == 0)
        {
            uics = FindObjectsOfType<UICharacter>();
        }

        foreach(Char c in characters)
        {
            foreach(UICharacter uic in uics)
            {
                if (uic.cid != c.ID) continue;
                uic.health.text = c.Health.ToString();
                uic.stamina.text = c.Stamina.ToString();
                uic.speed.text = c.Speed.ToString();
                uic.sword.text = c.Sword.ToString();
                uic.bow.text = c.Bow.ToString();
                uic.shield.text = c.Shield.ToString();
                break;
            }
        }
        loaded = true;
    }

    public static Sprite GetImage(int id)
    {
        foreach(CharImage i in self.images)
        {
            if(i.ID == id)
            {
                return i.Sprite;
            }
        }
        return null;
    }

    public static void Clear()
    {
        characters.Clear();
    }
}
