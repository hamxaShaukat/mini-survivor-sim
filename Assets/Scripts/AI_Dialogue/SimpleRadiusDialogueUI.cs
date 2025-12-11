using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SimpleRadiusDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI responseText;
    public Button submitButton;
    public Button closeButton;
    
    [Header("Settings")]
    public bool freezePlayerDuringDialogue = true;
    
    // Private
    private SimpleOllamaNPC currentNPC;
    private bool isTyping = false;
    private GameObject player;
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        
        submitButton.onClick.AddListener(OnSubmit);
        closeButton.onClick.AddListener(CloseDialogue);
        
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
        
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        Debug.Log("Radius Dialogue UI Ready");
    }
    
    void Update()
    {
        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
        
        // Auto-focus input field
        if (dialoguePanel.activeSelf && !inputField.isFocused && !isTyping)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    public void StartDialogueWithNPC(SimpleOllamaNPC npc)
    {
        if (npc == null || dialoguePanel.activeSelf) return;
        
        currentNPC = npc;
        dialoguePanel.SetActive(true);
        npcNameText.text = npc.npcName;
        
        // Freeze player
        if (freezePlayerDuringDialogue && player != null)
        {
            FreezePlayer(true);
        }
        
        // Clear and focus
        inputField.text = "";
        responseText.text = "";
        
        // Auto-greet
        currentNPC.GetResponse("Hello", (response) => {
            StartCoroutine(TypeResponse(response));
        });
        
        inputField.Select();
        inputField.ActivateInputField();
    }
    
    void OnSubmit()
    {
        if (currentNPC == null || string.IsNullOrEmpty(inputField.text) || isTyping)
            return;
        
        string message = inputField.text;
        responseText.text = "Thinking...";
        
        currentNPC.GetResponse(message, (response) => {
            StartCoroutine(TypeResponse(response));
        });
        
        inputField.text = "";
        inputField.Select();
    }
    
    IEnumerator TypeResponse(string text)
    {
        isTyping = true;
        responseText.text = "";
        
        foreach (char c in text)
        {
            responseText.text += c;
            yield return new WaitForSeconds(0.02f);
        }
        
        isTyping = false;
    }
    
    void FreezePlayer(bool freeze)
    {
        if (player == null) return;
        
        if (freeze)
        {
            // Disable all scripts on player
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script != this && script.enabled)
                    script.enabled = false;
            }
            
            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Enable all scripts
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                if (script != this)
                    script.enabled = true;
            }
            
            // Hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    public void CloseDialogue()
    {
        if (isTyping) return;
        
        dialoguePanel.SetActive(false);
        
        // Unfreeze player
        if (freezePlayerDuringDialogue)
        {
            FreezePlayer(false);
        }
        
        currentNPC = null;
    }
    
    void OnGUI()
    {
        // Simple "Press T" indicator when player is in radius
        if (player == null || dialoguePanel.activeSelf) return;
        
        // Check all NPCs in scene
        SimpleOllamaNPC[] allNPCs = FindObjectsOfType<SimpleOllamaNPC>();
        
        foreach (var npc in allNPCs)
        {
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius)
            {
                // Draw "Press T" prompt
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.green;
                
                GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 80, 200, 30), 
                         "Press T to talk", style);
                
                break; // Only show for one NPC at a time
            }
        }
    }
}