using UnityEngine;
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] private PathManager path;

    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 4f;  
    [SerializeField] private float maxForce = 10f; 

    [Header("Arrival")]
    [SerializeField] private float slowingRadius = 2f;  
    [SerializeField] private float eatDistance = 0.2f; 
    [SerializeField] private float eatPause = 0.5f;    

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleMask;    
    [SerializeField] private float lookAhead = 1.5f;    
    [SerializeField] private float agentRadius = 0.3f;  
    [SerializeField] private float avoidForce = 15f;   

    private Vector2 velocity;
    private float pauseTimer;

    private void Update()
    {
        if (path == null || path.Finished)
        {
            velocity = Vector2.zero;
            return;
        }

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        Cheese target = path.CurrentCheese;
        Vector2 pos = transform.position;
        Vector2 targetPos = target.transform.position;

        if (Vector2.Distance(pos, targetPos) <= eatDistance)
        {
            target.gameObject.SetActive(false);
            path.Advance();
            pauseTimer = eatPause;
            velocity = Vector2.zero;
            return;
        }

        Vector2 steering = Arrive(pos, targetPos) + AvoidObstacles(pos, targetPos);
        steering = Vector2.ClampMagnitude(steering, maxForce);

        velocity = Vector2.ClampMagnitude(velocity + steering * Time.deltaTime, maxSpeed);
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    private Vector2 Arrive(Vector2 pos, Vector2 targetPos)
    {
        Vector2 toTarget = targetPos - pos;
        float distance = toTarget.magnitude;

        float desiredSpeed = maxSpeed;
        if (distance < slowingRadius)
            desiredSpeed = maxSpeed * (distance / slowingRadius);

        Vector2 desiredVelocity = toTarget.normalized * desiredSpeed;
        return desiredVelocity - velocity;
    }

    private Vector2 AvoidObstacles(Vector2 pos, Vector2 targetPos)
    {
        Vector2 dir = velocity.sqrMagnitude > 0.01f
            ? velocity.normalized
            : (targetPos - pos).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(pos, agentRadius, dir, lookAhead, obstacleMask);
        if (!hit) return Vector2.zero;

        Vector2 side = Vector2.Perpendicular(dir);
        if (Vector2.Dot(side, targetPos - pos) < 0f) side = -side;

        Vector2 avoidDir = (hit.normal + side).normalized;

        float urgency = 1f - hit.distance / lookAhead;
        return avoidDir * avoidForce * urgency;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 end = transform.position + (Vector3)(velocity.normalized * lookAhead);
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawWireSphere(end, agentRadius);
    }
}
