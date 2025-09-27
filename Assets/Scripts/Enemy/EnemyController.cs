using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator animator;

    private Vector2 centerPoint;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;

    private Transform target;
    [SerializeField]private float stopRadius = 2f;

    private bool isRandAction = true;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

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

    public void SetTarget(Transform newTarget)
    {
        SetMovementActive(false);
        target = newTarget;
    }
    public void DeleteTarget()
    {
        SetMovementActive(true);
        target = null;
    }

    void Update()
    {
        if (!isRandAction)
        {
            if (target == null)
                return;

            Vector3 direction = (target.position - transform.position);
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
