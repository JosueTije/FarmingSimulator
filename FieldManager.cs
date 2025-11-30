using System.Collections.Generic;
using UnityEngine;

public class FieldManager : MonoBehaviour
{
    public int rows = 9;
    public int cols = 9;
    public float rowSpacing = 5f;
    public float colSpacing = 5f;

    public GameObject plantPrefab;
    public float infectedPercent = 0.4f;
    public Transform fieldOrigin;

    public GameObject tractorPrefab;
    public Transform[] tractorStarts;
    public Transform barnTransform;

    List<Plant> plants = new List<Plant>();
    bool simulationEnded = false;

    void Start()
    {
        SpawnField();
        SpawnTractors();
    }

    void Update()
    {
        if (!simulationEnded && NoWorkLeft())
        {
            simulationEnded = true;
            Debug.Log("SIMULACION TERMINADA (no quedan plantas por curar ni cosechar).");
            if (SimulationMetrics.instance != null)
                SimulationMetrics.instance.PrintMetrics();

            // opcional: pausar simulación
            Time.timeScale = 0f;
        }
    }

    // ======= SPAWN DE PLANTAS =======
    void SpawnField()
    {
        Vector3 origin = fieldOrigin ? fieldOrigin.position : Vector3.zero;
        float totalW = (cols - 1) * colSpacing;
        float totalH = (rows - 1) * rowSpacing;
        Vector3 start = origin - new Vector3(totalW / 2f, 0f, totalH / 2f);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = start + new Vector3(c * colSpacing, 1f, r * rowSpacing);
                GameObject go = Instantiate(plantPrefab, pos, Quaternion.identity, transform);
                Plant pl = go.GetComponent<Plant>();
                plants.Add(pl);
            }
        }

        int total = plants.Count;
        int infectNum = Mathf.Max(1, Mathf.RoundToInt(total * infectedPercent));

        for (int i = 0; i < infectNum; i++)
        {
            int idx = Random.Range(0, plants.Count);
            if (plants[idx] != null)
                plants[idx].SetState(PlantState.Infected);
        }
    }

    // ======= SPAWN DE TRACTORES =======
    void SpawnTractors()
    {
        if (tractorPrefab == null) return;

        for (int i = 0; i < tractorStarts.Length; i++)
        {
            Transform st = tractorStarts[i];
            GameObject t = Instantiate(tractorPrefab, st.position, st.rotation);
            TractorAI ai = t.GetComponent<TractorAI>();
            ai.barn = barnTransform;
            ai.role = (i < 2) ? TractorRole.Herbicide : TractorRole.Harvester;
        }
    }

    // ======= TARGET MÁS CERCANO PARA CADA ROL =======
    public Plant GetClosestValidPlant(Vector3 pos, TractorRole role)
    {
        Plant best = null;
        float bestDist = Mathf.Infinity;

        // limpiar nulos
        for (int i = plants.Count - 1; i >= 0; i--)
        {
            if (plants[i] == null)
                plants.RemoveAt(i);
        }

        foreach (var p in plants)
        {
            if (p == null) continue;

            bool valid =
                (role == TractorRole.Herbicide && p.state == PlantState.Infected) ||
                (role == TractorRole.Harvester && (p.state == PlantState.Healthy || p.state == PlantState.Cured));

            if (!valid) continue;

            float d = (p.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }

        return best;
    }

    // ======= CONDICIÓN DE FIN DE SIMULACIÓN =======
    bool NoWorkLeft()
    {
        // No queda ninguna planta infectada, healthy o cured
        foreach (var p in plants)
        {
            if (p == null) continue;

            if (p.state == PlantState.Infected) return false;
            if (p.state == PlantState.Healthy || p.state == PlantState.Cured) return false;
        }
        return true;
    }
}
