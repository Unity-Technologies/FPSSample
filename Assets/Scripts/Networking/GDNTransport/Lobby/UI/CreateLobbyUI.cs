using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreateLobbyUI : MonoBehaviour {
    public TMP_InputField baseName;
    public TMP_Dropdown gameMode;

    public GameObject nameError;

    public void NameError(bool val) {
        nameError.SetActive(val);
    }

}
