using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator animator;
    private Detector detectorEnemy;
    private HeatPointsController heatPointsController;

    private Vector2 centerPoint;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int speedAttack = 5;

    private Transform target;
    [SerializeField] private float stopRadius = 2f;
    private bool isFollowPlayer = false;

    private bool isRandAction = true;
    private bool isAttacking = false;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        detectorEnemy = GetComponent<Detector>();
        heatPointsController = GetComponent<HeatPointsController>();

        centerPoint = transform.position;

        StartCoroutine(EnemyMove());
    }

    private Vector2 GetNewTargetPosition()
    {

        Vector2 randomOffset = Random.insideUnitCircle * maxDistance;
        Vector2 targetPos = centerPoint + randomOffset;
        return targetPos;
    }

    IEnumerator EnemyMove()
    {
        while (true)
        {
            if (!isRandAction)
            {
                _rb.velocity = Vector2.zero;
                yield return null;
                continue;
            }

            Vector2 targetPos = GetNewTargetPosition();
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

            float distance = Vector2.Distance(transform.position, targetPos);

            while (distance > 0.1f)
            {
                if (!isRandAction)
                {
                    _rb.velocity = Vector2.zero;
                    break;
                }

                _rb.velocity = direction * moveSpeed;
                yield return null;
                distance = Vector2.Distance(transform.position, targetPos);
                direction = (targetPos - (Vector2)transform.position).normalized;
            }

            _rb.velocity = Vector2.zero;

            float waitTime = Random.Range(3f, 6f);
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (!isRandAction)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    public void SetTargetPlayer(Transform newTarget)
    {
        SetMovementActive(false);
        target = newTarget;
        isFollowPlayer = true;
    }
    public void DeleteTargetPlayer()
    {
        SetMovementActive(true);
        target = null;
        isFollowPlayer = false;
    }

    public void SetTargetEnemy()
    {
        SetMovementActive(false);
        target = detectorEnemy.DetectTarget();
        isFollowPlayer = false;
        if (target == null)
        {
            SetMovementActive(true);
            target = null;
            Debug.Log("Not found enemy");
        }

    }

    void Update()
    {
        //TODO: Сделать логику для преследования врага и атаки, и логику для перса
        if (!isRandAction)
        {
            if (isFollowPlayer)
                FollowToPlayer();
            else
                FollowToEnemy();
                
        }
    }

    private void FollowToPlayer()
    {
        
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stopRadius)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {

        }
    }

    private void FollowToEnemy()
    {
        if (target == null) { 
            isRandAction = true;
            return;
        }

        Vector3 direction = target.position - transform.position;

        Vector3 moveDir = direction.normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        if (detectorEnemy.GetCanHit() && !isAttacking)
        {
            StartCoroutine(AttackAction());
        }
    }

    IEnumerator AttackAction()
    {
        isAttacking = true;
        yield return new WaitForSeconds(speedAttack);
        bool isDead = target.GetComponent<HeatPointsController>().TakeDamage(1);
        if(!isDead)
            target.GetComponent<EnemyController>().StartCoroutine(ReactByAttack(gameObject));

        isAttacking = false;
    }
    IEnumerator ReactByAttack(GameObject newTarget)
    {
        SetTargetEnemy();
        FollowToEnemy();
        yield return new WaitForSeconds(2f);
        newTarget.GetComponent<HeatPointsController>().TakeDamage(1); 

        
    }
    
    

    public void SetMovementActive(bool active)
    {
        isRandAction = active;
        if (!active)
            _rb.velocity = Vector2.zero;
        if (active)
            centerPoint = transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPoint, maxDistance);
    }
    

}
