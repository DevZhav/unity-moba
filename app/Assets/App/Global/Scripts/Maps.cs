using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Maps : MonoBehaviour {

    public static bool ready = false;

    public static IEnumerator LoadAsync(MatchType typ)
    {
        string mapName = MapName(typ);

        AsyncOperation asyncLoad =
            SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                if (asyncLoad.progress >= 0.9f)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            ready = true;
        }
    }

    public static void UnloadAsync(MatchType typ)
    {
        string mapName = MapName(typ);

        SceneManager.UnloadSceneAsync(mapName);
        SceneManager.UnloadSceneAsync("Client");
    }

    private static string MapName(MatchType typ)
    {
        string mapName = "Map";
        switch (typ)
        {
            case MatchType.One: mapName += "1vs1"; break;
            case MatchType.Two: mapName += "2vs2"; break;
            case MatchType.Three: mapName += "3vs3"; break;
            case MatchType.Four: mapName += "4vs4"; break;
            case MatchType.Five: mapName += "5vs5"; break;
            case MatchType.DM: mapName += "DM"; break;
            case MatchType.Battle: mapName += "Battle"; break;
        }

        return mapName;
    }
}
