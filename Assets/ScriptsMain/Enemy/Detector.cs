using Photon.Pun;
using UnityEngine;

public class Detector : MonoBehaviourPun
{
    [SerializeField] private float viewRadius = 10f;
    private float viewAngle = 360f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    private bool canHit = false;

    public Transform DetectTargetAuth()
    {
        if (!photonView.IsMine) return null;

        Transform unitTransform = null;
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewRadius, targetMask);

        foreach (Collider2D c in targetsInViewRadius)
        {
            Vector2 dirToTarget = (c.transform.position - transform.position).normalized;
            float dist = Vector2.Distance(transform.position, c.transform.position);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, dist, obstacleMask);

            var targetUnit = c.GetComponent<UnitsDefinition>();
            var myUnit = GetComponent<UnitsDefinition>();
            if (targetUnit != null && myUnit != null && hit.collider == null && targetUnit.GetTeam() != myUnit.GetTeam())
            {
                unitTransform = c.transform;
                break;
            }
        }
        return unitTransform;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;
        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = GetComponent<UnitsDefinition>();
        if (targetUnit != null && myUnit != null && targetUnit != myUnit)
            canHit = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!photonView.IsMine) return;
        var targetUnit = collision.GetComponent<UnitsDefinition>();
        var myUnit = GetComponent<UnitsDefinition>();
        if (targetUnit != null && myUnit != null && targetUnit != myUnit)
            canHit = false;
    }

    public bool GetCanHitAuth() => photonView.IsMine && canHit;
}
