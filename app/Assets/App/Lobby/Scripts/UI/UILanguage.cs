using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Text))]
public class UILanguage : MonoBehaviour {

    public string key;

    public IEnumerator Start()
    {
        yield return new WaitUntil(() => Language.ready == true);

        if(GetComponent<Text>())
        {
            GetComponent<Text>().text = Language.v[key];
        }
    }

}
