using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main class of the toolkit
/// </summary>
[DisallowMultipleComponent]
public class Kill2Live : MonoBehaviour {
    private const int WM_SYSCOMMAND = 0x112;
    private const int MOUSE_MOVE = 0xF012;
    private const int SW_SHOWNORMAL = 1;
    private const int SW_SHOWMAXIMIZED = 3;
    private static int nul = 0;

    private static IntPtr appWindow;
    private const string appTitle = "Kill2Live";
    public static bool quiting = false;

    public string initialPanel = "Login";
    public int resolutionX = 1024;
    public int resolutionY = 576;
    private bool moveWindow = false;
    public string clientScene = "Client";

    /// <summary>
    /// Self script for static operations
    /// </summary>
    public static Kill2Live self;

    public GameObject uiLobby;
    public GameObject uiSettings;
    public GameObject uiLanguage;

    public Kill2Live()
    {
#if UNITY_STANDALONE_WIN
        appWindow = FindWindow(null, appTitle);

        // this is important when you are working on the same local machine
        // because server will have the same application title
        SetWindowText(appWindow, appTitle);
        appWindow = FindWindow(null, appTitle);
#endif
    }

    public static bool Ready()
    {
        if (!Language.ready) return false;
        return true;
    }

    private void Awake()
    {
        // quality settings
        QualitySettings.SetQualityLevel(1, true);
        Application.targetFrameRate = 60;
        Application.runInBackground = true;

        // set itself for static access
        self = this;

        // resolution
        Screen.SetResolution(resolutionX, resolutionY, false);
    }

    IEnumerator Start()
    {
        UIMessageGlobal.Load();
        while (!Ready()) { yield return null; }

        // start with login screen
        if (initialPanel != "")
        {
            GetComponent<Account>().PanelShow(initialPanel);
        }
    }

    
    private void Update()
    {
        // move window
#if UNITY_STANDALONE_WIN
        if (moveWindow && Input.GetKeyDown(KeyCode.Mouse0))
        {
            ReleaseCapture(appWindow);
            SendMessage(appWindow, WM_SYSCOMMAND, MOUSE_MOVE, ref nul);     
        }
#endif
    }

    // mark for windows movement
    public void MoveWindow(bool b)
    {
        moveWindow = b;
    }

    // quit
    private void OnApplicationQuit()
    {
        quiting = true;
    }
    public void QuitUI(){ Application.Quit(); }

    // restart
    public static void Restart()
    {
        UDP.Clear();
        UIPlayers.Clear();
        Match.Clear();
        Account.Clear();
        
        SceneManager.LoadScene(0);
    }

    // run the client
    public static void Client()
    {
        self.ClientScene();
    }

    public void ClientScene()
    {
        StartCoroutine(ClientSceneAsync());
    }

    IEnumerator ClientSceneAsync()
    {
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(clientScene, LoadSceneMode.Additive);
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                if (asyncLoad.progress >= 0.9f)
                {
                    uiLobby.SetActive(false);
                    asyncLoad.allowSceneActivation = true;

                    GetComponent<Match>().uiPlay.SetActive(false);
                    GetComponent<Match>().uiJoin.SetActive(true);

#if UNITY_STANDALONE_WIN
                    if (!Debug.isDebugBuild)
                    {
                        ShowWindow(appWindow, SW_SHOWMAXIMIZED);
                    }
#endif
                }

                yield return null;
            }
        }
    }

    public void WindowNormal()
    {
        if (uiLobby == null) return;
        uiLobby.SetActive(true);

#if UNITY_STANDALONE_WIN
        if (!Debug.isDebugBuild)
        {
            ShowWindow(appWindow, SW_SHOWNORMAL);
        }
#endif
    }

    // independent ui buttons
    public void HelpUI() { Application.OpenURL("http://www.kill2live.com"); }
    public void SettingsUI() { uiSettings.SetActive(true); }


#if UNITY_STANDALONE_WIN
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(String className, 
        String windowName);
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, 
        ref int lParam);
    [DllImport("user32.dll")]
    public static extern int ReleaseCapture(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(IntPtr hWnd, String lpString);
#endif
}
