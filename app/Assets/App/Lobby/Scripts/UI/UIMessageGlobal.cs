using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMessageGlobal {

    private const string prefabName = "MessageGlobal";

    private static Transform obj;
    private static Text title;
    private static Button close;

    private static bool isServer = false;

    public static void Load()
    {
        // skip if it is server
        Server server = Object.FindObjectOfType<Server>();
        if (server != null)
        {
            isServer = true;
            return;
        }
        // skip if the scene is not lobby
        if(SceneManager.GetActiveScene().name != "Lobby")
        {
            return;
        }

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Global messages will not going to be displayed" +
                " because there is no canvas in the scene");
            return;
        }
        obj = Object.FindObjectOfType<Canvas>().transform.Find(prefabName);
        title = obj.Find("Title").GetComponent<Text>();
        close = obj.Find("Close").GetComponent<Button>();
        close.onClick.AddListener(Close);
    }

    public static void Open(string msg, string detail="")
    {
        if (obj == null || title == null) Load();
        if (!isServer)
        {
            title.text = msg + "\r\n<size=16>" + detail + "</size>";
            obj.gameObject.SetActive(true);
        } else
        {
            Debug.LogWarning(msg);
        }
    }

    public static void Close()
    {
        title.text = "";
        obj.gameObject.SetActive(false);
    }

    public static bool IsActive()
    {
        if (obj == null) return false;
        return obj.gameObject.activeSelf;
    }
}
