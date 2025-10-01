using UnityEngine;

public class Detector : MonoBehaviour
{
    [SerializeField]private float viewRadius = 10f;          // Радиус видимости
    private float viewAngle = 360f;           

    public LayerMask targetMask;            
    public LayerMask obstacleMask;

    private bool canHit = false;



    public Transform DetectTarget()
    {
        Transform unitTransform = null;
        // Проверяем, есть ли цель в радиусе
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        foreach (Collider2D target in targetsInViewRadius)
        {
            Vector2 dirToTarget = (target.transform.position - transform.position).normalized;

            float distToTarget = Vector2.Distance(transform.position, target.transform.position);

            // Проверяем, есть ли преграда между персонажем и целью
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask);
            var targetUnit = target.GetComponent<UnitsDefinition>();
            var myUnit = gameObject.GetComponent<UnitsDefinition>();

            if (targetUnit != null && myUnit != null)
            {
                if (hit.collider == null && targetUnit.GetTeam() != myUnit.GetTeam())
                {
                    Debug.Log("Цель обнаружена: " + target.name);
                    unitTransform = target.gameObject.transform;
                }
            }
            else if (myUnit == null)
            {
                Debug.LogWarning("Отсутствует компонент UnitsDefinition у цели или у детектора");
            }
            else
            {
                Debug.Log("His in my team");
            }


        }
        return unitTransform;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = gameObject.GetComponent<UnitsDefinition>();
        if (targetUnit != null && myUnit != null)
        {
            if (targetUnit != myUnit)
            {
                canHit = true;
            }
        }

    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = gameObject.GetComponent<UnitsDefinition>();
        if (targetUnit != null && myUnit != null)
        {
            if (targetUnit != myUnit)
            {
                canHit = false;
            }
        }
    }

    public bool GetCanHit()
    {
        return canHit;
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
