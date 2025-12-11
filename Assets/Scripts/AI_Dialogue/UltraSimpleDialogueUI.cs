using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UltraSimpleDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI responseText;
    public Button submitButton;
    
    // Private
    private UltraSimpleOllamaNPC currentNPC;
    private bool isWaiting = false;
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        submitButton.onClick.AddListener(OnSubmit);
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
        
        if (dialoguePanel.activeSelf && !inputField.isFocused && !isWaiting)
        {
            inputField.Select();
        }
    }
    
    public void StartDialogue(UltraSimpleOllamaNPC npc)
    {
        if (npc == null || dialoguePanel.activeSelf) return;
        
        currentNPC = npc;
        dialoguePanel.SetActive(true);
        npcNameText.text = npc.npcName;
        
        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Clear
        inputField.text = "";
        responseText.text = "Hello!";
        
        inputField.Select();
        inputField.ActivateInputField();
    }
    
    void OnSubmit()
    {
        if (currentNPC == null || string.IsNullOrEmpty(inputField.text) || isWaiting)
            return;
        
        string message = inputField.text;
        responseText.text = "Thinking...";
        isWaiting = true;
        
        currentNPC.GetAIResponse(message, (response) => {
            responseText.text = response;
            isWaiting = false;
        });
        
        inputField.text = "";
        inputField.Select();
    }
    
    void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNPC = null;
        
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void OnGUI()
    {
        // Simple "Press T" text when near NPC
        if (dialoguePanel.activeSelf) return;
        
        UltraSimpleOllamaNPC[] npcs = FindObjectsOfType<UltraSimpleOllamaNPC>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null) return;
        
        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                
                GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 80, 200, 30), 
                         "Press T to talk", style);
                break;
            }
        }
    }
}