using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("=== UI REFERENCES ===")]
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI npcResponseText;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI thinkingText;
    public Image npcAvatar;
    public Button submitButton;
    public Button closeButton;
    public GameObject typingIndicator;
    
    [Header("=== SETTINGS ===")]
    public KeyCode toggleKey = KeyCode.T;
    public float typingSpeed = 0.03f;
    public float interactionRange = 5f;
    public bool freezePlayerDuringDialogue = true;
    public bool showThinkingProcess = false;
    
    [Header("=== COLORS & STYLES ===")]
    public Color playerTextColor = Color.cyan;
    public Color npcTextColor = Color.yellow;
    public Color thinkingTextColor = Color.gray;
    
    [Header("=== INTERACTION PROMPT ===")]
    public bool showInteractionPrompt = true;
    public string promptText = "Press T to Talk";
    public Color promptColor = Color.white;
    public int promptFontSize = 20;
    
    // Private variables
    private GeminiNPC currentGeminiNPC;
    private SmartDialogueSystem currentSimpleNPC;
    private bool isTypingResponse = false;
    private bool isWaitingForAI = false;
    private string currentNPCName = "";
    
    // Player control
    private GameObject playerObject;
    private bool wasCursorVisible;
    private CursorLockMode previousCursorLock;
    
    // Conversation history UI
    private string conversationHistory = "";
    
    void Start()
    {
        // Initialize UI
        dialoguePanel.SetActive(false);
        if (typingIndicator != null) typingIndicator.SetActive(false);
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);
        
        // Setup button listeners
        submitButton.onClick.AddListener(OnSubmit);
        closeButton.onClick.AddListener(CloseDialogue);
        
        // Input field setup
        inputField.onSubmit.AddListener(delegate { OnSubmit(); });
        inputField.onSelect.AddListener(delegate { 
            if (dialoguePanel.activeSelf) inputField.ActivateInputField(); 
        });
        
        // Find player
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            playerObject = GameObject.Find("Player");
        }
        
        // Initial cursor state
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("DialogueUI Initialized. Press '" + toggleKey + "' near NPCs to talk.");
    }
    
    void Update()
    {
        // Toggle dialogue with T key
        if (Input.GetKeyDown(toggleKey) && !dialoguePanel.activeSelf && !isWaitingForAI)
        {
            TryStartDialogue();
        }
        
        // Close with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
        
        // Handle Space key carefully
        if (Input.GetKeyDown(KeyCode.Space) && dialoguePanel.activeSelf)
        {
            if (!inputField.isFocused && !isTypingResponse && !isWaitingForAI)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }
        
        // Auto-focus input field
        if (dialoguePanel.activeSelf && !inputField.isFocused && !isTypingResponse && !isWaitingForAI)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }
    
    void TryStartDialogue()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found!");
            return;
        }
        
        // Raycast to find NPC
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            // Try Gemini NPC first
            GeminiNPC geminiNPC = hit.collider.GetComponent<GeminiNPC>();
            if (geminiNPC != null)
            {
                StartDialogueWithNPC(geminiNPC);
                return;
            }
            
            // Fallback to simple NPC
            SmartDialogueSystem simpleNPC = hit.collider.GetComponent<SmartDialogueSystem>();
            if (simpleNPC != null)
            {
                StartDialogueWithNPC(simpleNPC);
                return;
            }
        }
        
        Debug.Log("No NPC found within range.");
    }
    
    public void StartDialogueWithNPC(GeminiNPC npc)
    {
        if (npc == null) return;
        
        currentGeminiNPC = npc;
        currentSimpleNPC = null;
        currentNPCName = npc.npcName;
        
        OpenDialoguePanel();
        
        // Auto-greet
        AddToConversation("", "Greetings! How can I help you today?", false);
        inputField.Select();
        inputField.ActivateInputField();
        
        Debug.Log($"Started dialogue with {currentNPCName} (Gemini AI)");
    }
    
    public void StartDialogueWithNPC(SmartDialogueSystem npc)
    {
        if (npc == null) return;
        
        currentSimpleNPC = npc;
        currentGeminiNPC = null;
        currentNPCName = npc.knowledge.npcName;
        
        OpenDialoguePanel();
        
        // Auto-greet
        string greeting = npc.ProcessPlayerInput("hello");
        AddToConversation("", greeting, false);
        inputField.Select();
        inputField.ActivateInputField();
        
        Debug.Log($"Started dialogue with {currentNPCName} (Simple AI)");
    }
    
    void OpenDialoguePanel()
    {
        dialoguePanel.SetActive(true);
        npcNameText.text = currentNPCName;
        
        // Freeze player
        if (freezePlayerDuringDialogue && playerObject != null)
        {
            FreezePlayer(true);
        }
        
        // Clear input
        inputField.text = "";
        conversationHistory = "";
        
        // Update thinking text visibility
        if (thinkingText != null)
        {
            thinkingText.gameObject.SetActive(showThinkingProcess);
        }
    }
    
    void OnSubmit()
    {
        if (string.IsNullOrWhiteSpace(inputField.text))
            return;
        
        string playerMessage = inputField.text.Trim();
        
        // Add player message to conversation
        AddToConversation("You", playerMessage, true);
        
        // Clear input field
        inputField.text = "";
        
        // Process based on NPC type
        if (currentGeminiNPC != null)
        {
            ProcessWithGemini(playerMessage);
        }
        else if (currentSimpleNPC != null)
        {
            ProcessWithSimpleAI(playerMessage);
        }
        
        // Keep focus on input field
        inputField.Select();
        inputField.ActivateInputField();
    }
    
    void ProcessWithGemini(string playerMessage)
    {
        if (currentGeminiNPC == null) return;
        
        isWaitingForAI = true;
        
        // Show thinking indicator
        if (typingIndicator != null) typingIndicator.SetActive(true);
        if (thinkingText != null && showThinkingProcess)
        {
            thinkingText.text = $"{currentNPCName} is thinking...";
            thinkingText.color = thinkingTextColor;
        }
        
        // Send to Gemini
        currentGeminiNPC.ProcessPlayerMessage(playerMessage, (response) => {
            StartCoroutine(DisplayGeminiResponse(response));
        });
    }
    
    IEnumerator DisplayGeminiResponse(string response)
    {
        // Hide thinking indicator
        if (typingIndicator != null) typingIndicator.SetActive(false);
        
        if (showThinkingProcess && thinkingText != null)
        {
            thinkingText.text = "";
        }
        
        // Display response with typing effect
        yield return StartCoroutine(TypeResponse(response));
        
        isWaitingForAI = false;
    }
    
    void ProcessWithSimpleAI(string playerMessage)
    {
        if (currentSimpleNPC == null) return;
        
        string response = currentSimpleNPC.ProcessPlayerInput(playerMessage);
        StartCoroutine(TypeResponse(response));
    }
    
    IEnumerator TypeResponse(string response)
    {
        isTypingResponse = true;
        
        // Clear previous response if needed
        if (!showThinkingProcess)
        {
            AddToConversation("", "", false);
        }
        
        // Type out character by character
        string typedText = "";
        for (int i = 0; i < response.Length; i++)
        {
            typedText += response[i];
            UpdateResponseText(typedText);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Add to conversation history
        AddToConversation(currentNPCName, response, false);
        
        isTypingResponse = false;
    }
    
    void UpdateResponseText(string text)
    {
        if (showThinkingProcess && thinkingText != null)
        {
            thinkingText.text = text;
        }
        else
        {
            // Find the last NPC response and update it
            string[] lines = conversationHistory.Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].StartsWith(currentNPCName + ":"))
                {
                    lines[i] = currentNPCName + ": " + text;
                    conversationHistory = string.Join("\n", lines);
                    npcResponseText.text = FormatConversation(conversationHistory);
                    return;
                }
            }
            
            // If no existing line, add new one
            AddToConversation(currentNPCName, text, false);
        }
    }
    
    void AddToConversation(string speaker, string message, bool isPlayer)
    {
        if (!string.IsNullOrEmpty(speaker))
        {
            conversationHistory += $"{speaker}: {message}\n\n";
        }
        
        // Update display
        npcResponseText.text = FormatConversation(conversationHistory);
        
        // Auto-scroll to bottom
        Canvas.ForceUpdateCanvases();
        if (npcResponseText.transform.parent.GetComponent<ScrollRect>() != null)
        {
            npcResponseText.transform.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }
    
    string FormatConversation(string history)
    {
        string formatted = "";
        string[] lines = history.Split('\n');
        
        foreach (string line in lines)
        {
            if (line.StartsWith("You:"))
            {
                formatted += $"<color=#{ColorUtility.ToHtmlStringRGB(playerTextColor)}>{line}</color>\n";
            }
            else if (line.Contains(":"))
            {
                formatted += $"<color=#{ColorUtility.ToHtmlStringRGB(npcTextColor)}>{line}</color>\n";
            }
            else
            {
                formatted += line + "\n";
            }
        }
        
        return formatted;
    }
    
    void FreezePlayer(bool freeze)
    {
        if (playerObject == null) return;
        
        if (freeze)
        {
            // Save current state
            wasCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            
            // Disable all MonoBehaviours on player
            MonoBehaviour[] components = playerObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp != this && comp.enabled)
                {
                    comp.enabled = false;
                }
            }
            
            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Enable all MonoBehaviours on player
            MonoBehaviour[] components = playerObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp != this)
                {
                    comp.enabled = true;
                }
            }
            
            // Restore cursor
            Cursor.visible = wasCursorVisible;
            Cursor.lockState = previousCursorLock;
        }
    }
    
    public void CloseDialogue()
    {
        // Stop any ongoing typing
        StopAllCoroutines();
        isTypingResponse = false;
        isWaitingForAI = false;
        
        // Hide indicators
        if (typingIndicator != null) typingIndicator.SetActive(false);
        if (thinkingText != null) thinkingText.gameObject.SetActive(false);
        
        // Unfreeze player
        if (freezePlayerDuringDialogue)
        {
            FreezePlayer(false);
        }
        
        // Close panel
        dialoguePanel.SetActive(false);
        
        // Clear references
        currentGeminiNPC = null;
        currentSimpleNPC = null;
        currentNPCName = "";
        
        Debug.Log("Dialogue closed");
    }
    
    void OnGUI()
    {
        if (!showInteractionPrompt || dialoguePanel.activeSelf) return;
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // Check for NPC in sight
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.GetComponent<GeminiNPC>() != null || 
                hit.collider.GetComponent<SmartDialogueSystem>() != null)
            {
                float distance = Vector3.Distance(mainCamera.transform.position, hit.point);
                float alpha = Mathf.Clamp01(2f - (distance / interactionRange));
                
                // Create style
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = promptFontSize;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = new Color(promptColor.r, promptColor.g, promptColor.b, alpha);
                
                // Draw prompt
                int width = 200;
                int height = 30;
                int x = Screen.width / 2 - width / 2;
                int y = Screen.height - 100;
                
                GUI.Label(new Rect(x, y, width, height), promptText, style);
            }
        }
    }
    
    [ContextMenu("Test Dialogue System")]
    void TestSystem()
    {
        Debug.Log("=== DIALOGUE UI TEST ===");
        Debug.Log($"Toggle Key: {toggleKey}");
        Debug.Log($"Freeze Player: {freezePlayerDuringDialogue}");
        Debug.Log($"Interaction Range: {interactionRange}");
        
        // Find NPCs
        GeminiNPC[] geminiNPCs = FindObjectsOfType<GeminiNPC>();
        SmartDialogueSystem[] simpleNPCs = FindObjectsOfType<SmartDialogueSystem>();
        
        Debug.Log($"Found {geminiNPCs.Length} Gemini NPCs");
        Debug.Log($"Found {simpleNPCs.Length} Simple NPCs");
        
        if (geminiNPCs.Length > 0)
        {
            Debug.Log("Gemini NPCs require API key setup!");
        }
    }
}