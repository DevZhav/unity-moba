using UnityEngine;
using UnityEngine.UI;

public class HealthTest : MonoBehaviour
{
    public const int maxHealth = 100;
    public bool destroyOnDeath;

    public int currentHealth = maxHealth;

    public string playerName = "???";

    public RectTransform healthBar;
    public Image healthCircle;
    public Text playerNameText;

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        GetComponent<CharBase>().animator.SetTrigger("Damage");

        if (currentHealth <= 0)
        {
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                currentHealth = maxHealth;
            }
        }

        healthBar.sizeDelta =
            new Vector2(currentHealth, healthBar.sizeDelta.y);

        if (healthCircle != null)
        {
            healthCircle.fillAmount = currentHealth / 100f;
        }
    }
}