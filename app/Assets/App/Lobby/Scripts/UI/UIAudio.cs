using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class UIAudio : MonoBehaviour {
    
    public static float multiplier = 1.0f;

    private AudioSource source;
    private bool active = true;
    private float volume = 1f;

    public float volButton = 0.3f;

    public AudioClip btnClick;
    public AudioClip btnHover;
    public Toggle toggle;
    public Slider slider;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null || slider == null || toggle == null)
        {
            Debug.LogWarning("Please set all variables to make audio " +
                "work properly");
            return;
        }

        volume = PlayerPrefs.HasKey("volume") ? PlayerPrefs.GetFloat("volume") : 1f;
        slider.value = volume;
        multiplier = volume;

        active = (PlayerPrefs.HasKey("sound") && PlayerPrefs.GetInt("sound") == 1) ? true : false;
        toggle.isOn = active;
    }

    private void Play(AudioClip clip)
    {
        if (source == null) return;
        source.Stop();
        source.clip = clip;
        source.Play();
    }

	public void Toggle()
    {
        active = !active;
        PlayerPrefs.SetInt("sound", (active ? 1 : 0));
    }

    public void Change()
    {
        PlayerPrefs.SetFloat("volume", slider.value);
        multiplier = slider.value;
    }

    public void BtnClick()
    {
        if (source == null || btnClick == null) return;
        source.volume = volButton * multiplier;
        Play(btnClick);
    }
    public void BtnHover()
    {
        if (source == null || btnHover == null) return;
        source.volume = volButton * multiplier;
        Play(btnHover);
    }
}
