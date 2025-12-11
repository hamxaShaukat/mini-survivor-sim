using UnityEngine;
using System.Collections;

public class SmartFieldWorker : MonoBehaviour
{
    [Header("NPC Identity")]
    public string workerName = "Worker";
    public bool worksInCornField = true; // True: Corn, False: Wheat
    
    [Header("Work Settings")]
    public Transform[] fieldWorkPoints; // 3 points for this NPC
    public int cyclesBeforeBreak = 3; // Work cycles before considering break
    public float workTimePerPoint = 5f;
    
    [Header("Path Settings")]
    public Transform[] pathHomeToField; // A → B → C (Home to field)
    public Transform[] pathFieldToMerchantBranch; // C → E (Field to merchant branch)
    public Transform[] pathMerchantBranchToStalls; // E → Stall1, Stall2, Stall3
    public Transform[] pathToSocialArea; // C → D (Field to social/X area)
    
    [Header("Merchant Settings")]
    public MerchantInteraction[] merchants; // Array of 3 merchants/stalls
    public float sellingTime = 7f;
    public float interactionDistance = 2f;
    
    [Header("Animation Settings")]
    public string workTrigger = "StartWork";
    public string sellTrigger = "StartSell";
    
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float stoppingDistance = 1f;
    
    // Components
    private Animator animator;
    private CharacterController controller;
    
    // State
    private bool isWorking = true;
    private bool isAtMarket = false;
    private int workCyclesCompleted = 0;
    private int goodsProduced = 0;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
            Debug.LogError($"No Animator found on {workerName}!");
        if (controller == null)
            Debug.LogError($"No CharacterController found on {workerName}!");
        
        // Start at position A (first home waypoint)
        if (pathHomeToField.Length > 0 && pathHomeToField[0] != null)
            transform.position = pathHomeToField[0].position;
        
