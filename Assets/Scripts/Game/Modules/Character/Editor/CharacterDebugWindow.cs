using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public class CharacterDebugWindow : EditorWindow
{
    private static Character character;
    private static Character[] availableCharacters;


    struct DamageInfo
    {
        public Vector3 startPosition;
        public Quaternion direction;
        public float impulse;
        public float damage;
    }

    private static DamageInfo damageInfo; 
    
    [MenuItem("FPS Sample/Windows/Character Debug")]
    public static void ShowWindow()
    {
        GetWindow<CharacterDebugWindow>(false, "Char Debug", true);

        ScanForCharacters();       
    }
    
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += change => ScanForCharacters();
        SceneView.onSceneGUIDelegate += OnSceneGuiDelegate;
    }

    private void OnSceneGuiDelegate(SceneView sceneview)
    {
        if (character == null)
            return;

        var damStart = character.transform.position + damageInfo.startPosition;
        var damVector = damageInfo.direction*Vector3.forward * damageInfo.impulse;    
        Debug.DrawLine(damStart, damStart + damVector, Color.red);
        DebugDraw.Sphere(damStart, 0.1f, Color.red);
    }

    static void ScanForCharacters()
    {
        availableCharacters = FindObjectsOfType<Character>();
        if(availableCharacters.Length > 0)
            character = availableCharacters[0];
    }
    
    void OnGUI()
    {
        GUILayout.Label("CHARACTER", EditorStyles.boldLabel);
        // Character selection
        if (GUILayout.Button("Scan for chars"))
        {
            ScanForCharacters();
        }

        if (availableCharacters != null)
        {
            var charNames = new string[availableCharacters.Length];
            var selectedindex = -1;
            for (var i = 0; i < availableCharacters.Length; i++)
            {
                charNames[i] = availableCharacters[i].name;
                if (availableCharacters[i] == character)
                    selectedindex = i;
            }

            selectedindex = EditorGUILayout.Popup("Char", selectedindex, charNames);
            if(selectedindex >= 0 && selectedindex < availableCharacters.Length)
                character = availableCharacters[selectedindex];
        }
        
        if (character == null)
        {
            GUILayout.Label("Please select character ...");
            return;
        }
        
        GUILayout.Label("DAMAGE", EditorStyles.boldLabel);
        
        // Give damage
        damageInfo.damage = EditorGUILayout.FloatField("damage", damageInfo.damage);
        damageInfo.impulse = EditorGUILayout.FloatField("impulse", damageInfo.impulse);
        damageInfo.startPosition = EditorGUILayout.Vector3Field("start", damageInfo.startPosition);
        damageInfo.direction.eulerAngles = EditorGUILayout.Vector3Field("dir", damageInfo.direction.eulerAngles);
        if (GUILayout.Button("Give Damage"))
        {
            var hitCollisionOwner = character.GetComponent<HitCollisionOwner>();
    
            var damageEvent = new DamageEvent
            {
                instigator = Entity.Null,
                damage = damageInfo.damage,
                direction = damageInfo.direction*Vector3.forward,
                impulse = damageInfo.impulse,
            };

            hitCollisionOwner.damageEvents.Add(damageEvent);
        }
    }
    
    
    
}
