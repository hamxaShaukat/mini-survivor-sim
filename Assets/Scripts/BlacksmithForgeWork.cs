using UnityEngine;
using System.Collections;

public class BlacksmithForgeWork : MonoBehaviour
{
    [Header("Work Stations")]
    public Transform anvilStation;
    public Transform bellowsStation;
    public Transform storageStation;
    
    [Header("Timing Settings")]
    public float timeAtAnvil = 2f;
    public float timeAtBellows = 2f;
    public float timeAtStorage = 2f;
    public float moveSpeed = 2f;
    public int workCycles = 20;
    
    [Header("Merchant Settings")]
    public Transform[] pathToMerchant; // Waypoints from forge to merchant
    public MerchantInteraction merchant; // Reference to merchant script
    public int cyclesBeforeSelling = 5; // How many work cycles before going to merchant
    public float sellingTime = 7f; // Time spent selling at merchant (matches animation length)
    public float interactionDistance = 2f; // How close to get to merchant
    
    [Header("Animation Settings")]
    public string bellowsTrigger = "StartBellows";
    public string anvilTrigger = "StartAnvil";
    public string storageTrigger = "StartStorage";
    public string sellTrigger = "StartSell"; // NEW: Selling animation trigger
    
    // Private variables
    private Animator animator;
    private CharacterController controller;
    private int completedCycles = 0;
    private int goodsProduced = 0; // Tracks how many items made since last sell
    private bool isWorking = true;
    private bool isGoingToMerchant = false;
    private bool isSelling = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
            Debug.LogError("No Animator found on Blacksmith!");
        else
            Debug.Log("Blacksmith animator ready");
        
