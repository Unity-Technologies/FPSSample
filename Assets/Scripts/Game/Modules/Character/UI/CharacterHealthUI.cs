using UnityEngine;
using UnityEngine.UI;

public class CharacterHealthUI : MonoBehaviour
{
    public void UpdateUI(ref HealthStateData healthState)
    {
        if (m_Health != healthState.health)
        {
            m_Health = healthState.health;
            m_HealthText.text = (Mathf.CeilToInt(m_Health)).ToString();
        }
    }

    [SerializeField] TMPro.TextMeshProUGUI m_HealthText;

    float m_Health = -1;
}
