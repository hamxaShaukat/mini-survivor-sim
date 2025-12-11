using UnityEngine;
using System.Collections;

public class FarmerFieldWork : MonoBehaviour
{
    [Header("Field Working Settings")]
    public Transform[] fieldWaypoints; // Empty GameObjects for farming spots
    public float moveSpeed = 2f;
    public int workCycles = 20;
    
    [Header("Merchant Settings")]
    public Transform[] pathToMerchant; // Waypoints from field to merchant
    public MerchantInteraction merchant; // Reference to merchant script
    public int cyclesBeforeSelling = 3; // How many work cycles before going to merchant
    public float sellingTime = 7f; // Time spent selling at merchant (matches animation length)
    public float interactionDistance = 2f; // How close to get to merchant
    
    [Header("Animation Settings")]
    public string workTrigger = "StartWork"; // Trigger for farming animation
    public string sellTrigger = "StartSell"; // Selling animation trigger
    
    // Private variables
    private Animator animator;
    private CharacterController controller;
    public int completedCycles = 0;
    public int goodsProduced = 0; // Tracks how many crops harvested since last sell
    public bool isWorking = true;
    public bool isGoingToMerchant = false;
    public bool isSelling = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
            Debug.LogError("No Animator found on Farmer!");
        else
            Debug.Log("Farmer animator ready");
        
