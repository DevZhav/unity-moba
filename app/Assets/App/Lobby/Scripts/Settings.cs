using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings class
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Kill2Live))]
public class Settings : MonoBehaviour {

    public Text tou;

    private IEnumerator Start()
    {
        while (!Kill2Live.Ready()) { yield return null; }
        if (tou != null)
        {
            Debug.Log("Loading TOU...");
            
            ResourceRequest txtAsync = Resources.LoadAsync("Translations/tou_" +
                Language.l.ToString(), typeof(TextAsset));

            while (!txtAsync.isDone)
            {
                yield return null;
            }

            if (txtAsync.asset != null)
            {
                TextAsset txtAsset = txtAsync.asset as TextAsset;
                tou.text = txtAsset.text;
            }
        }
    }
}
