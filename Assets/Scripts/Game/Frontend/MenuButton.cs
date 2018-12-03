using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    public bool defaultButton;
    public bool ingameOption;
    public bool mainmenuOption;

    void Start()
    {
        if (defaultButton)
        {
            var gameOjectsWithEventSystems = FindObjectsOfType<EventSystem>();
            if (gameOjectsWithEventSystems.Length > 0)
            {
                EventSystem eventSystem = gameOjectsWithEventSystems[0].GetComponent<EventSystem>();
                if (!eventSystem.currentSelectedGameObject)
                {
                    Debug.Log(string.Format("Activating {0} as currentSelectedGameObject on first Event System {1}", this.name, gameOjectsWithEventSystems[0].name));
                    eventSystem.SetSelectedGameObject(this.gameObject);
                }
            }
            else
            {
                InputField inputField = GetComponent<InputField>();
                if (inputField)
                {
                    Debug.Log(string.Format("Activating {0} with ActivateInputField", this.name));
                    inputField.ActivateInputField();
                }
            }
        }
    }
}
