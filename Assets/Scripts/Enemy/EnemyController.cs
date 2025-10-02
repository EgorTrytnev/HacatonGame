using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PhotonView))]
public class EnemyController : MonoBehaviourPun
{
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Animator _animator;
    private Detector _detectorEnemy;
    private HeatPointsController _hp;

    // Patrol / move
    private Vector2 centerPoint;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float multiplySpeed = 1.5f;

    // Combat
    [SerializeField] private int speedAttack = 5;

    // Targets
    private Transform target;
    private Transform currentAttacker;
    [SerializeField] private float stopRadiusPlayer = 2f;
    [SerializeField] private float stopRadiusEnemy = 0.3f;
    [SerializeField] private float pursuitRadius = 6f;

    // Steering
    [SerializeField] private float slowRadius = 3f;
    [SerializeField] private float seekWeight = 1.0f;
    [SerializeField] private float fleeWeight = 1.6f;
    [SerializeField] private float dangerRadius = 2.0f;

    // States
    private bool isFollowPlayer = false;
    private bool isRandAction = true;
    private bool isAttacking = false;

    // Movement
    private Vector2 desiredVelocity = Vector2.zero;

    private Coroutine attackCoroutine;
    private Coroutine patrolCoroutine;

    // NEW: anti-stick windows
    [SerializeField] private float followLockDuration = 1.5f;   // после Follow: не переходить в погоню
    [SerializeField] private float reaggroBlockDuration = 0.8f; // сразу после отмены: не ре-агриться
    private float followLockUntil = -1f;
    private float reaggroBlockUntil = -1f;

    bool HasAuth => photonView.IsMine; // ИИ/физика только у владельца [PUN ownership]

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _detectorEnemy = GetComponent<Detector>();
        _hp = GetComponent<HeatPointsController>();

