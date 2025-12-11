using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnifiedNPCController : MonoBehaviour
{
    [Header("NPC Identity")]
    public string npcName = "NPC";
    public enum NPCType { Farmer, Blacksmith, Merchant, Guard, Enemy }
    public NPCType npcType = NPCType.Farmer;

    [Header("Work Stations (Blacksmith)")]
    public Transform anvilStation;
    public Transform bellowsStation;
    public Transform storageStation;

    [Header("Field Work (Farmer)")]
    public Transform[] fieldWaypoints;

    [Header("Merchant Settings")]
    public Transform[] pathToMerchant;
    public MerchantInteraction merchant;

    [Header("Timing Settings")]
    public float moveSpeed = 2f;
    public float workCycleTime = 2f;
    public int cyclesBeforeSelling = 5;
    public float sellingTime = 7f;

    [Header("Mining Settings")]
    public Transform[] minePath;
    public Transform minePoint;
    public float miningTime = 10f;
    public int miningEarnings = 15;
    public string mineAnimationTrigger = "StartAnvil";

    [Header("Animation Settings")]
    public string workTrigger = "StartWork";
    public string sellTrigger = "StartSell";
    public string bellowsTrigger = "StartBellows";
    public string anvilTrigger = "StartAnvil";
    public string storageTrigger = "StartStorage";
    public string mineTrigger = "StartAnvil";

    public bool isBusy { get { return isTaskActive; } }
    public string currentTask { get; private set; } = "None";
    public bool isTaskActive { get; private set; } = false;

    [Header("Task System")]
    public float interactionRadius = 5f;
    public Transform[] patrolPoints;
    public float followDistance = 3f;

    // Components
    private Animator animator;
    private CharacterController controller;

    // State
    private bool isWorking = true;
    private bool isGoingToMerchant = false;
    private bool isSelling = false;


    // Stats
    public int goodsProduced { get; private set; } = 0;
    public int dailyEarnings { get; private set; } = 0;
    public int toolsCrafted { get; private set; } = 0;
    public int weaponsForged { get; private set; } = 0;
    public int cropsHarvested { get; private set; } = 0;

    // Task queue
    private Queue<string> taskQueue = new Queue<string>();
    private Coroutine currentTaskCoroutine;
    private GameObject player;


    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        player = GameObject.FindGameObjectWithTag("Player");

        // Start normal work
        StartCoroutine(NPCLifecycle());
    }

    // ================== NORMAL WORK LIFE CYCLE ==================
    IEnumerator NPCLifecycle()
    {
        while (isWorking && !isTaskActive)
        {
            switch (npcType)
            {
                case NPCType.Blacksmith:
                    yield return StartCoroutine(BlacksmithWorkPhase());
                    break;
                case NPCType.Farmer:
                    yield return StartCoroutine(FarmerWorkPhase());
                    break;
                case NPCType.Enemy:
                    yield return StartCoroutine(TaskFollowMayorAndAttack());
                    break;
            }

            if (goodsProduced > 0 && !isTaskActive)
            {
                yield return StartCoroutine(SellToMerchant());
            }
        }
    }

    IEnumerator BlacksmithWorkPhase()
    {
        int cyclesDone = 0;
        while (cyclesDone < cyclesBeforeSelling && !isTaskActive)
        {
            // Check if we have ore to use
            if (goodsProduced > 0)
            {
                Debug.Log($"[{npcName}] Using mined ore for crafting...");
            }

            // Bellows
            yield return StartCoroutine(GoToPosition(bellowsStation.position, false));
            yield return PlayAnimation(bellowsTrigger, workCycleTime);

            // Anvil
            yield return StartCoroutine(GoToPosition(anvilStation.position, false));
            yield return PlayAnimation(anvilTrigger, workCycleTime);

            // Storage
            yield return StartCoroutine(GoToPosition(storageStation.position, false));
            yield return PlayAnimation(storageTrigger, workCycleTime);

            cyclesDone++;
            goodsProduced++; // Produces one item
            toolsCrafted++;
            dailyEarnings += 10;

            Debug.Log($"[{npcName}] Cycle {cyclesDone}: Made tool. Goods: {goodsProduced}");
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator FarmerWorkPhase()
    {
        int cyclesDone = 0;
        while (cyclesDone < cyclesBeforeSelling && !isTaskActive)
        {
            foreach (Transform field in fieldWaypoints)
            {
                if (field != null && !isTaskActive)
                {
                    yield return StartCoroutine(GoToPosition(field.position));
                    yield return PlayAnimation(workTrigger, workCycleTime);
                }
            }

            cyclesDone++;
            goodsProduced++;
            cropsHarvested++;
            dailyEarnings += 5;

            yield return new WaitForSeconds(0.5f);
        }
    }




    IEnumerator SellToMerchant()
    {
        if (merchant == null || pathToMerchant.Length == 0) yield break;

        isGoingToMerchant = true;

        // Go to merchant
        foreach (Transform waypoint in pathToMerchant)
        {
            if (waypoint != null && !isTaskActive)
            {
                yield return StartCoroutine(GoToPosition(waypoint.position, false));
            }
        }

        // Sell animation
        yield return StartCoroutine(GoToPosition(merchant.transform.position, false));
        yield return PlayAnimation(sellTrigger, sellingTime);

        if (merchant != null)
        {
            // Notify merchant and get payment - FIXED THIS LINE
            bool success = merchant.ReceiveGoodsFromNPC(npcType.ToString(), goodsProduced);

            if (success)
            {
                // Add payment to daily earnings
                int payment = 0;
                if (npcType == NPCType.Blacksmith)
                    payment = goodsProduced * 15; // Tool price
                else if (npcType == NPCType.Farmer)
                    payment = goodsProduced * 8; // Crop price

                dailyEarnings += payment;
                Debug.Log($"[{npcName}] Sold {goodsProduced} goods for {payment} gold");

                // Play effects
                merchant.PlaySellEffects();
            }
            else
            {
                Debug.LogWarning($"[{npcName}] Merchant couldn't buy goods!");
            }
        }

        // Return
        for (int i = pathToMerchant.Length - 1; i >= 0; i--)
        {
            if (pathToMerchant[i] != null && !isTaskActive)
            {
                yield return StartCoroutine(GoToPosition(pathToMerchant[i].position, false));
            }
        }

        goodsProduced = 0;
        isGoingToMerchant = false;
    }
    // ================== TASK SYSTEM ==================
    public void AssignTask(string taskName)
    {
        Debug.Log($"[{npcName}] Task received: {taskName}");

        if (isTaskActive)
        {
            taskQueue.Enqueue(taskName);
            return;
        }

        // Stop normal work
        StopAllCoroutines();
        isWorking = false;

        // Start task
        currentTask = taskName;
        isTaskActive = true;

        switch (taskName)
        {
            case "Patrol Village":
                currentTaskCoroutine = StartCoroutine(TaskPatrol());
                break;
            case "Follow Mayor":
                currentTaskCoroutine = StartCoroutine(TaskFollowMayor());
                break;
            case "Craft Tools":
                currentTaskCoroutine = StartCoroutine(TaskCraftTools());
                break;
            case "Forge Weapons":
                currentTaskCoroutine = StartCoroutine(TaskForgeWeapons());
                break;
            case "Mine Resources":
                currentTaskCoroutine = StartCoroutine(TaskMineResources());
                break;
            case "Harvest Crops":
                currentTaskCoroutine = StartCoroutine(TaskHarvestCrops());
                break;
            case "Report Production":
                TaskReport();
                break;
            default:
                ResumeNormalWork();
                break;
        }
    }

    // Task implementations
    IEnumerator TaskPatrol()
    {
        if (patrolPoints.Length == 0)
        {
            CompleteTask();
            yield break;
        }

        Debug.Log($"[{npcName}] Starting patrol");

        float patrolDuration = 30f; // Patrol for 30 seconds
        float timer = 0f;
        int currentPoint = 0;

        while (isTaskActive && timer < patrolDuration)
        {
            if (currentPoint >= patrolPoints.Length || patrolPoints[currentPoint] == null)
            {
                CompleteTask();
                yield break;
            }

            // Move to current patrol point
            yield return StartCoroutine(GoToPosition(patrolPoints[currentPoint].position, true));

            // Wait at point (look around)
            if (animator != null)
                animator.SetBool("isWalking", false);
            yield return new WaitForSeconds(3f);

            // Move to next point
            currentPoint = (currentPoint + 1) % patrolPoints.Length;

            timer += Time.deltaTime * 4; // Multiply because we spend time moving/waiting
            yield return null;
        }

        Debug.Log($"[{npcName}] Finished patrol");
        CompleteTask();
    }

    IEnumerator TaskFollowMayor()
    {
        if (player == null)
        {
            CompleteTask();
            yield break;
        }

        Debug.Log($"[{npcName}] Starting to follow player");

        float followDuration = 30f; // Follow for 30 seconds
        float timer = 0f;

        while (isTaskActive && timer < followDuration)
        {
            if (player == null) break;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance > followDistance)
            {
                // Calculate position behind the player
                Vector3 targetPos = player.transform.position - (player.transform.forward * followDistance);

                // Move to position
                yield return StartCoroutine(GoToPosition(targetPos, true)); // true = allow movement during task
            }
            else
            {
                // Already close enough, just wait
                if (animator != null)
                    animator.SetBool("isWalking", false);
                yield return new WaitForSeconds(0.5f);
            }

            timer += Time.deltaTime;
            yield return null; // Wait one frame
        }

        Debug.Log($"[{npcName}] Finished following player");
        CompleteTask();
    }

    IEnumerator TaskFollowMayorAndAttack()
    {
        if (player == null)
        {
            CompleteTask();
            yield break;
        }

        Debug.Log($"[{npcName}] Starting to follow player");

        float followDuration = 30f; // Follow for 30 seconds
        float timer = 0f;


        // if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > followDistance)
        {
            // Calculate position behind the player
            Vector3 targetPos = player.transform.position - (player.transform.forward * 0);

            // Move to position
            yield return StartCoroutine(GoToPosition(targetPos, true)); // true = allow movement during task
        }
        else
        {
            if (animator != null)
                animator.SetBool("StartPunch", true);
            yield return new WaitForSeconds(0.5f);
        }

        timer += Time.deltaTime;
        yield return null; 


        Debug.Log($"[{npcName}] Finished following player");
        CompleteTask();
    }

    IEnumerator TaskCraftTools()
    {
        int toolsToMake = 5;
        for (int i = 0; i < toolsToMake && isTaskActive; i++)
        {
            yield return StartCoroutine(GoToPosition(bellowsStation.position));
            yield return PlayAnimation(bellowsTrigger, workCycleTime);

            yield return StartCoroutine(GoToPosition(anvilStation.position));
            yield return PlayAnimation(anvilTrigger, workCycleTime);

            yield return StartCoroutine(GoToPosition(storageStation.position));
            yield return PlayAnimation(storageTrigger, workCycleTime);

            toolsCrafted++;
            dailyEarnings += 10;
            yield return new WaitForSeconds(0.5f);
        }

        CompleteTask();
    }

    IEnumerator TaskForgeWeapons()
    {
        int weaponsToForge = 3; // Forge 3 weapons
        for (int i = 0; i < weaponsToForge && isTaskActive; i++)
        {
            // Bellows (longer for weapons)
            yield return StartCoroutine(GoToPosition(bellowsStation.position));
            yield return PlayAnimation(bellowsTrigger, workCycleTime * 1.5f);

            // Anvil (longer hammering)
            yield return StartCoroutine(GoToPosition(anvilStation.position));
            yield return PlayAnimation(anvilTrigger, workCycleTime * 2f);

            // Storage
            yield return StartCoroutine(GoToPosition(storageStation.position));
            yield return PlayAnimation(storageTrigger, workCycleTime);

            weaponsForged++;
            dailyEarnings += 25; // Each weapon worth 25 gold
            Debug.Log($"[{npcName}] Forged weapon #{weaponsForged}");

            yield return new WaitForSeconds(1f);
        }

        CompleteTask();
    }

    IEnumerator TaskMineResources()
    {
        Debug.Log($"[{npcName}] Starting mining task");

        if (minePath == null || minePath.Length == 0 || minePoint == null)
        {
            Debug.LogWarning($"[{npcName}] Mining path or point not set up!");
            CompleteTask();
            yield break;
        }

        // 1. Follow path to mine
        Debug.Log($"[{npcName}] Following path to mine...");
        foreach (Transform waypoint in minePath)
        {
            if (waypoint != null && isTaskActive)
            {
                yield return StartCoroutine(GoToPosition(waypoint.position, true));
            }
        }

        // 2. Go to exact mining spot
        yield return StartCoroutine(GoToPosition(minePoint.position, true));

        // 3. Play mining animation
        Debug.Log($"[{npcName}] Mining at {minePoint.name}...");
        yield return PlayAnimation(mineAnimationTrigger, miningTime);


        goodsProduced += 3; // Get 3 ore pieces from mining
        dailyEarnings += miningEarnings;
        Debug.Log($"[{npcName}] Mined 3 ore pieces! Total goods: {goodsProduced}");

        // 5. Return to forge (reverse path)
        Debug.Log($"[{npcName}] Returning to forge...");
        for (int i = minePath.Length - 1; i >= 0; i--)
        {
            if (minePath[i] != null && isTaskActive)
            {
                yield return StartCoroutine(GoToPosition(minePath[i].position, true));
            }
        }

        if (bellowsStation != null)
        {
            yield return StartCoroutine(GoToPosition(bellowsStation.position, true));
        }

        Debug.Log($"[{npcName}] Mining task complete!");
        CompleteTask();
    }

    IEnumerator TaskMineAndSell()
    {

        yield return StartCoroutine(TaskMineResources());

        // Then sell if we have goods
        if (goodsProduced > 0 && merchant != null)
        {
            yield return StartCoroutine(SellToMerchant());
        }

        CompleteTask();
    }
    IEnumerator TaskHarvestCrops()
    {
        int cropsToHarvest = 5;
        for (int i = 0; i < cropsToHarvest && isTaskActive; i++)
        {
            foreach (Transform field in fieldWaypoints)
            {
                if (field != null && isTaskActive)
                {
                    yield return StartCoroutine(GoToPosition(field.position));
                    yield return PlayAnimation(workTrigger, workCycleTime);
                }
            }
            cropsHarvested++;
            dailyEarnings += 5;
        }

        CompleteTask();
    }

    void TaskReport()
    {
        // Just report stats
        CompleteTask();
    }

    void CompleteTask()
    {
        Debug.Log($"[{npcName}] Task completed: {currentTask}");

        if (taskQueue.Count > 0)
        {
            string nextTask = taskQueue.Dequeue();
            AssignTask(nextTask);
        }
        else
        {
            ResumeNormalWork();
        }
    }

    void ResumeNormalWork()
    {
        currentTask = "None";
        isTaskActive = false;
        isWorking = true;
        StartCoroutine(NPCLifecycle());
    }

    // ================== HELPER METHODS ==================
    IEnumerator GoToPosition(Vector3 target, bool allowDuringTask = false)
    {
        if (animator != null)
            animator.SetBool("isWalking", true);

        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        // FIX: Remove the "!isTaskActive" condition or handle it differently
        float arrivalDistance = 1f;

        while (Vector3.Distance(transform.position, target) > arrivalDistance)
        {
            // Allow movement if: not in a task OR allowDuringTask is true
            if (controller != null && (!isTaskActive || allowDuringTask))
            {
                // Recalculate direction
                direction = (target - transform.position).normalized;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);

                    // Move
                    Vector3 move = direction * moveSpeed * Time.deltaTime;
                    controller.Move(move);
                }
            }
            else
            {
                // If task became inactive, break out
                break;
            }

            yield return null;
        }

        if (animator != null)
            animator.SetBool("isWalking", false);
    }

    IEnumerator PlayAnimation(string trigger, float duration)
    {
        if (animator != null && !string.IsNullOrEmpty(trigger))
        {
            animator.SetTrigger(trigger);
        }
        yield return new WaitForSeconds(duration);
    }

    // ================== DIALOGUE INTEGRATION ==================
    public string GetTaskReport()
    {
        string report = $"Current Task: {currentTask}\n";

        switch (npcType)
        {
            case NPCType.Blacksmith:
                report += $"Tools Crafted: {toolsCrafted}\n";
                report += $"Weapons Forged: {weaponsForged}\n";
                break;
            case NPCType.Farmer:
                report += $"Crops Harvested: {cropsHarvested}\n";
                break;
        }

        report += $"Daily Earnings: {dailyEarnings} gold\n";
        report += $"Goods Ready: {goodsProduced}";

        return report;
    }

    public void SubmitToTreasury()
    {
        Treasury treasury = FindObjectOfType<Treasury>();
        if (treasury != null && dailyEarnings > 0)
        {
            treasury.AddMoney(dailyEarnings);
            dailyEarnings = 0;
        }
    }
    public List<string> GetAvailableTasks()
    {
        List<string> tasks = new List<string>();

        switch (npcType)
        {
            case NPCType.Farmer:
                tasks.Add("Harvest Crops");
                tasks.Add("Plant New Field");
                tasks.Add("Clear Jungle Area");
                tasks.Add("Patrol Village");
                tasks.Add("Follow Mayor");
                tasks.Add("Report Production");
                tasks.Add("Submit to Treasury");
                break;

            case NPCType.Blacksmith:
                tasks.Add("Craft Tools");
                tasks.Add("Forge Weapons");
                tasks.Add("Mine Resources");
                tasks.Add("Patrol Village");
                tasks.Add("Follow Mayor");
                tasks.Add("Report Production");
                tasks.Add("Submit to Treasury");
                break;

            case NPCType.Merchant:
                tasks.Add("Sell Goods");
                tasks.Add("Buy Supplies");
                tasks.Add("Travel to Market");
                tasks.Add("Follow Mayor");
                tasks.Add("Submit to Treasury");
                break;

            case NPCType.Guard:
                tasks.Add("Patrol Village");
                tasks.Add("Guard Treasury");
                tasks.Add("Escort Merchant");
                tasks.Add("Train Militia");
                tasks.Add("Follow Mayor");
                break;
        }

        return tasks;
    }

    void Update()
    {
        // Interaction detection
        if (player != null && Vector3.Distance(transform.position, player.transform.position) <= interactionRadius)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                MayorDialogueUI dialogueUI = FindObjectOfType<MayorDialogueUI>();
                if (dialogueUI != null)
                {
                    dialogueUI.OpenDialogue(this);
                }
            }
        }
    }
}