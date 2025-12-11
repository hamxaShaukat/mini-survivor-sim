using UnityEngine;
using UnityEngine.AI;

public class RandomRoam : MonoBehaviour
{
    public float roamRadius = 20f;
    public float idleDurationMin = 2f;
    public float idleDurationMax = 5f;

    private NavMeshAgent agent;
    private Animator anim;

    private float idleTimer;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false; // IMPORTANT FIX

        SetIdleState();
    }

    private void Update()
    {
        // Smooth rotate based on movement
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }

        // If walking and reached destination -> go idle
        if (agent.remainingDistance <= agent.stoppingDistance && agent.velocity.magnitude < 0.1f)
        {
            if (anim.GetBool("StartWalk") == true)
            {
                SetIdleState();
            }
        }

        // If idle, count time
        if (!anim.GetBool("StartWalk"))
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
            {
                StartWalkingToRandomPoint();
            }
        }
    }

    void SetIdleState()
    {
        anim.SetBool("StartWalk", false);
        idleTimer = Random.Range(idleDurationMin, idleDurationMax);
        agent.ResetPath();
    }

    void StartWalkingToRandomPoint()
    {
        anim.SetBool("StartWalk", true);

        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
