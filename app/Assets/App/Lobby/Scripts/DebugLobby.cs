using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DebugAccount
{
    public string Email;
    public string Password;
}

public class DebugLobby : MonoBehaviour {

    public GameObject debugCanvas;

    public List<DebugAccount> debugAccounts = new List<DebugAccount>();
    public GameObject debugAccountsContainer;
    public Transform debugAccountsContent;
    public GameObject debugAccountOption;

    public InputField loginEmail;
    public InputField loginPassword;
    public Button loginBtn;

    private void Awake()
    {
        if(!Debug.isDebugBuild)
        {
            DestroyImmediate(gameObject);
        }
    }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.2f);

        foreach(DebugAccount da in debugAccounts)
        {
            GameObject g = Instantiate(debugAccountOption, 
                debugAccountsContent) as GameObject;
            g.SetActive(true);
            g.GetComponent<Button>().onClick.AddListener(
                delegate { Account(da); });
            g.transform.Find("Text").GetComponent<Text>().text =
                da.Email;
        }
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.F1))
        {
            debugCanvas.SetActive(true);

            if (Input.GetKey(KeyCode.F2)) debugAccountsContainer.SetActive(true);
            else debugAccountsContainer.SetActive(false);

        } else
        {
            debugCanvas.SetActive(false);
            debugAccountsContainer.SetActive(false);
        }
    }

    public void Account(DebugAccount da)
    {
        loginEmail.text = da.Email;
        loginPassword.text = da.Password;
        loginBtn.onClick.Invoke();
    }
}
