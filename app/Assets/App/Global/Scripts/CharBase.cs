using UnityEngine;
using UnityEngine.Networking;

public class CharBase : NetworkBehaviour
{
    [HideInInspector]
    [SyncVar]
    public int cid;

    [HideInInspector]
    public bool isMe = false;

    [HideInInspector]
    public Animator animator;

    private bool loaded = false;

    public override void OnStartLocalPlayer()
    {
        isMe = true;
    }

    public void Load()
    {
        foreach (Transform t in transform)
        {
            animator = t.gameObject.GetComponent<Animator>();
            if (animator == null) continue;
            break;
        }
        if(animator == null)
        {
            Debug.LogWarning("Animator is not found!");
        }

        loaded = true;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (loaded == false || animator == null) return;

        float move = Mathf.Abs(Input.GetAxis("Horizontal")) + 
            Mathf.Abs(Input.GetAxis("Vertical"));
        animator.SetFloat("Speed", move);

        if(Input.GetButtonDown("Fire"))
        {
            animator.SetTrigger("Attack");
            Invoke("AttackEnd", 0.2f);
        }
    }

    private void AttackEnd()
    {
        if (animator == null) return;
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Move");
    }

}
