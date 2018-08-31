using UnityEngine;

public class UILoading : MonoBehaviour {

    private RectTransform rectComponent;
    private readonly float rotateSpeed = 400f;

    private void Start()
    {
        rectComponent = GetComponent<RectTransform>();
    }

    private void Update()
    {
        rectComponent.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}