        StartCoroutine(NPCRoutine());
    }
    
    IEnumerator NPCRoutine()
    {
        Debug.Log($"{workerName}: Starting daily routine");
        
        while (isWorking)
        {
            // 1. Morning: Home → Field
            Debug.Log($"{workerName}: Going to {GetFieldName()} field");
            yield return StartCoroutine(FollowPath(pathHomeToField));
            
            // 2. Work Phase
            yield return StartCoroutine(WorkPhase());
            
            // 3. Decision: Go to market or social area?
            bool goToMarket = Random.Range(0, 2) == 0; // 50% chance
            
            if (goToMarket && goodsProduced > 0)
            {
                Debug.Log($"{workerName}: Going to market with {goodsProduced} goods");
                yield return StartCoroutine(GoToMarket());
                goodsProduced = 0;
            }
            else
            {
                Debug.Log($"{workerName}: Going to social area");
                yield return StartCoroutine(GoToSocialArea());
            }
            
            // 4. Return home (reverse path)
            Debug.Log($"{workerName}: Going home");
            yield return StartCoroutine(FollowPathReverse(pathHomeToField));
            
            // 5. Sleep/rest at home
            Debug.Log($"{workerName}: Resting at home");
            yield return new WaitForSeconds(10f);
        }
    }
    
    IEnumerator WorkPhase()
    {
        workCyclesCompleted = 0;
        
        while (workCyclesCompleted < cyclesBeforeBreak)
        {
            Debug.Log($"{workerName}: Work cycle {workCyclesCompleted + 1}/{cyclesBeforeBreak}");
            
            // Work at each point
            foreach (Transform workPoint in fieldWorkPoints)
            {
                if (workPoint == null) continue;
                
                // Move to work point
                yield return StartCoroutine(MoveToPosition(workPoint.position));
                
                // Face work point
                Vector3 lookDir = workPoint.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);
                
                // Work animation
                yield return StartCoroutine(DoWork());
                
                goodsProduced++;
                
                // Random chance to take a quick break during work
                if (Random.Range(0f, 1f) < 0.2f) // 20% chance
                {
                    yield return StartCoroutine(TakeQuickBreak());
                }
            }
            
            workCyclesCompleted++;
            
            // Small pause between cycles
            yield return new WaitForSeconds(1f);
        }
    }
    
    IEnumerator GoToMarket()
    {
        // Field (C) → Merchant branch (E)
        yield return StartCoroutine(FollowPath(pathFieldToMerchantBranch));
        
        // Choose a random merchant
        int merchantIndex = Random.Range(0, merchants.Length);
        MerchantInteraction chosenMerchant = merchants[merchantIndex];
        
        if (chosenMerchant != null)
        {
            // Go to the specific merchant from branch
            // Find which stall path to take (assuming 3 stalls)
            if (pathMerchantBranchToStalls.Length >= 3)
            {
                // Go to the chosen stall
                Transform stallPoint = pathMerchantBranchToStalls[merchantIndex];
                if (stallPoint != null)
                {
                    yield return StartCoroutine(MoveToPosition(stallPoint.position));
                }
            }
            
            // Face merchant
            Vector3 lookDir = chosenMerchant.transform.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookDir);
            
            // Sell
            Debug.Log($"{workerName}: Selling at merchant {merchantIndex + 1}");
            isAtMarket = true;
            
            if (animator != null && !string.IsNullOrEmpty(sellTrigger))
            {
                animator.SetTrigger(sellTrigger);
                yield return new WaitForSeconds(sellingTime);
            }
            
            if (chosenMerchant != null)
            {
                chosenMerchant.PlaySellEffects();
            }
            
            isAtMarket = false;
        }
    }
    
    IEnumerator GoToSocialArea()
    {
        // Field (C) → Social area (D)
        yield return StartCoroutine(FollowPath(pathToSocialArea));
        
        // Look around randomly at social area
        Debug.Log($"{workerName}: At social area, looking around");
        
        for (int i = 0; i < Random.Range(2, 4); i++)
        {
            // Random rotation
            float randomYaw = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0, randomYaw, 0);
            yield return new WaitForSeconds(Random.Range(2f, 4f));
        }
        
        // Maybe walk around a bit
        if (Random.Range(0f, 1f) < 0.5f)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-3f, 3f),
                0,
                Random.Range(-3f, 3f)
            );
            Vector3 wanderPos = transform.position + randomOffset;
            yield return StartCoroutine(MoveToPosition(wanderPos));
        }
    }
    
    IEnumerator DoWork()
    {
        if (animator == null) yield break;
        
        Debug.Log($"{workerName}: Working for {workTimePerPoint} seconds");
        
        animator.SetTrigger(workTrigger);
        yield return new WaitForSeconds(workTimePerPoint);
    }
    
    IEnumerator TakeQuickBreak()
    {
        Debug.Log($"{workerName}: Taking a quick break");
        
        // Look around
        for (int i = 0; i < 2; i++)
        {
            float randomYaw = Random.Range(0f, 360f);
            transform.rotation = Quaternion.Euler(0, randomYaw, 0);
            yield return new WaitForSeconds(Random.Range(1f, 2f));
        }
        
        Debug.Log($"{workerName}: Break over, back to work");
    }
    
    // ========== MOVEMENT METHODS ==========
    
    IEnumerator FollowPath(Transform[] path)
    {
        foreach (Transform point in path)
        {
            if (point != null)
            {
                yield return StartCoroutine(MoveToPosition(point.position));
            }
        }
    }
    
    IEnumerator FollowPathReverse(Transform[] path)
    {
        for (int i = path.Length - 1; i >= 0; i--)
        {
            if (path[i] != null)
            {
                yield return StartCoroutine(MoveToPosition(path[i].position));
            }
        }
    }
    
    IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        if (animator == null || controller == null) yield break;
        
        // Start walking
        animator.SetBool("isWalking", true);
        
        // Calculate direction (IGNORE Y-AXIS)
        Vector3 targetFlat = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        Vector3 direction = (targetFlat - transform.position).normalized;
        
        // Face target
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);
        
        // Move to position (check X-Z distance only)
        while (GetXZDistance(transform.position, targetPosition) > stoppingDistance)
        {
            if (controller.enabled)
            {
                // Recalculate direction
                targetFlat = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
                direction = (targetFlat - transform.position).normalized;
                
                // Move in X-Z plane only
                Vector3 move = new Vector3(direction.x, 0, direction.z) * walkSpeed * Time.deltaTime;
                
                // Apply gravity if needed
                if (!controller.isGrounded)
                {
                    move.y = -9.81f * Time.deltaTime;
                }
                
                controller.Move(move);
                
                // Keep facing direction
                if (direction != Vector3.zero)
                {
                    Vector3 lookDir = new Vector3(direction.x, 0, direction.z);
                    if (lookDir != Vector3.zero)
                        transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            yield return null;
        }
        
        // Stop walking
        animator.SetBool("isWalking", false);
        
        // Small pause
        yield return new WaitForSeconds(0.1f);
    }
    
    float GetXZDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
    
    string GetFieldName()
    {
        return worksInCornField ? "Corn" : "Wheat";
    }
    
    // ========== DEBUG & VISUALIZATION ==========
    
    void OnDrawGizmosSelected()
    {
        // Draw field work points in green
        Gizmos.color = Color.green;
        foreach (Transform point in fieldWorkPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, 0.4f);
                Gizmos.DrawWireSphere(point.position, 0.6f);
            }
        }
        
        // Draw paths with different colors
        DrawPathGizmos(pathHomeToField, Color.blue, "Home→Field");
        DrawPathGizmos(pathFieldToMerchantBranch, Color.yellow, "Field→Merchant Branch");
        DrawPathGizmos(pathToSocialArea, Color.magenta, "Field→Social");
        
        // Draw merchant branch points
        if (pathMerchantBranchToStalls != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform point in pathMerchantBranchToStalls)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.3f);
                    Gizmos.DrawWireSphere(point.position, 0.4f);
                }
            }
        }
    }
    
    void DrawPathGizmos(Transform[] path, Color color, string label)
    {
        if (path == null || path.Length == 0) return;
        
        Gizmos.color = color;
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (path[i] != null && path[i + 1] != null)
            {
                Gizmos.DrawLine(path[i].position, path[i + 1].position);
                Gizmos.DrawSphere(path[i].position, 0.2f);
            }
        }
        
        // Label first point
        if (path[0] != null)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(path[0].position + Vector3.up, label);
            #endif
        }
    }
    
    void Update()
    {
        // Debug info
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"{workerName}: AtMarket={isAtMarket}, Goods={goodsProduced}, Field={GetFieldName()}");
        }
    }
}