using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SimpleDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI responseText;
    public TextMeshProUGUI relationshipText;
    public Button submitButton;
    public Button closeButton;
    
    [Header("Settings")]
    public bool freezePlayer = true;
    public float typingSpeed = 0.05f;
    
    private SimpleNPCDialogue currentNPC;
    private bool isTyping = false;
    private GameObject player;
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        
        submitButton.onClick.AddListener(OnSubmit);
        closeButton.onClick.AddListener(CloseDialogue);
        
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
        
        player = GameObject.FindGameObjectWithTag("Player");
        
        Debug.Log("Simple Dialogue UI Ready");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
        
        if (dialoguePanel.activeSelf && !inputField.isFocused && !isTyping)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    public void StartDialogueWithNPC(SimpleNPCDialogue npc)
    {
        if (npc == null || dialoguePanel.activeSelf) return;
        
        currentNPC = npc;
        dialoguePanel.SetActive(true);
        
        // Update UI
        npcNameText.text = npc.npcName;
        UpdateRelationshipDisplay();
        
        // Freeze player
        if (freezePlayer && player != null)
        {
            FreezePlayer(true);
        }
        
        // Clear and focus
        inputField.text = "";
        responseText.text = "";
        
        // Auto-greet
        string greeting = npc.GetResponse("hello");
        StartCoroutine(TypeResponse(greeting));
        
        inputField.Select();
        inputField.ActivateInputField();
    }
    
    void OnSubmit()
    {
        if (currentNPC == null || string.IsNullOrEmpty(inputField.text) || isTyping)
            return;
        
        string playerMessage = inputField.text;
        
        // Get NPC response
        string npcResponse = currentNPC.GetResponse(playerMessage);
        
        // Display with typing effect
        StartCoroutine(TypeResponse(npcResponse));
        
        // Update relationship display
        UpdateRelationshipDisplay();
        
        // Clear input
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
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
    }
    
    void UpdateRelationshipDisplay()
    {
        if (relationshipText != null && currentNPC != null)
        {
            relationshipText.text = $"Relationship: {currentNPC.relationshipWithPlayer}/100\n" +
                                  $"Mood: {currentNPC.currentMood}";
            
            // Color based on relationship
            if (currentNPC.relationshipWithPlayer > 70)
                relationshipText.color = Color.green;
            else if (currentNPC.relationshipWithPlayer > 40)
                relationshipText.color = Color.yellow;
            else
                relationshipText.color = Color.red;
        }
    }
    
    void FreezePlayer(bool freeze)
    {
        if (player == null) return;
        
        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
                script.enabled = !freeze;
        }
        
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }
    
    public void CloseDialogue()
    {
        if (isTyping) return;
        
        dialoguePanel.SetActive(false);
        
        // Unfreeze player
        if (freezePlayer)
        {
            FreezePlayer(false);
        }
        
        currentNPC = null;
    }
    
    void OnGUI()
    {
        // Show "Press T" when near any NPC
        if (dialoguePanel.activeSelf) return;
        
        SimpleNPCDialogue[] npcs = FindObjectsOfType<SimpleNPCDialogue>();
        if (player == null) return;
        
        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 18;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.cyan;
                
                GUI.Label(new Rect(Screen.width/2 - 80, Screen.height - 70, 160, 30),
                         $"Press T to talk to {npc.npcName}", style);
                break;
            }
        }
    }
}