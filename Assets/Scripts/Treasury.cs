using UnityEngine;
using TMPro;

public class Treasury : MonoBehaviour
{
    public static Treasury Instance;
    
    [Header("Treasury Settings")]
    public int currentGold = 1000;
    public TextMeshProUGUI goldText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        UpdateDisplay();
    }
    
    public void AddMoney(int amount)
    {
        currentGold += amount;
        UpdateDisplay();
        
        Debug.Log($"💰 Treasury: +{amount} gold. Total: {currentGold}");
    }
    
    public bool SpendMoney(int amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateDisplay();
            Debug.Log($"💰 Treasury: -{amount} gold. Total: {currentGold}");
            return true;
        }
        
        Debug.LogWarning($"Not enough gold! Need {amount}, have {currentGold}");
        return false;
    }
    
    void UpdateDisplay()
    {
        if (goldText != null)
        {
            goldText.text = $"Treasury: {currentGold} gold";
        }
    }
    
    [ContextMenu("Add 100 Gold")]
    void AddTestGold()
    {
        AddMoney(100);
    }
}