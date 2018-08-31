using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(InputField))]
public class UITab : MonoBehaviour {

    private InputField self;
    public bool activate = false;
    public InputField next;
    public Button enter;

    private void Awake()
    {
        self = GetComponent<InputField>();
    }

    private void Start()
    {
        if(next == null)
        {
            next = self;
        }

        // if the container of this object is activated, focus first one
        // parent of the form | 2 objects up
        if(activate && transform.parent.parent.gameObject.activeSelf)
        {
            self.Select();
        }
    }

    private void FixedUpdate()
    {
        if(self != null && self.isFocused && !UIMessageGlobal.IsActive())
        {
            if(Input.GetKeyDown(KeyCode.Tab))
            {
                next.Select();
            } else if(enter != null &&
                (Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                enter.onClick.Invoke();
            }
        }
    }

}
