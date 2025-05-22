using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent), typeof(AudioSource))]
public class King_Of_Ice_AI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject throne;
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip themeMusic;

    [Header("Paramètres de Combat")]
    [SerializeField] private float maxHealth = 400f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float meleeDamage = 30f;
    [SerializeField] private float chaseSpeed = 3.5f;

    [Header("Animations")]
    [SerializeField] private float attackCooldown = 2f;

    // Composants
    private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    private AudioSource audioSource;
    private AudioSource musicSource;
    private Collider hitbox;

    // États
    private float currentHealth;
    private bool isSeated = true;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool hasRoaredFromLowHealth = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        hitbox = GetComponent<Collider>();

        // Audio source pour musique
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = themeMusic;
        musicSource.loop = true;
        musicSource.spatialBlend = 0;
        musicSource.playOnAwake = false;

        currentHealth = maxHealth;
        agent.enabled = false;
        hitbox.enabled = false;
    }

    private void Update()
    {
        if (isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Réveil du boss
        if (isSeated && distanceToPlayer <= detectionRange)
        {
            StandUpFromThrone();
            return;
        }

        // Si debout, on engage le combat
        if (!isSeated)
        {
            if (distanceToPlayer <= detectionRange)
            {
                if (!musicSource.isPlaying && themeMusic != null)
                {
                    musicSource.Play();
                }

                if (!isAttacking)
                {
                    if (distanceToPlayer <= attackRange)
                    {
                        StartCoroutine(MeleeAttack());
                    }
                    else
                    {
                        ChasePlayer();
                    }
                }
            }
            else
            {
                animator.SetFloat("Speed", 0f);
                agent.isStopped = true;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isSeated) return;

        currentHealth -= amount;

        if (hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        animator.SetTrigger("GetHit");

        if (!hasRoaredFromLowHealth && currentHealth <= maxHealth / 2f)
        {
            hasRoaredFromLowHealth = true;
            if (roarSound != null)
            {
                audioSource.PlayOneShot(roarSound);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator MeleeAttack()
    {
        isAttacking = true;
        agent.isStopped = true;

        int attackType = Random.Range(0, 2); // 0 ou 1
        animator.SetInteger("AttackType", attackType);
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f);

        if (Vector3.Distance(transform.position, player.position) <= attackRange * 1.2f)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(meleeDamage);
            }
        }

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        agent.isStopped = false;
    }

    private void ChasePlayer()
    {
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    public void StandUpFromThrone()
    {
        if (!isSeated) return;

        isSeated = false;
        animator.SetTrigger("StandUp");

        if (roarSound != null)
        {
            audioSource.PlayOneShot(roarSound);
        }

        StartCoroutine(EnableCombatComponents());
    }

    private IEnumerator EnableCombatComponents()
    {
        yield return new WaitForSeconds(2f); // durée de l'animation "StandUp"
        agent.enabled = true;
        agent.speed = chaseSpeed;
        hitbox.enabled = true;
        animator.SetBool("IsStanding", true);
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        agent.enabled = false;
        hitbox.enabled = false;
        musicSource.Stop();

        Destroy(gameObject, 5f);
    }

    public void OnAttackHitFrame()
    {
        // Utilisé via Animation Event
    }
}
