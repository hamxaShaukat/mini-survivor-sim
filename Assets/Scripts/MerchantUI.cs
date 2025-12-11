using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MerchantUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject merchantPanel;
    public TextMeshProUGUI merchantNameText;
    public TextMeshProUGUI merchantInfoText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI goodsReceivedText;
    public TextMeshProUGUI goodsSoldText;
    public TextMeshProUGUI profitText;
    
    [Header("Close Button")]
    public Button closeButton;
    
    [Header("Buy/Sell Buttons (Optional)")]
    public Button buyButton;
    public Button sellButton;
    
    private MerchantInteraction currentMerchant;
    
    void Start()
    {
        merchantPanel.SetActive(false);
        closeButton.onClick.AddListener(CloseMerchantUI);
        
        // Optional buy/sell buttons
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
            
        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && merchantPanel.activeSelf)
        {
            CloseMerchantUI();
        }
    }
    
    public void OpenMerchantUI(MerchantInteraction merchant)
    {
        if (merchant == null || merchantPanel.activeSelf) return;
        
        currentMerchant = merchant;
        merchantPanel.SetActive(true);
        UpdateMerchantInfo(merchant);
        
        // Freeze player (similar to NPC dialogue)
        FreezePlayer(true);
    }
    
    public void UpdateMerchantInfo(MerchantInteraction merchant)
    {
        if (merchant == null) return;
        
        currentMerchant = merchant;
        
        // Update all UI elements
        merchantNameText.text = merchant.merchantName;
        merchantInfoText.text = merchant.GetMerchantInfo();
        goldText.text = $"💰 Gold: {merchant.merchantGold}";
        goodsReceivedText.text = $"📦 Goods Received: {merchant.goodsReceived}";
        goodsSoldText.text = $"🛒 Goods Sold: {merchant.goodsSold}";
        profitText.text = $"📈 Total Profit: {merchant.totalProfit}";
    }
    
    void OnBuyClicked()
    {
        // Implement buying from merchant
        Debug.Log("Buy button clicked");
        // You could open a shop interface here
    }
    
    void OnSellClicked()
    {
        // Implement selling to merchant
        Debug.Log("Sell button clicked");
        // You could sell player's items here
    }
    
    void FreezePlayer(bool freeze)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Disable movement scripts
        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
                script.enabled = !freeze;
        }
        
        // Cursor
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }
    
    public void CloseMerchantUI()
    {
        merchantPanel.SetActive(false);
        currentMerchant = null;
        FreezePlayer(false);
    }
    
    // Optional: Show "Press T" when near merchant
    void OnGUI()
    {
        if (merchantPanel.activeSelf) return;
        
        MerchantInteraction[] merchants = FindObjectsOfType<MerchantInteraction>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null || merchants.Length == 0) return;
        
        foreach (var merchant in merchants)
        {
            float distance = Vector3.Distance(player.transform.position, merchant.transform.position);
            if (distance <= merchant.interactionRadius)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 12;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.cyan;
                
                GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 70, 200, 30),
                         $"Press T to trade with {merchant.merchantName}", style);
                break;
            }
        }
    }
}