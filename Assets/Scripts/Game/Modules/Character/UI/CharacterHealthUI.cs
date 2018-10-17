using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthUI : MonoBehaviour
{
    public HealthState health;

    public void UpdateUI()
    {
        if (m_Health != health.health)
        {
            m_Health = health.health;
            m_HealthText.text = (Mathf.CeilToInt(m_Health)).ToString();
        }

        if (m_MaxHealth != health.maxHealth)
        {
            m_MaxHealth = health.maxHealth;
            m_MaxHealthText.text = "/" + ((int)m_MaxHealth).ToString();
        }
    }

    [SerializeField] Text m_HealthText;
    [SerializeField] Text m_MaxHealthText;

    float m_Health = -1;
    float m_MaxHealth = -1;
}
