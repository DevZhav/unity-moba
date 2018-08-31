using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIPlayer : MonoBehaviour {

    public Player player;
    public Image charImage;
    public Button charBtn;
    public Text nick;
    public Image circleImage;

    private Color circleNormal = new Color(0.5f, 0.2f, 0.2f, 1f);
    private Color circleHover = new Color(0.8f, 0.2f, 0.2f, 1f);
    private Color circleDisabled = new Color(0.25f, 0.25f, 0.25f, 1f);

    public void Set(Player player)
    {
        nick.text = player.PlayerName;
        charImage.sprite = Character.GetImage(player.Character);
        // TODO check with ID
        if(player.PlayerName != Account.GetUser().Nickname)
        {
            charBtn.interactable = false;
            circleImage.color = circleDisabled;
        }
    }

    public void ButtonHover(bool toggle)
    {
        if (!charBtn.interactable) return;
        circleImage.color = (toggle) ? circleHover : circleNormal;
    }
}
