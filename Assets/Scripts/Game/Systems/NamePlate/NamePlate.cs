using UnityEngine;
using UnityEngine.UI;

public class NamePlate : MonoBehaviour 
{
    public Transform namePlateRoot;
    public Text nameText;
    public RawImage icon;
    
    public Color friendColor = Color.cyan;    
    public Color enemyColor = Color.red;
    public float maxNameDistance = 50f;
}
