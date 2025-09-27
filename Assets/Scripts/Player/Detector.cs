using UnityEngine;

public class Detector : MonoBehaviour
{
    public float viewRadius = 10f;          // Радиус видимости
    [Range(0, 360)]
    public float viewAngle = 90f;           // Угол обзора персонажа

    public LayerMask targetMask;            // Слой цели (враги)
    public LayerMask obstacleMask;          // Слой препятствий (стены)

    public Transform enemyTransform;        // Ссылка на цель (можно найти динамически)

    void Update()
    {
        DetectTarget();
    }

    void DetectTarget()
    {
        // Проверяем, есть ли цель в радиусе
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        foreach (Collider2D target in targetsInViewRadius)
        {
            Vector2 dirToTarget = (target.transform.position - transform.position).normalized;
            float angleToTarget = Vector2.Angle(transform.right, dirToTarget);

            if (angleToTarget < viewAngle / 2)
            {
                float distToTarget = Vector2.Distance(transform.position, target.transform.position);

                // Проверяем, есть ли преграда между персонажем и целью
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask);
                if (hit.collider == null)
                {
                    Debug.Log("Цель обнаружена: " + target.name);
                    enemyTransform = target.gameObject.transform;
                    // Здесь можно поставить логику реагирования на обнаружение врага
                }
            }
        }
    }

    // Для визуализации угла обзора в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * transform.right;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * transform.right;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);
    }
}