        StartCoroutine(FarmerLifecycle());
    }
    
    IEnumerator FarmerLifecycle()
    {
        Debug.Log("Farmer: Starting work lifecycle");
        
        while (isWorking)
        {
            // Phase 1: Work in field (harvest crops)
            yield return StartCoroutine(WorkPhase());
            
            // Phase 2: Go sell crops to merchant
            yield return StartCoroutine(SellToMerchantPhase());
            
            // Phase 3: Return to work
            Debug.Log("Farmer: Returning to work after selling");
        }
    }
    
    IEnumerator WorkPhase()
    {
        Debug.Log("Farmer: Starting work phase");
        
        int cyclesThisBatch = 0;
        
        while (cyclesThisBatch < cyclesBeforeSelling && completedCycles < workCycles)
        {
            // Work at each field waypoint in sequence
            foreach (Transform fieldPoint in fieldWaypoints)
            {
                if (fieldPoint != null)
                {
                    Debug.Log($"Farmer: Going to field point {fieldPoint.name}");
                    yield return StartCoroutine(GoToWorkPoint(fieldPoint));
                }
            }
            
            completedCycles++;
            cyclesThisBatch++;
            goodsProduced++;
            
            Debug.Log($"Farmer: Completed cycle {completedCycles}/{workCycles}. Crops harvested: {goodsProduced}");
            
            // Brief pause between cycles
            yield return new WaitForSeconds(0.5f);
        }
        
        if (goodsProduced > 0)
        {
            Debug.Log($"Farmer: Finished batch of {cyclesThisBatch} cycles. Ready to sell {goodsProduced} crops");
        }
    }
    
    IEnumerator SellToMerchantPhase()
    {
        if (goodsProduced == 0 || merchant == null || pathToMerchant == null || pathToMerchant.Length == 0)
        {
            Debug.Log("Farmer: No crops to sell or merchant not set up");
            yield break;
        }
        
        isGoingToMerchant = true;
        Debug.Log($"Farmer: Going to merchant with {goodsProduced} crops");
        
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
        Debug.Log("Farmer: Selling crops to merchant...");
        
        // Trigger selling animation
        if (animator != null && !string.IsNullOrEmpty(sellTrigger))
        {
            // Reset work trigger to avoid conflicts
            animator.ResetTrigger(workTrigger);
            
            // Trigger the selling animation
            animator.SetTrigger(sellTrigger);
            Debug.Log($"Farmer: Triggered selling animation: {sellTrigger}");
            
            // Wait for animation to complete (7 seconds)
            yield return StartCoroutine(WaitForAnimationCompletion("Sell", sellingTime));
        }
        else
        {
            // If no selling animation, just wait
            yield return new WaitForSeconds(sellingTime);
        }
        
        // Notify merchant that we're selling
        if (merchant != null)
        {
            merchant.PlaySellEffects();
        }
        
        // 5. Crops sold
        Debug.Log($"Farmer: Sold {goodsProduced} crops!");
        goodsProduced = 0;
        isSelling = false;
        
        // 6. Return to field (reverse path)
        Debug.Log("Farmer: Returning to field");
        for (int i = pathToMerchant.Length - 1; i >= 0; i--)
        {
            if (pathToMerchant[i] != null)
            {
                yield return StartCoroutine(MoveToPosition(pathToMerchant[i].position));
            }
        }
        
        // 7. Return to first field waypoint
        if (fieldWaypoints != null && fieldWaypoints.Length > 0 && fieldWaypoints[0] != null)
        {
            yield return StartCoroutine(MoveToPosition(fieldWaypoints[0].position));
        }
        
        isGoingToMerchant = false;
        Debug.Log("Farmer: Back at field, ready to work again");
    }
    
    // Move to a work point and perform farming
    IEnumerator GoToWorkPoint(Transform workPoint)
    {
        // Move to the work point
        yield return StartCoroutine(MoveToPosition(workPoint.position));
        
        // Face work point exactly
        Vector3 lookDir = workPoint.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
        
        // Play farming animation and wait for it to complete
        yield return StartCoroutine(PlayFarmingAnimation());
    }
    
    // Play farming animation and wait for it to complete
    IEnumerator PlayFarmingAnimation()
    {
        if (animator == null) yield break;
        
        // Trigger the farming animation
        if (!string.IsNullOrEmpty(workTrigger))
        {
            animator.SetTrigger(workTrigger);
            Debug.Log($"Farmer: Triggered farming animation: {workTrigger}");
        }
        
        // Wait for the animation to complete (your gathering animation is ~4-5 seconds)
        // This method waits for the animation to finish naturally
        yield return StartCoroutine(WaitForAnimationCompletion("Gathering", 5f));
    }
    
    // Wait for animation to complete naturally
    IEnumerator WaitForAnimationCompletion(string stateName, float maxWaitTime = 5f)
    {
        if (animator == null) yield break;
        
        // Wait one frame for trigger to take effect
        yield return null;
        
        Debug.Log($"Waiting for {stateName} animation to complete");
        
        float timer = 0f;
        bool isInAnimation = false;
        
        // Wait for animation to start
        while (timer < 1f && !isInAnimation) // 1 second timeout to start
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(stateName))
            {
                isInAnimation = true;
                Debug.Log($"{stateName} animation started");
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!isInAnimation)
        {
            Debug.LogWarning($"{stateName} animation didn't start within 1 second");
            yield return new WaitForSeconds(maxWaitTime);
            yield break;
        }
        
        // Now wait for animation to complete (exit time)
        // Your animations have exit time = 0.95, so wait for that
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = currentState.length;
        float exitTime = 0.95f * animationLength; // 95% of animation length
        
        // Wait for exit time or maxWaitTime, whichever is smaller
        float waitTime = Mathf.Min(exitTime, maxWaitTime);
        Debug.Log($"Waiting {waitTime:F2} seconds for {stateName} animation (length: {animationLength:F2}s)");
        
        yield return new WaitForSeconds(waitTime);
        
        Debug.Log($"{stateName} animation completed");
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
    
    void Update()
    {
        if (animator != null && Time.frameCount % 120 == 0) 
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            string stateName = "Unknown";
            
            if (state.IsName("Idle")) stateName = "Idle";
            else if (state.IsName("Walk")) stateName = "Walk";
            else if (state.IsName("Gathering")) stateName = "Gathering";
            else if (state.IsName("Sell")) stateName = "Sell";
            
            Debug.Log($"Farmer anim state: {stateName} (NormTime: {state.normalizedTime:F2}) | " +
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
        
        // Draw field waypoints in blue
        if (fieldWaypoints != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform wp in fieldWaypoints)
            {
                if (wp != null)
                {
                    Gizmos.DrawSphere(wp.position, 0.4f);
                    Gizmos.DrawWireSphere(wp.position, 0.5f);
                }
            }
        }
    }
}