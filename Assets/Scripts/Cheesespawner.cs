using System.Collections.Generic;
using UnityEngine;

// Hace dos cosas:
//  1) Al iniciar, cambia de lugar los quesos de la escena para que nunca sean los mismos.
//  2) Durante el juego, crea quesos nuevos en posiciones aleatorias y los agrega a la lista ligada.
//
// Técnica: "muestreo por rechazo". Se sortea una posición al azar, se revisa si cumple
// las reglas y, si no las cumple, se descarta y se vuelve a sortear (con un máximo de intentos).
public class CheeseSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PathManager path;
    [SerializeField] private Transform agent;
    [SerializeField] private Cheese cheesePrefab;   // prefab con el componente Cheese

    [Header("Área del mapa (se dibuja en cian en la vista Scene)")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private Vector2 mapSize = new Vector2(16f, 9f);
    [SerializeField] private float edgeMargin = 0.5f;   // separación mínima con los bordes

    [Header("Reglas de posición")]
    [SerializeField] private LayerMask obstacleMask;            // capa "Obstacle"
    [SerializeField] private float obstacleClearRadius = 0.6f;  // espacio libre alrededor del queso
    [SerializeField] private float minDistanceBetweenCheeses = 3f;
    [SerializeField] private float minDistanceFromAgent = 1.5f;
    [SerializeField] private int maxAttempts = 50;

    [Header("Aleatoriedad inicial")]
    [SerializeField] private bool randomizeOnStart = true;

    [Header("Quesos extra")]
    [SerializeField] private KeyCode spawnKey = KeyCode.N;
    [SerializeField] private float autoSpawnInterval = 0f;  // segundos; 0 = desactivado
    [SerializeField] private int maxBonusCheeses = 3;

    private int bonusCount;
    private float autoTimer;

    private void Start()
    {
        if (randomizeOnStart) RandomizeInitialCheeses();
    }

    private void Update()
    {
        if (Input.GetKeyDown(spawnKey)) SpawnBonusCheese();

        if (autoSpawnInterval > 0f)
        {
            autoTimer += Time.deltaTime;
            if (autoTimer >= autoSpawnInterval)
            {
                autoTimer = 0f;
                SpawnBonusCheese();
            }
        }
    }

    // ---------- 1) Reubicar los quesos iniciales ----------

    private void RandomizeInitialCheeses()
    {
        // Posiciones ya "reservadas" por los quesos que vamos colocando
        List<Vector2> occupied = new List<Vector2>();

        foreach (Cheese cheese in path.AllCheeses)
        {
            if (TryGetValidPosition(occupied, out Vector2 pos))
            {
                cheese.transform.position = pos;
                occupied.Add(pos);
            }
            else
            {
                Debug.LogWarning($"No encontré lugar válido para {cheese.Label}; se queda donde estaba.");
                occupied.Add(cheese.transform.position);
            }
        }
    }

    // ---------- 2) Crear un queso nuevo en juego ----------

    public void SpawnBonusCheese()   // pública: también se puede llamar desde un botón de UI
    {
        if (bonusCount >= maxBonusCheeses) return;

        // Solo cuentan los quesos que siguen en el mapa (los comidos están desactivados)
        List<Vector2> occupied = new List<Vector2>();
        foreach (Cheese c in path.AllCheeses)
            if (c.gameObject.activeInHierarchy) occupied.Add(c.transform.position);

        if (!TryGetValidPosition(occupied, out Vector2 pos))
        {
            Debug.LogWarning("No encontré lugar válido para un queso nuevo.");
            return;
        }

        Cheese cheese = Instantiate(cheesePrefab, pos, Quaternion.identity);
        bonusCount++;
        cheese.type = CheeseType.BonusCheese;
        cheese.name = $"BonusCheese_{bonusCount}";

        path.AddCheese(cheese);     // se engancha como nodo nuevo en la lista ligada
    }

    // ---------- Búsqueda de una posición válida ----------

    private bool TryGetValidPosition(List<Vector2> occupied, out Vector2 result)
    {
        float halfX = mapSize.x / 2f - edgeMargin;
        float halfY = mapSize.y / 2f - edgeMargin;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(mapCenter.x - halfX, mapCenter.x + halfX),
                Random.Range(mapCenter.y - halfY, mapCenter.y + halfY));

            if (IsValid(candidate, occupied))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector2.zero;
        return false;   // se agotaron los intentos: el mapa está muy lleno
    }

    private bool IsValid(Vector2 point, List<Vector2> occupied)
    {
        // Regla 1: no encima de un obstáculo
        if (Physics2D.OverlapCircle(point, obstacleClearRadius, obstacleMask) != null)
            return false;

        // Regla 2: no pegado al agente
        if (agent != null && Vector2.Distance(point, agent.position) < minDistanceFromAgent)
            return false;

        // Regla 3: no cerca de otros quesos
        foreach (Vector2 other in occupied)
            if (Vector2.Distance(point, other) < minDistanceBetweenCheeses)
                return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(mapCenter, mapSize);
    }
}
