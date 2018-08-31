using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ServerCamera : UnityStandardAssets.Utility.SmoothFollow
{

    private GameObject[] players;

    public float spectatorTimer = 2f;
    private float spectatorTime = 0f;
    private int spectatorLimit = 0;
    private int spectatorIndex = 0;

    private bool refreshing = false;

    private Transform prevTarget = null;
    
    public GameObject uiContainer;
    public Text uiID;
    public Text uiCount;

    private void Start()
    {
        if (uiContainer != null) uiContainer.SetActive(true);
    }
	
	private void Update () {
        if (spectatorLimit == 0 || refreshing) return;

        // spectates the next player
		if(spectatorTime < Time.time)
        {
            spectatorIndex++;
            if(spectatorIndex > spectatorLimit - 1)
            {
                spectatorIndex = 0;
            }

            target = players[spectatorIndex].transform;
            if (uiID != null) uiID.text = players[spectatorIndex].name;

            spectatorTime = Time.time + spectatorTimer;
        }

        // spectate the player
        if(target != null)
        {
            if (prevTarget == null || target != prevTarget)
            {
                prevTarget = target;
            }
        }
	}

    public void Refresh()
    {
        StartCoroutine(RefreshAsync());
    }

    IEnumerator RefreshAsync()
    {
        refreshing = true;
        yield return new WaitForSeconds(0.2f);
        players = GameObject.FindGameObjectsWithTag("Player");
        spectatorTime = Time.time + spectatorTimer;
        spectatorLimit = players.Length;

        if (uiCount != null) uiCount.text = spectatorLimit.ToString();

        refreshing = false;
    }
}
