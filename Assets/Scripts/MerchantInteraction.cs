using UnityEngine;
using TMPro;

public class MerchantInteraction : MonoBehaviour
{
    [Header("Merchant Identity")]
    public string merchantName = "Merchant";
    public enum MerchantType { General, Blacksmith, Farmer }
    public MerchantType merchantType = MerchantType.General;
    
    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    public Transform interactionPoint; // Where NPCs should stand
    
    [Header("Merchant Economy")]
    public int merchantGold = 1000;
    public int goodsReceived = 0; // Goods bought from NPCs
    public int goodsSold = 0; // Goods sold to players
    public int totalProfit = 0;
    
    [Header("Buy Prices")]
    public int toolBuyPrice = 15; // Pays 15 gold per tool
    public int weaponBuyPrice = 40; // Pays 40 gold per weapon
    public int cropBuyPrice = 8; // Pays 8 gold per crop
    
    [Header("Visual Feedback")]
    public ParticleSystem sellEffect;
    public AudioClip sellSound;
    public GameObject merchantUIPanel; // Reference to UI panel
    
    private AudioSource audioSource;
    private MerchantUI merchantUI;
    
    void Start()
    {
        if (interactionPoint == null)
            interactionPoint = transform;
            
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Find UI if not assigned
        if (merchantUIPanel != null)
        {
            merchantUI = merchantUIPanel.GetComponent<MerchantUI>();
        }
        
        Debug.Log($"[{merchantName}] Ready for business! Gold: {merchantGold}");
    }
    
    // Called by NPCs when they come to sell - CHANGED TO RETURN BOOL
    public bool ReceiveGoodsFromNPC(string npcType, int quantity)
    {
        int payment = 0;
        
        switch (npcType)
        {
            case "Blacksmith":
                // Assume tools for now, could expand
                payment = quantity * toolBuyPrice;
                Debug.Log($"[{merchantName}] Bought {quantity} tools from Blacksmith for {payment} gold");
                break;
                
            case "Farmer":
                payment = quantity * cropBuyPrice;
                Debug.Log($"[{merchantName}] Bought {quantity} crops from Farmer for {payment} gold");
                break;
                
            default:
                Debug.LogWarning($"[{merchantName}] Unknown NPC type: {npcType}");
                return false;
        }
        
        // Check if merchant has enough gold
        if (merchantGold >= payment)
        {
            merchantGold -= payment;
            goodsReceived += quantity;
            totalProfit += (int)(payment * 0.3f); // 30% profit margin
            
            Debug.Log($"[{merchantName}] Transaction successful! Paid {payment} gold");
            return true;
        }
        else
        {
            Debug.LogWarning($"[{merchantName}] Not enough gold to buy {quantity} items! Need {payment}, have {merchantGold}");
            return false;
        }
    }
    
    public bool IsAtMerchant(Vector3 npcPosition)
    {
        return Vector3.Distance(interactionPoint.position, npcPosition) <= interactionRadius;
    }
    
    public Vector3 GetInteractionPosition(Vector3 npcApproachDirection)
    {
        return interactionPoint.position;
    }
    
    public void PlaySellEffects()
    {
        if (sellEffect != null)
            sellEffect.Play();
            
        if (sellSound != null && audioSource != null)
            audioSource.PlayOneShot(sellSound);
    }
    
    // Open merchant UI when player interacts
    public void OpenMerchantUI()
    {
        if (merchantUIPanel != null)
        {
            merchantUIPanel.SetActive(true);
            if (merchantUI != null)
            {
                merchantUI.UpdateMerchantInfo(this);
            }
        }
    }
    
    // Close merchant UI
    public void CloseMerchantUI()
    {
        if (merchantUIPanel != null)
        {
            merchantUIPanel.SetActive(false);
        }
    }
    
    // Get merchant info for display
    public string GetMerchantInfo()
    {
        string info = $"🏪 {merchantName}\n";
        info += $"💰 Gold: {merchantGold}\n";
        info += $"📦 Goods Received: {goodsReceived}\n";
        info += $"🛒 Goods Sold: {goodsSold}\n";
        info += $"📈 Total Profit: {totalProfit}\n";
        info += $"🏷️ Type: {merchantType}";
        
        return info;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint != null ? interactionPoint.position : transform.position, interactionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(interactionPoint != null ? interactionPoint.position : transform.position, 0.2f);
    }
}