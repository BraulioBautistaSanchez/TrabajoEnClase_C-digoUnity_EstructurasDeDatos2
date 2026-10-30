using UnityEngine;

// Agente 2D con tres steering behaviours:
//  - Path Following: sigue los nodos de la lista ligada (PathManager) uno por uno
//  - Arrival:        frena suavemente al acercarse a cada queso
//  - Obstacle Avoidance: esquiva obstáculos con un CircleCast hacia adelante
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] private PathManager path;

    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 4f;    // velocidad máxima
    [SerializeField] private float maxForce = 10f;   // qué tan rápido puede cambiar de dirección

    [Header("Arrival")]
    [SerializeField] private float slowingRadius = 2f;  // desde aquí empieza a frenar
    [SerializeField] private float eatDistance = 0.2f;  // a esta distancia "se come" el queso
    [SerializeField] private float eatPause = 0.5f;     // pausa después de comer

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleMask;    // capa "Obstacle"
    [SerializeField] private float lookAhead = 1.5f;    // qué tan lejos "ve"
    [SerializeField] private float agentRadius = 0.3f;  // grosor del rayo
    [SerializeField] private float avoidForce = 15f;    // fuerza máxima de esquive

    private Vector2 velocity;
    private float pauseTimer;

    private void Update()
    {
        if (path == null || path.Finished)
        {
            velocity = Vector2.zero;
            return;
        }

        // Pausa corta después de comer un queso
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        Cheese target = path.CurrentCheese;
        Vector2 pos = transform.position;
        Vector2 targetPos = target.transform.position;

        // ¿Llegamos? Nos comemos el queso y pasamos al siguiente nodo de la lista
        if (Vector2.Distance(pos, targetPos) <= eatDistance)
        {
            target.gameObject.SetActive(false);
            path.Advance();
            pauseTimer = eatPause;
            velocity = Vector2.zero;
            return;
        }

        // Sumamos las fuerzas de cada comportamiento
        Vector2 steering = Arrive(pos, targetPos) + AvoidObstacles(pos, targetPos);
        steering = Vector2.ClampMagnitude(steering, maxForce);

        // Aplicamos la fuerza a la velocidad y movemos
        velocity = Vector2.ClampMagnitude(velocity + steering * Time.deltaTime, maxSpeed);
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // Rotamos hacia donde nos movemos (-90 porque el sprite mira hacia ARRIBA;
        // si tu sprite mira a la derecha, quita el -90)
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    // ARRIVAL: velocidad deseada proporcional a la distancia dentro del radio de frenado.
    // steering = velocidad deseada - velocidad actual
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

    // OBSTACLE AVOIDANCE: lanzamos un círculo hacia adelante; si choca con algo,
    // empujamos al agente lejos de la superficie y hacia el lado que da al objetivo.
    private Vector2 AvoidObstacles(Vector2 pos, Vector2 targetPos)
    {
        Vector2 dir = velocity.sqrMagnitude > 0.01f
            ? velocity.normalized
            : (targetPos - pos).normalized;

        RaycastHit2D hit = Physics2D.CircleCast(pos, agentRadius, dir, lookAhead, obstacleMask);
        if (!hit) return Vector2.zero;

        // Lado perpendicular que apunta hacia el objetivo (evita quedarse trabado de frente)
        Vector2 side = Vector2.Perpendicular(dir);
        if (Vector2.Dot(side, targetPos - pos) < 0f) side = -side;

        Vector2 avoidDir = (hit.normal + side).normalized;

        // Entre más cerca el obstáculo, más fuerte el esquive
        float urgency = 1f - hit.distance / lookAhead;
        return avoidDir * avoidForce * urgency;
    }

    // Dibuja el "rayo" en la vista Scene al seleccionar el agente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 end = transform.position + (Vector3)(velocity.normalized * lookAhead);
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawWireSphere(end, agentRadius);
    }
}
