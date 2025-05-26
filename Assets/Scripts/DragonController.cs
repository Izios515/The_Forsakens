using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DragonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource; // utilisé pour tout
    [SerializeField] private Transform player;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip roarClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip dieClip;
    [SerializeField] private AudioClip themeMusic;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float attackRadius = 4f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float damageDealt = 20f;

    private float currentHealth;
    private bool isDead = false;
    private bool isAttacking = false;

    private void Start()
    {
        currentHealth = maxHealth;
        audioSource.loop = true;
        audioSource.clip = themeMusic;
        audioSource.Play();
        StartCoroutine(SleepIdleThenRoar());
    }

    private void Update()
    {
        if (isDead || isAttacking) return;
        


        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            FacePlayer();
            if (distanceToPlayer <= attackRadius)
            {
                StartCoroutine(Attack());
            }
        }
    }

    private void FacePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private IEnumerator SleepIdleThenRoar()
    {
        animator.SetTrigger("Sleep");
        yield return new WaitForSeconds(5f);
        animator.SetTrigger("Roar");
        audioSource.PlayOneShot(roarClip);
    }

    private IEnumerator Attack()
    {
        isAttacking = true;
        animator.SetTrigger("Attack");
        audioSource.PlayOneShot(attackClip);
        yield return new WaitForSeconds(1f); // attendre l’impact de l’animation
        // Ajoute ici les dégâts si tu as un script PlayerStats
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    public void TakeDamage(float amount, bool isDefending = false)
    {
        if (isDead) return;

        if (isDefending)
        {
            animator.SetTrigger("Defend");
            return;
        }

        currentHealth -= amount;
        animator.SetTrigger("GetHit");
        audioSource.PlayOneShot(hurtClip);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        audioSource.PlayOneShot(dieClip);
        agent.enabled = false;
    }
}
