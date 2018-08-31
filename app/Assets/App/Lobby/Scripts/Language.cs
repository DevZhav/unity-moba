using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Language class
/// </summary>
[DisallowMultipleComponent]
public class Language : MonoBehaviour {

    public static bool ready= false;
    public static int l = 0;
    public static Dictionary<string, string> v;

    public bool active = true;
    public Dropdown dropdown;

    private void Awake()
    {
        if (ready) return;

        if (PlayerPrefs.HasKey("language"))
        {
            l = PlayerPrefs.GetInt("language");
        }

        if (!active && dropdown != null)
        {
            dropdown.transform.parent.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        Debug.Log("Loading languages...");

        ResourceRequest langAsync = Resources.LoadAsync("languages", 
            typeof(TextAsset));

        while (!langAsync.isDone)
        {
            yield return null;
        }

        if (langAsync.asset != null)
        {
            if (dropdown != null)
            {
                TextAsset langAsset = langAsync.asset as TextAsset;
                string langJson = langAsset.text;

                string[] langs = JsonConvert.DeserializeObject<string[]>(langJson);
                dropdown.ClearOptions();

                // load dropdown options
                List<Dropdown.OptionData> ods = new List<Dropdown.OptionData>();
                foreach (string l in langs)
                {
                    ods.Add(new Dropdown.OptionData(l));
                }
                dropdown.AddOptions(ods);

                dropdown.value = l;
            }

            StartCoroutine(Translations());

            if (!active)
            {
                foreach (UILanguage o in FindObjectsOfType<UILanguage>())
                {
                    StartCoroutine(o.Start());
                }
            }
        }
    }

    private IEnumerator Translations()
    {
        ResourceRequest langAsync = Resources.LoadAsync("Translations/" + l,
            typeof(TextAsset));

        while (!langAsync.isDone)
        {
            yield return null;
        }

        TextAsset langAsset = langAsync.asset as TextAsset;
        string langJson = langAsset.text;

        v = JsonConvert.DeserializeObject<Dictionary<string, string>>
            (langJson);
        ready = true;

        Debug.Log("Translations are loaded.");
    }

    public void UIChange()
    {
        active = false;
        PlayerPrefs.SetInt("language", dropdown.value);
        StartCoroutine(Load());
    }
}