        StartCoroutine(BlacksmithLifecycle());
    }
    
    IEnumerator BlacksmithLifecycle()
    {
        Debug.Log("Blacksmith: Starting work lifecycle");
        
        while (isWorking)
        {
            // Phase 1: Work at forge (produce goods)
            yield return StartCoroutine(WorkPhase());
            
            // Phase 2: Go sell goods to merchant
            yield return StartCoroutine(SellToMerchantPhase());
            
            // Phase 3: Return to work
            Debug.Log("Blacksmith: Returning to work after selling");
        }
    }
    
    IEnumerator WorkPhase()
    {
        Debug.Log("Blacksmith: Starting work phase");
        
        int cyclesThisBatch = 0;
        
        while (cyclesThisBatch < cyclesBeforeSelling && completedCycles < workCycles)
        {
            // 1. BELLOWS
            Debug.Log("Blacksmith: Going to bellows");
            yield return StartCoroutine(GoToStation(bellowsStation, WorkStation.Bellows));
            yield return new WaitForSeconds(timeAtBellows);
            
            // 2. ANVIL
            Debug.Log("Blacksmith: Going to anvil");
            yield return StartCoroutine(GoToStation(anvilStation, WorkStation.Anvil));
            yield return new WaitForSeconds(timeAtAnvil);
            
            // 3. STORAGE
            Debug.Log("Blacksmith: Going to storage");
            yield return StartCoroutine(GoToStation(storageStation, WorkStation.Storage));
            yield return new WaitForSeconds(timeAtStorage);
            
            completedCycles++;
            cyclesThisBatch++;
            goodsProduced++;
            
            Debug.Log($"Blacksmith: Completed cycle {completedCycles}/{workCycles}. Goods: {goodsProduced}");
            
            // Brief pause between cycles
            yield return new WaitForSeconds(0.5f);
        }
        
        if (goodsProduced > 0)
        {
            Debug.Log($"Blacksmith: Finished batch of {cyclesThisBatch} cycles. Ready to sell {goodsProduced} items");
        }
    }
    
    IEnumerator SellToMerchantPhase()
    {
        if (goodsProduced == 0 || merchant == null || pathToMerchant == null || pathToMerchant.Length == 0)
        {
            Debug.Log("Blacksmith: No goods to sell or merchant not set up");
            yield break;
        }
        
        isGoingToMerchant = true;
        Debug.Log($"Blacksmith: Going to merchant with {goodsProduced} items");
        
        // 1. Follow path to merchant
        foreach (Transform waypoint in pathToMerchant)
        {
            if (waypoint != null)
            {
                yield return StartCoroutine(MoveToPosition(waypoint.position));
            }
        }
        
        // 2. Go to merchant's interaction point
        Vector3 merchantPosition = merchant.transform.position;
        Vector3 directionToMerchant = (merchantPosition - transform.position).normalized;
        Vector3 interactionPoint = merchantPosition - directionToMerchant * interactionDistance;
        
        yield return StartCoroutine(MoveToPosition(interactionPoint));
        
        // 3. Face the merchant
        Vector3 lookDirection = merchantPosition - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        // 4. Play selling animation
        isSelling = true;
        Debug.Log("Blacksmith: Selling items to merchant...");
        
        // Trigger selling animation
        if (animator != null && !string.IsNullOrEmpty(sellTrigger))
        {
            // Reset any previous triggers to avoid conflicts
            animator.ResetTrigger(bellowsTrigger);
            animator.ResetTrigger(anvilTrigger);
            animator.ResetTrigger(storageTrigger);
            
            // Trigger the selling animation
            animator.SetTrigger(sellTrigger);
            Debug.Log($"Blacksmith: Triggered selling animation: {sellTrigger}");
            
            // Wait for animation to start
            yield return StartCoroutine(WaitForAnimation("Sell"));
        }
        
        // Notify merchant that we're selling
        if (merchant != null)
        {
            merchant.PlaySellEffects();
        }
        
        // Wait for selling animation to complete (7 seconds as per your animation)
        Debug.Log($"Blacksmith: Selling for {sellingTime} seconds");
        yield return new WaitForSeconds(sellingTime);
        
        // 5. Items sold
        Debug.Log($"Blacksmith: Sold {goodsProduced} items!");
        goodsProduced = 0;
        isSelling = false;
        
        // 6. Return to forge (reverse path)
        Debug.Log("Blacksmith: Returning to forge");
        for (int i = pathToMerchant.Length - 1; i >= 0; i--)
        {
            if (pathToMerchant[i] != null)
            {
                yield return StartCoroutine(MoveToPosition(pathToMerchant[i].position));
            }
        }
        
        // 7. Return to first station (bellows)
        if (bellowsStation != null)
        {
            yield return StartCoroutine(MoveToPosition(bellowsStation.position));
        }
        
        isGoingToMerchant = false;
        Debug.Log("Blacksmith: Back at forge, ready to work again");
    }
    
    // Generic movement method (can be used for stations or waypoints)
    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        // Start walking
        if (animator != null)
            animator.SetBool("isWalking", true);
        
        // Calculate direction
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
        
        // Move to position
        while (Vector3.Distance(transform.position, targetPosition) > 1f)
        {
            if (controller != null && controller.enabled)
            {
                Vector3 move = direction * moveSpeed * Time.deltaTime;
                controller.Move(move);
                
                // Recalculate direction for smoother movement
                direction = (targetPosition - transform.position).normalized;
                direction.y = 0;
                
                // Keep facing movement direction
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            yield return null;
        }
        
        // Stop walking
        if (animator != null)
            animator.SetBool("isWalking", false);
    }
    
    // Your existing GoToStation method (unchanged)
    IEnumerator GoToStation(Transform station, WorkStation stationType)
    {
        // Start walking
        if (animator != null)
            animator.SetBool("isWalking", true);
        
        // Calculate direction
        Vector3 direction = (station.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
        
        // Move to station
        while (Vector3.Distance(transform.position, station.position) > 1f)
        {
            if (controller != null && controller.enabled)
            {
                Vector3 move = direction * moveSpeed * Time.deltaTime;
                controller.Move(move);
            }
            yield return null;
        }
        
        // Stop walking
        if (animator != null)
            animator.SetBool("isWalking", false);
        
        // Face station exactly
        Vector3 lookDir = station.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
        
        // Play specific work animation based on station
        yield return StartCoroutine(PlayWorkAnimation(stationType));
    }
    
    // Your existing PlayWorkAnimation method (unchanged)
    IEnumerator PlayWorkAnimation(WorkStation stationType)
    {
        if (animator != null)
        {
            string triggerName = "";
            
            switch (stationType)
            {
                case WorkStation.Bellows:
                    triggerName = bellowsTrigger;
                    Debug.Log("Playing bellows animation");
                    break;
                    
                case WorkStation.Anvil:
                    triggerName = anvilTrigger;
                    Debug.Log("Playing anvil/hammer animation");
                    break;
                    
                case WorkStation.Storage:
                    triggerName = storageTrigger;
                    Debug.Log("Playing storage animation");
                    break;
            }
            
            // Trigger the animation
            if (!string.IsNullOrEmpty(triggerName))
            {
                animator.SetTrigger(triggerName);
                Debug.Log($"Triggered: {triggerName}");
            }
        }
        
        // Wait one frame to ensure animation starts
        yield return null;
        
        // Optional: Wait for animation to actually start
        if (animator != null)
        {
            float timeout = 0.5f;
            float timer = 0f;
            bool animationStarted = false;
            
            while (timer < timeout && !animationStarted)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                
                // Check if we're in any work state
                if (state.IsName("Bellows_Work") || 
                    state.IsName("Anvil_Work") || 
                    state.IsName("Storage_Work"))
                {
                    animationStarted = true;
                    Debug.Log("Work animation confirmed started");
                }
                
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }
    
    // NEW: Wait for specific animation state
    IEnumerator WaitForAnimation(string stateName)
    {
        if (animator == null) yield break;
        
        // Wait one frame for trigger to take effect
        yield return null;
        
        Debug.Log($"Waiting for animation state: {stateName}");
        
        float timeout = 2f; // Safety timeout
        float timer = 0f;
        bool animationStarted = false;
        
        while (timer < timeout && !animationStarted)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            
            if (state.IsName(stateName))
            {
                animationStarted = true;
                Debug.Log($"Animation '{stateName}' started successfully");
                break;
            }
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!animationStarted)
        {
            Debug.LogWarning($"Could not detect animation '{stateName}' after {timeout} seconds");
        }
    }
    
    enum WorkStation { Bellows, Anvil, Storage }
    
    void Update()
    {
        if (animator != null && Time.frameCount % 120 == 0) 
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            string stateName = "Unknown";
            
            if (state.IsName("Idle")) stateName = "Idle";
            else if (state.IsName("Walk")) stateName = "Walk";
            else if (state.IsName("Bellows_Work")) stateName = "Bellows_Work";
            else if (state.IsName("Anvil_Work")) stateName = "Anvil_Work";
            else if (state.IsName("Storage_Work")) stateName = "Storage_Work";
            else if (state.IsName("Sell")) stateName = "Sell"; // NEW: Selling state
            
            Debug.Log($"Blacksmith anim state: {stateName} (NormTime: {state.normalizedTime:F2}) | " +
                     $"Going to merchant: {isGoingToMerchant} | Selling: {isSelling}");
        }
    }
    
    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw merchant path in green
        if (pathToMerchant != null && pathToMerchant.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathToMerchant.Length; i++)
            {
                if (pathToMerchant[i] != null)
                {
                    Gizmos.DrawSphere(pathToMerchant[i].position, 0.3f);
                    if (i < pathToMerchant.Length - 1 && pathToMerchant[i + 1] != null)
                    {
                        Gizmos.DrawLine(pathToMerchant[i].position, pathToMerchant[i + 1].position);
                    }
                }
            }
            
            // Draw line from last waypoint to merchant
            if (merchant != null && pathToMerchant[pathToMerchant.Length - 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pathToMerchant[pathToMerchant.Length - 1].position, merchant.transform.position);
            }
        }
        
        // Draw stations
        Gizmos.color = Color.blue;
        if (anvilStation != null) Gizmos.DrawWireSphere(anvilStation.position, 0.5f);
        if (bellowsStation != null) Gizmos.DrawWireSphere(bellowsStation.position, 0.5f);
        if (storageStation != null) Gizmos.DrawWireSphere(storageStation.position, 0.5f);
    }
}