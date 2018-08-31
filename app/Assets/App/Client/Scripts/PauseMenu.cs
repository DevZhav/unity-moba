using UnityEngine;

[DisallowMultipleComponent]
public class PauseMenu : MonoBehaviour {

    private bool visible = false;

    public GameObject uiMenu;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            visible = !visible;
            uiMenu.SetActive(visible);
        }
    }

    public void Quit()
    {
        FindObjectOfType<Client>().Disconnect();
    }
}
