using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float chaseSpeed;
    [SerializeField] private float detectionRadius;
    [SerializeField] private float attackRadius;
    [SerializeField] private float attackDelay;
    [SerializeField] private float damageDealt;
    [SerializeField] private float rotationSpeed;

    [Header("Wandering parameters")]
    [SerializeField] private float wanderingWaitTimeMin;
    [SerializeField] private float wanderingWaitTimeMax;
    [SerializeField] private float wanderingDistanceMin;
    [SerializeField] private float wanderingDistanceMax;

    [Header("Boss Throne Settings")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private GameObject throne;

    private Transform player;
    private PlayerStats playerStats;
    private float currentHealth;
    private bool hasDestination;
    private bool isAttacking;
    private bool isDead;
    private bool isSeated = true;
    private bool initialDetectionDone = false;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerStats = player.GetComponent<PlayerStats>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        bool playerInRange = Vector3.Distance(player.position, transform.position) < detectionRadius && !playerStats.isDead;

        if (isBoss && isSeated)
        {
            if (playerInRange && !initialDetectionDone)
            {
                StandUpFromThrone();
                initialDetectionDone = true;
            }
            return;
        }

        if (playerInRange)
        {
            HandlePlayerDetection();
        }
        else
        {
            HandleWandering();
        }

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void HandlePlayerDetection()
    {
        agent.speed = chaseSpeed;
        RotateTowardsPlayer();

        if (!isAttacking)
        {
            if (Vector3.Distance(player.position, transform.position) < attackRadius)
            {
                StartCoroutine(AttackPlayer());
            }
            else
            {
                agent.SetDestination(player.position);
            }
        }
    }

    private void HandleWandering()
    {
        agent.speed = walkSpeed;

        if (agent.remainingDistance < 0.75f && !hasDestination)
        {
            StartCoroutine(GetNewDestination());
        }
    }

    private void RotateTowardsPlayer()
    {
        Quaternion rot = Quaternion.LookRotation(player.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
    }

    public void TakeDammage(float damages)
    {
        if (isDead) return;

        currentHealth -= damages;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("GetHit");
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        agent.enabled = false;
        enabled = false;
    }

    private IEnumerator GetNewDestination()
    {
        hasDestination = true;
        yield return new WaitForSeconds(Random.Range(wanderingWaitTimeMin, wanderingWaitTimeMax));

        Vector3 nextDestination = transform.position;
        nextDestination += Random.Range(wanderingDistanceMin, wanderingDistanceMax) *
                          new Vector3(Random.Range(-1f, 1), 0f, Random.Range(-1f, 1f)).normalized;

        if (NavMesh.SamplePosition(nextDestination, out NavMeshHit hit, wanderingDistanceMax, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        hasDestination = false;
    }

    private IEnumerator AttackPlayer()
    {
        isAttacking = true;
        agent.isStopped = true;
        audioSource.Play();
        animator.SetTrigger("Attack");

        playerStats.TakeDamage(damageDealt);

        yield return new WaitForSeconds(attackDelay);

        if (agent.enabled)
        {
            agent.isStopped = false;
        }
        isAttacking = false;
    }

    private void StandUpFromThrone()
    {
        isSeated = false;
        animator.SetTrigger("StandUp");
        StartCoroutine(EnableNavMeshAfterAnimation());

        if (throne != null)
        {
            throne.GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator EnableNavMeshAfterAnimation()
    {
        yield return new WaitForSeconds(2f);
        agent.enabled = true;
        animator.SetBool("IsStanding", true);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
    
    private void OnStandUpComplete()
    {
        // Logique supplémentaire après s'être levé
        agent.enabled = true;
        animator.SetBool("IsStanding", true);
    }

    private void DealDamage()
    {
        if (Vector3.Distance(player.position, transform.position) <= attackRadius)
        {
            playerStats.TakeDamage(damageDealt);
        }
    }

    private void OnAttackComplete()
    {
        isAttacking = false;
        if (agent.enabled)
        {
            agent.isStopped = false;
        }
    }
}