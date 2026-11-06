using UnityEngine;

// El agente NO decide qué hacer en cada momento (eso lo hacen los estados).
// Solo ofrece "herramientas": calcular fuerzas, moverse, detectar obstáculos, comer.
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] private PathManager path;

    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 4f;
    [SerializeField] private float maxForce = 15f;

    [Header("Arrival")]
    [SerializeField] private float slowingRadius = 2f;
    [SerializeField] private float minArrivalSpeed = 0.8f;  // evita "arrastrarse" al llegar
    [SerializeField] private float eatDistance = 0.4f;      // distancia a la que se come el queso
    [SerializeField] private float eatPause = 0.5f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float lookAhead = 2.5f;     // qué tan lejos "ve" hacia el queso
    [SerializeField] private float agentRadius = 0.3f;   // tamaño del agente (grosor del rayo)
    [SerializeField] private float sideLockTime = 1.5f;  // tiempo que mantiene el lado elegido
    [SerializeField] private float probeDistance = 3f;   // alcance de las sondas laterales

    [Header("Anti-atasco")]
    [SerializeField] private float stuckCheckInterval = 1f;   // cada cuánto revisa si avanza
    [SerializeField] private float stuckMinProgress = 0.5f;   // avance mínimo en ese tiempo

    [Header("Máquina de estados")]
    [SerializeField] private float idleTime = 1f;          // espera inicial
    [SerializeField] private float avoidClearTime = 0.3f;  // camino libre antes de volver a Moving

    // ---------- Acceso para los estados (solo lectura) ----------
    public PathManager Path => path;
    public float IdleTime => idleTime;
    public float EatPause => eatPause;
    public float AvoidClearTime => avoidClearTime;

    // ---------- La máquina y sus estados ----------
    public StateMachine Fsm { get; private set; }
    public IState StateIdle { get; private set; }
    public IState StateMoving { get; private set; }
    public IState StateAvoiding { get; private set; }
    public IState StateEating { get; private set; }
    public IState StateFinished { get; private set; }

    private Vector2 velocity;

    // Lado por el que se rodea el obstáculo: +1 = izquierda, -1 = derecha
    private float avoidSide = 1f;
    private float avoidSideUntil;   // hasta cuándo se mantiene ese lado

    // Detector de atasco
    private Vector2 stuckCheckPos;
    private float stuckCheckTime;

    private Vector2 Position => transform.position;
    private Vector2 TargetPosition => path.CurrentCheese.transform.position;

    // ---------- Ciclo de vida de Unity ----------

    private void Awake()
    {
        Fsm = new StateMachine();
        StateIdle = new IdleState(this);
        StateMoving = new MovingState(this);
        StateAvoiding = new AvoidingState(this);
        StateEating = new EatingState(this);
        StateFinished = new FinishedState(this);
    }

    private void Start()
    {
        Fsm.ChangeState(StateIdle);
    }

    private void Update()
    {
        Fsm.Tick();
    }

    // ---------- Herramientas que usan los estados ----------

    public void Stop()
    {
        velocity = Vector2.zero;
    }

    public bool HasReachedTarget()
    {
        if (path.Finished) return false;
        return Vector2.Distance(Position, TargetPosition) <= eatDistance;
    }

    public void EatCurrentCheese()
    {
        path.CurrentCheese.gameObject.SetActive(false);
        path.Advance();         // avanza el puntero de la lista ligada
        avoidSideUntil = 0f;    // con un objetivo nuevo, olvidamos el lado elegido
    }

    // ARRIVAL: fuerza para ir al queso frenando suave.
    // La rapidez baja con la distancia, pero nunca por debajo de minArrivalSpeed:
    // sin ese piso, el agente se "arrastra" y tarda mucho en cubrir los últimos centímetros.
    public Vector2 ComputeArrive()
    {
        Vector2 toTarget = TargetPosition - Position;
        float distance = toTarget.magnitude;

        float desiredSpeed = maxSpeed;
        if (distance < slowingRadius)
            desiredSpeed = Mathf.Max(maxSpeed * (distance / slowingRadius), minArrivalSpeed);

        Vector2 desiredVelocity = toTarget.normalized * desiredSpeed;
        return desiredVelocity - velocity;
    }

    // DETECCIÓN: ¿hay un obstáculo entre el agente y el queso?
    // El rayo apunta al QUESO y se detiene una distancia 'agentRadius' ANTES de él,
    // así un muro pegado al queso no cuenta como obstáculo.
    public bool TryDetectObstacle(out RaycastHit2D hit)
    {
        hit = default;

        Vector2 toTarget = TargetPosition - Position;
        float distance = Mathf.Min(lookAhead, toTarget.magnitude - agentRadius);
        if (distance <= 0f) return false;   // ya estamos junto al queso: nada que esquivar

        hit = Physics2D.CircleCast(Position, agentRadius, toTarget.normalized, distance, obstacleMask);
        return hit.collider != null;
    }

    // OBSTACLE AVOIDANCE: fuerza para BORDEAR el obstáculo.
    public Vector2 ComputeAvoid(RaycastHit2D hit)
    {
        if (Time.time > avoidSideUntil)
        {
            // Episodio nuevo de esquive: elegimos lado y empezamos a vigilar el progreso
            avoidSide = ChooseAvoidSide(hit);
            ResetStuckCheck();
        }
        else if (Time.time >= stuckCheckTime)
        {
            // Llevamos esquivando un rato: ¿avanzamos de verdad?
            if (Vector2.Distance(Position, stuckCheckPos) < stuckMinProgress)
            {
                avoidSide = -avoidSide;     // atascados: probamos el otro lado
                Debug.Log("[Agente] Atascado: cambio de lado de esquive");
            }
            ResetStuckCheck();
        }
        avoidSideUntil = Time.time + sideLockTime;

        // Tangente: dirección paralela a la superficie golpeada.
        // (Perpendicular(normal) apunta a la derecha del agente, de ahí el signo menos)
        Vector2 tangent = -Vector2.Perpendicular(hit.normal) * avoidSide;

        // Urgencia: 0 = obstáculo lejos, 1 = pegado. Cerca, también nos separamos de él.
        float urgency = Mathf.Clamp01(1f - hit.distance / lookAhead);

        Vector2 desiredDir = (tangent + hit.normal * urgency).normalized;
        return desiredDir * maxSpeed - velocity;    // fuerza = deseada - actual
    }

    // Elige el lado con SONDAS: lanza un círculo hacia cada lado y se va por donde hay
    // más espacio libre. Así no se mete en una esquina (muro + piso).
    private float ChooseAvoidSide(RaycastHit2D hit)
    {
        Vector2 tangentLeft = -Vector2.Perpendicular(hit.normal);   // lado +1
        float freeLeft = FreeDistanceAlong(tangentLeft);
        float freeRight = FreeDistanceAlong(-tangentLeft);

        // Si un lado está claramente más libre, ese gana
        if (Mathf.Abs(freeLeft - freeRight) > 0.3f)
            return freeLeft > freeRight ? 1f : -1f;

        // Empate: rodeamos por la orilla más cercana (lado opuesto al centro del obstáculo)
        Vector2 toTarget = (TargetPosition - Position).normalized;
        Vector2 toObstacle = (Vector2)hit.collider.bounds.center - Position;
        float cross = toTarget.x * toObstacle.y - toTarget.y * toObstacle.x;
        return cross > 0f ? -1f : 1f;
    }

    // Cuánto espacio libre hay en esa dirección (máximo probeDistance)
    private float FreeDistanceAlong(Vector2 dir)
    {
        RaycastHit2D h = Physics2D.CircleCast(Position, agentRadius, dir, probeDistance, obstacleMask);
        return h.collider == null ? probeDistance : h.distance;
    }

    private void ResetStuckCheck()
    {
        stuckCheckPos = Position;
        stuckCheckTime = Time.time + stuckCheckInterval;
    }

    // Aplica una fuerza: limita, actualiza velocidad, evita traspasar, mueve y rota
    public void ApplySteering(Vector2 steering)
    {
        steering = Vector2.ClampMagnitude(steering, maxForce);
        velocity = Vector2.ClampMagnitude(velocity + steering * Time.deltaTime, maxSpeed);

        Vector2 move = velocity * Time.deltaTime;

        // MURO FÍSICO: si este paso nos metería en un obstáculo, quitamos la parte de la
        // velocidad que empuja contra él. El agente se desliza en vez de traspasarlo.
        if (move.sqrMagnitude > 0.000001f)
        {
            RaycastHit2D block = Physics2D.CircleCast(
                Position, agentRadius, move.normalized, move.magnitude + 0.02f, obstacleMask);

            if (block.collider != null)
            {
                Vector2 normal = block.normal;

                // Si ya estamos tocando, la normal del cast no sirve: la calculamos a mano
                if (block.distance <= 0f)
                    normal = (Position - block.collider.ClosestPoint(Position)).normalized;

                float into = Vector2.Dot(velocity, normal);
                if (into < 0f) velocity -= into * normal;

                move = velocity * Time.deltaTime;
            }
        }

        transform.position += (Vector3)move;

        // Rotamos hacia donde nos movemos (-90 porque el sprite mira hacia ARRIBA;
        // si tu sprite mira a la derecha, quita el -90)
        if (velocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

    // ---------- Depuración ----------

    private void OnGUI()
    {
        string state = Fsm != null && Fsm.Current != null ? Fsm.Current.GetType().Name : "-";
        GUI.Label(new Rect(10, 50, 600, 25), $"Estado: {state}");
    }

    // Muestra el "rayo" hacia el queso al seleccionar el agente
    private void OnDrawGizmosSelected()
    {
        if (path == null || path.Finished || path.CurrentCheese == null) return;

        Gizmos.color = Color.yellow;
        Vector3 dir = (path.CurrentCheese.transform.position - transform.position).normalized;
        Vector3 end = transform.position + dir * lookAhead;
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawWireSphere(end, agentRadius);
    }
}