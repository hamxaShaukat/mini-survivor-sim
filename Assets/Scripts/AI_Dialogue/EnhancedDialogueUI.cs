using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class EnhancedDialogueUI : MonoBehaviour
{
    [Header("=== UI REFERENCES ===")]
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI npcResponseText;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI aiStatusText;
    public Image npcAvatar;
    public Button submitButton;
    public Button closeButton;
    public GameObject typingIndicator;
    
    [Header("=== SETTINGS ===")]
    public KeyCode interactKey = KeyCode.T;
    public float typingSpeed = 0.03f;
    public float interactionRange = 5f;
    public bool freezePlayerDuringDialogue = true;
    
    [Header("=== COLORS ===")]
    public Color playerTextColor = Color.cyan;
    public Color npcTextColor = Color.yellow;
    public Color thinkingColor = Color.gray;
    
    [Header("=== PROMPT UI ===")]
    public bool showInteractionPrompt = true;
    public string promptMessage = "Press T to Talk";
    public Color promptColor = Color.green;
    
    // Private variables
    private OllamaNPC currentNPC;
    private bool isTyping = false;
    private bool isWaitingForResponse = false;
    private string conversationHistory = "";
    private GameObject playerObject;
    
    void Start()
    {
        // Initialize
        dialoguePanel.SetActive(false);
        if (typingIndicator != null) typingIndicator.SetActive(false);
        if (aiStatusText != null) aiStatusText.text = "";
        
        // Setup listeners
        submitButton.onClick.AddListener(OnSubmit);
        closeButton.onClick.AddListener(CloseDialogue);
        
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
        inputField.onSelect.AddListener(delegate { 
            if (dialoguePanel.activeSelf) inputField.ActivateInputField(); 
        });
        
        // Find player
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            playerObject = GameObject.Find("Player");
        
        // Initial cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("EnhancedDialogueUI Ready");
    }
    
    void Update()
    {
        // Toggle dialogue
        if (Input.GetKeyDown(interactKey) && !dialoguePanel.activeSelf && !isWaitingForResponse)
        {
            TryStartDialogue();
        }
        
        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
        
        // Handle Space key
        if (Input.GetKeyDown(KeyCode.Space) && dialoguePanel.activeSelf)
        {
            if (!inputField.isFocused && !isTyping && !isWaitingForResponse)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
        
        // Auto-focus
        if (dialoguePanel.activeSelf && !inputField.isFocused && !isTyping && !isWaitingForResponse)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    void TryStartDialogue()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            OllamaNPC npc = hit.collider.GetComponent<OllamaNPC>();
            if (npc != null)
            {
                StartDialogueWithNPC(npc);
                return;
            }
        }
        
        Debug.Log("No NPC found nearby");
    }
    
    public void StartDialogueWithNPC(OllamaNPC npc)
    {
        if (npc == null) return;
        
        currentNPC = npc;
        
        // Open UI
        dialoguePanel.SetActive(true);
        npcNameText.text = npc.npcName;
        
        // Update AI status
        if (aiStatusText != null)
        {
            aiStatusText.text = npc.enableOllama ? "AI: Ollama" : "AI: Local";
            aiStatusText.color = npc.enableOllama ? Color.green : Color.yellow;
        }
        
        // Freeze player
        if (freezePlayerDuringDialogue && playerObject != null)
        {
            FreezePlayer(true);
        }
        
        // Clear and focus
        inputField.text = "";
        conversationHistory = "";
        npcResponseText.text = "";
        
        // Auto-greet
        AddMessage("System", $"You approach {npc.npcName}...", Color.gray);
        
        currentNPC.ProcessMessage("Hello", (response) => {
            StartCoroutine(TypeResponse(response));
        });
        
        inputField.Select();
        inputField.ActivateInputField();
        
        Debug.Log($"Started dialogue with {npc.npcName}");
    }
    
    void OnSubmit()
    {
        if (string.IsNullOrWhiteSpace(inputField.text) || isTyping || isWaitingForResponse)
            return;
        
        string message = inputField.text.Trim();
        
        // Add player message
        AddMessage("You", message, playerTextColor);
        
        // Clear input
        inputField.text = "";
        
        // Get NPC response
        isWaitingForResponse = true;
        if (typingIndicator != null) typingIndicator.SetActive(true);
        
        currentNPC.ProcessMessage(message, (response) => {
            StartCoroutine(DisplayResponse(response));
        });
        
        // Keep focus
        inputField.Select();
        inputField.ActivateInputField();
    }
    
    IEnumerator DisplayResponse(string response)
    {
        if (typingIndicator != null) typingIndicator.SetActive(false);
        isWaitingForResponse = false;
        
        yield return StartCoroutine(TypeResponse(response));
    }
    
    IEnumerator TypeResponse(string text)
    {
        isTyping = true;
        
        // Start with empty for this NPC message
        string npcLine = $"{currentNPC.npcName}: ";
        npcResponseText.text = npcLine;
        
        // Type out character by character
        for (int i = 0; i < text.Length; i++)
        {
            npcResponseText.text = npcLine + text.Substring(0, i + 1);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Add to conversation history
        AddMessage(currentNPC.npcName, text, npcTextColor);
        
        isTyping = false;
    }
    
    void AddMessage(string speaker, string message, Color color)
    {
        string coloredMessage = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{speaker}: {message}</color>\n\n";
        conversationHistory += coloredMessage;
        
        // Update display (show last 3 messages)
        string[] lines = conversationHistory.Split('\n');
        int start = Math.Max(0, lines.Length - 8); // Last 3 messages
        string displayText = "";
        for (int i = start; i < lines.Length; i++)
        {
            displayText += lines[i] + "\n";
        }
        
        npcResponseText.text = displayText;
        
        // Auto-scroll
        Canvas.ForceUpdateCanvases();
        if (npcResponseText.transform.parent.GetComponent<ScrollRect>() != null)
        {
            npcResponseText.transform.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }
    
    void FreezePlayer(bool freeze)
    {
        if (playerObject == null) return;
        
        if (freeze)
        {
            // Disable all scripts on player
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();
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
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();
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
        // Stop coroutines
        StopAllCoroutines();
        isTyping = false;
        isWaitingForResponse = false;
        
        // Hide indicators
        if (typingIndicator != null) typingIndicator.SetActive(false);
        
        // Unfreeze player
        if (freezePlayerDuringDialogue)
        {
            FreezePlayer(false);
        }
        
        // Close
        dialoguePanel.SetActive(false);
        currentNPC = null;
        
        Debug.Log("Dialogue closed");
    }
    
    void OnGUI()
    {
        if (!showInteractionPrompt || dialoguePanel.activeSelf) return;
        
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Check for NPC
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.GetComponent<OllamaNPC>() != null)
            {
                float distance = Vector3.Distance(cam.transform.position, hit.point);
                float alpha = Mathf.Clamp01(2f - (distance / interactionRange));
                
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(promptColor.r, promptColor.g, promptColor.b, alpha);
                
                int width = 200;
                int height = 30;
                int x = Screen.width / 2 - width / 2;
                int y = Screen.height - 100;
                
                GUI.Label(new Rect(x, y, width, height), promptMessage, style);
            }
        }
    }
    
    [ContextMenu("Test Dialogue System")]
    void TestSystem()
    {
        Debug.Log("=== DIALOGUE SYSTEM TEST ===");
        
        OllamaNPC[] npcs = FindObjectsOfType<OllamaNPC>();
        Debug.Log($"Found {npcs.Length} NPCs with OllamaNPC script");
        
        foreach (var npc in npcs)
        {
            Debug.Log($"- {npc.npcName} (Ollama: {npc.enableOllama})");
        }
    }
}