        centerPoint = transform.position;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        if (HasAuth)
            patrolCoroutine = StartCoroutine(WanderPatrol());
    }

    void Update()
    {
        if (!HasAuth) return;

        if (isRandAction) return;

        if (target == null)
        {
            StopAttack();
            SetMovementActive(true);
            return;
        }

        if (isFollowPlayer) FollowToPlayer_Steering();
        else FollowToEnemy();
    }

    void FixedUpdate()
    {
        if (!HasAuth) return;
        _rb.linearVelocity = desiredVelocity;
    }

    // ---------- Patrol ----------
    private Vector2 GetNewTargetPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * maxDistance;
        return centerPoint + randomOffset;
    }

    IEnumerator WanderPatrol()
    {
        while (true)
        {
            if (!isRandAction)
            {
                desiredVelocity = Vector2.zero;
                yield return null;
                continue;
            }

            Vector2 targetPos = GetNewTargetPosition();
            float distance = Vector2.Distance(transform.position, targetPos);

            while (distance > 0.1f)
            {
                if (!isRandAction) break;
                Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
                desiredVelocity = direction * moveSpeed;
                distance = Vector2.Distance(transform.position, targetPos);
                yield return null;
            }

            desiredVelocity = Vector2.zero;

            float waitTime = Random.Range(3f, 6f);
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (!isRandAction) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            centerPoint = transform.position;
        }
    }

    // ---------- Public API ----------
    public void SetMainTarget(Transform newTarget)
    {
        int id = ToViewId(newTarget);
        if (HasAuth) RPC_SetMainTarget(id);
        else photonView.RPC(nameof(RPC_SetMainTarget), photonView.Owner, id);
    }

    public void DeleteMainTarget()
    {
        if (HasAuth) RPC_DeleteMainTarget();
        else photonView.RPC(nameof(RPC_DeleteMainTarget), photonView.Owner);
    }

    public void SetTargetEnemy()
    {
        if (HasAuth) RPC_SetTargetEnemyAuto();
        else photonView.RPC(nameof(RPC_SetTargetEnemyAuto), photonView.Owner);
    }

    public void SetTargetEnemy(Transform targetEnemy)
    {
        int id = ToViewId(targetEnemy);
        if (HasAuth) RPC_SetTargetEnemy(id);
        else photonView.RPC(nameof(RPC_SetTargetEnemy), photonView.Owner, id);
    }

    // ---------- RPC (owner side) ----------
    [PunRPC] void RPC_SetMainTarget(int targetViewId)
    {
        StopAttack();
        SetMovementActive(false);
        target = FromViewId(targetViewId);
        isFollowPlayer = target != null;
        currentAttacker = null;

        if (isFollowPlayer)
            followLockUntil = Time.time + followLockDuration; // NEW: приказы важнее входящих ударов
    }

    [PunRPC] void RPC_DeleteMainTarget()
    {
        SetMovementActive(true);
        target = null;
        isFollowPlayer = false;
        currentAttacker = null;

        reaggroBlockUntil = Time.time + reaggroBlockDuration; // NEW: защита от мгновенного ре-агра
    }

    [PunRPC] void RPC_SetTargetEnemyAuto()
    {
        SetMovementActive(false);
        target = _detectorEnemy != null ? _detectorEnemy.DetectTargetAuth() : null;
        isFollowPlayer = false;
        if (target == null) SetMovementActive(true);
        else SetSpeedForLeaveFunc();
    }

    [PunRPC] void RPC_SetTargetEnemy(int targetViewId)
    {
        SetMovementActive(false);
        target = FromViewId(targetViewId);
        isFollowPlayer = false;
        if (target == null) SetMovementActive(true);
        else SetSpeedForLeaveFunc();
    }

    // ---------- Combat ----------
    void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        isAttacking = false;
    }

    IEnumerator AttackAction()
    {
        isAttacking = true;
        yield return new WaitForSeconds(speedAttack);
        if (isFollowPlayer || target == null) { isAttacking = false; yield break; }

        // 1) Урон — адресно владельцу HP цели (RPC_TakeDamage на том же PV, где HP)
        var hp = target.GetComponentInParent<HeatPointsController>();
        var hpPv = hp ? hp.GetComponent<PhotonView>() : null;
        if (hpPv != null)
            hpPv.RPC("RPC_TakeDamage", hpPv.Owner, 1); // адресный RPC владельцу [docs]

        // 2) Реакция жертвы — адресно её EnemyController (anti-stick учитывается на стороне жертвы)
        var victimEc = target.GetComponentInParent<EnemyController>();
        var victimPv = victimEc ? victimEc.GetComponent<PhotonView>() : null;
        if (victimPv != null)
            victimPv.RPC(nameof(RPC_ReactByAttack), victimPv.Owner, photonView.ViewID, false);

        isAttacking = false;
    }

    // NEW: реакция на удар с учётом замков
    [PunRPC]
    void RPC_ReactByAttack(int attackerViewId, bool forceChase = false)
    {
        var attackerT = FromViewId(attackerViewId);
        if (attackerT == null) return;

        // Если есть активный Follow-замок — не переключаемся в погоню, только ускоряем отход
        if (!forceChase && isFollowPlayer && Time.time < followLockUntil)
        {
            currentAttacker = attackerT;
            SetSpeedForLeaveFunc();
            return;
        }

        // Если только что отменили бой — коротко игнорируем ре-агр
        if (!forceChase && Time.time < reaggroBlockUntil)
        {
            currentAttacker = attackerT;
            return;
        }

        // Стандартная погоня за обидчиком
        isFollowPlayer = false;
        SetMovementActive(false);
        SetTargetEnemy(attackerT);
    }

    public void SetMovementActive(bool active)
    {
        isRandAction = active;
        desiredVelocity = Vector2.zero;

        if (!HasAuth) return;

        if (active)
        {
            if (patrolCoroutine == null)
                patrolCoroutine = StartCoroutine(WanderPatrol());
            centerPoint = transform.position;
        }
        else
        {
            if (patrolCoroutine != null)
            {
                StopCoroutine(patrolCoroutine);
                patrolCoroutine = null;
            }
            _rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetSpeedForLeaveFunc() => StartCoroutine(SpeedBoost());

    public IEnumerator SpeedBoost()
    {
        float originalSpeed = moveSpeed;
        moveSpeed = originalSpeed * multiplySpeed;
        yield return new WaitForSeconds(2f);
        moveSpeed = originalSpeed;
    }

    private void FollowToPlayer_Steering()
    {
        if (target == null) { desiredVelocity = Vector2.zero; return; }

        Vector2 pos = _rb.position;
        Vector2 toPlayer = (Vector2)target.position - pos;
        float distToPlayer = toPlayer.magnitude;

        if (distToPlayer <= stopRadiusPlayer)
        {
            desiredVelocity = Vector2.zero;
            currentAttacker = null;
            return;
        }

        Vector2 dirSeek = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.zero;

        Vector2 dirFlee = Vector2.zero;
        float wFlee = fleeWeight;

        if (currentAttacker != null)
        {
            Vector2 fromAttacker = pos - (Vector2)currentAttacker.position;
            if (fromAttacker.sqrMagnitude > 0.0001f) dirFlee = fromAttacker.normalized;
            float dA = fromAttacker.magnitude;
            if (dA < dangerRadius) wFlee *= 1.5f;
        }

        Vector2 blended = (seekWeight * dirSeek) + (wFlee * dirFlee);
        if (blended.sqrMagnitude > 0.0001f) blended.Normalize();

        float slowFactor = distToPlayer < slowRadius ? (distToPlayer / slowRadius) : 1f;
        desiredVelocity = blended * moveSpeed * slowFactor;
    }

    private void FollowToEnemy()
    {
        if (target == null) { SetMovementActive(true); return; }

        float distToTarget = Vector2.Distance(transform.position, target.position);
        if (distToTarget > pursuitRadius)
        {
            StopAttack();
            target = null;
            SetMovementActive(true);
            return;
        }

        Vector2 toEnemy = (Vector2)target.position - _rb.position;
        float distance = toEnemy.magnitude;

        desiredVelocity = (distance > stopRadiusEnemy)
            ? (distance > 0.0001f ? toEnemy.normalized * moveSpeed : Vector2.zero)
            : Vector2.zero;

        if (_detectorEnemy != null && _detectorEnemy.GetCanHitAuth() && !isAttacking)
            attackCoroutine = StartCoroutine(AttackAction());
    }

    // helpers: ViewID <-> Transform
    static int ToViewId(Transform t)
    {
        if (t == null) return 0;
        var pv = t.GetComponentInParent<PhotonView>();
        return pv != null ? pv.ViewID : 0;
    }
    static Transform FromViewId(int id) => id != 0 ? PhotonView.Find(id)?.transform : null;
}
