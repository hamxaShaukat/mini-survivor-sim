using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FastDialogueUI : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI responseText;
    public Button submitButton;
    
    private FastOllamaNPC currentNPC;
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
    
    public void StartDialogue(FastOllamaNPC npc)
    {
        if (npc == null || dialoguePanel.activeSelf) return;
        
        currentNPC = npc;
        dialoguePanel.SetActive(true);
        
        // Freeze time slightly (optional)
        Time.timeScale = 0.3f;
        
        // UI
        inputField.text = "";
        responseText.text = "Hello traveler...";
        
        inputField.Select();
        inputField.ActivateInputField();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void OnSubmit()
    {
        if (currentNPC == null || string.IsNullOrEmpty(inputField.text) || isWaiting)
            return;
        
        string message = inputField.text;
        responseText.text = "🤔";
        isWaiting = true;
        
        currentNPC.GetResponse(message, (response) => {
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
        
        // Restore time
        Time.timeScale = 1f;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void OnGUI()
    {
        if (dialoguePanel.activeSelf) return;
        
        FastOllamaNPC[] npcs = FindObjectsOfType<FastOllamaNPC>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null) return;
        
        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius)
            {
                // Simple text
                GUI.Label(new Rect(Screen.width/2 - 50, Screen.height - 60, 100, 30), 
                         "[T] Talk");
                break;
            }
        }
    }
}