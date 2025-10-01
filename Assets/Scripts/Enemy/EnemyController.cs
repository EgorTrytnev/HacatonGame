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
    [SerializeField] private float multiplySpeed = 1.5f;
    [SerializeField] private int speedAttack = 5;

    private Transform target;
    [SerializeField] private float stopRadiusPlayer = 2f;
    [SerializeField] private float stopRadiusEnemy = 0.3f;
    private bool isFollowPlayer = false;

    private bool isRandAction = true;
    private bool isAttacking = false;

    private Coroutine attackCoroutine;

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
                _rb.linearVelocity = Vector2.zero;
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
                    _rb.linearVelocity = Vector2.zero;
                    break;
                }

                _rb.linearVelocity = direction * moveSpeed;
                yield return null;
                distance = Vector2.Distance(transform.position, targetPos);
                direction = (targetPos - (Vector2)transform.position).normalized;
            }

            _rb.linearVelocity = Vector2.zero;

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

    public void SetMainTarget(Transform newTarget)
    {
        SetMovementActive(false);
        target = newTarget;
        isFollowPlayer = true;
    }
    public void DeleteMainTarget()
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
        Debug.Log("Set target - " + target.name);

    }
    public void SetTargetEnemy(Transform targetEnemy)
    {
        SetMovementActive(false);
        target = targetEnemy;
        isFollowPlayer = false;
    }

    void Update()
    {
        if (isRandAction) return;

        if (target == null)
        {
            isRandAction = true;
            StopAttack();
            return;
        }
        if (isFollowPlayer)
            FollowToPlayer();
        else
            FollowToEnemy();
    }
    
    void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
            SetSpeedForLeave();
        }
        isAttacking = false;
    }



    private void FollowToPlayer()
    {

        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stopRadiusPlayer)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        else
        {

        }
    }

    public void SetSpeedForLeaveFunc()
    {
        StartCoroutine(SetSpeedForLeave());
    }

    IEnumerator SetSpeedForLeave()
    {
        Debug.Log("LEEEEEAVE");
        moveSpeed *= multiplySpeed;
        yield return new WaitForSeconds(2f);
        moveSpeed /= multiplySpeed;
    }

    private void FollowToEnemy()
    {
        if (target == null)
        {
            isRandAction = true;
            return;
        }

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        if (distance > stopRadiusEnemy)
        {
            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
        if (detectorEnemy.GetCanHit() && !isAttacking)
        {
            attackCoroutine = StartCoroutine(AttackAction());
        }
    }

IEnumerator AttackAction()
{
    isAttacking = true;
    yield return new WaitForSeconds(speedAttack);

    if (target == null)
        {
            isAttacking = false;
            yield break;
        }

    var hp = target.GetComponent<HeatPointsController>();
    if (hp == null)
    {
        isAttacking = false;
        yield break;
    }

    bool isDead = hp.TakeDamage(1);
    if (!isDead)
    {
        var targetUnit = target.GetComponent<EnemyController>();

        if (targetUnit != null)
            targetUnit.ReactByAttack(gameObject.transform);
        else
            Debug.Log("Less");
    }

    isAttacking = false;
}

    public void ReactByAttack(Transform attacker)
    {
        // При атаке начинаем преследовать атаковавшего
        SetTargetEnemy(attacker);
    }

    

    public void SetMovementActive(bool active)
    {
        isRandAction = active;
        if (!active)
            _rb.linearVelocity = Vector2.zero;
        else
            centerPoint = transform.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(centerPoint, maxDistance);
    }
    

}
