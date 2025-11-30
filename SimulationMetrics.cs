using UnityEngine;

public class SimulationMetrics : MonoBehaviour
{
    public static SimulationMetrics instance;

    public float totalReward = 0f;
    public int curedPlants = 0;
    public int harvestedPlants = 0;
    public int timesRefilled = 0;
    public float totalDistance = 0f;
    public float simulationTime = 0f;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        simulationTime += Time.deltaTime;
    }

    public void AddReward(float r)
    {
        totalReward += r;
    }

    public void AddDistance(float d)
    {
        totalDistance += d;
    }

    public void OnCured() { curedPlants++; }
    public void OnHarvested() { harvestedPlants++; }
    public void OnRefilled() { timesRefilled++; }

    public void PrintMetrics()
    {
        Debug.Log("\nMETRICAS DE LA SIMULACION");
        Debug.Log($"Tiempo total de simulacion: {simulationTime:F1} segundos");
        Debug.Log($"Recompensa total acumulada: {totalReward}");
        Debug.Log($"Plantas curadas: {curedPlants}");
        Debug.Log($"Plantas cosechadas: {harvestedPlants}");
        Debug.Log($"Veces que recargo: {timesRefilled}");
        Debug.Log($"Distancia recorrida: {totalDistance:F1} unidades");
    }
}
