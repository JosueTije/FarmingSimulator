using System.Collections;
using UnityEngine;

public enum TractorRole { Herbicide, Harvester }

public class TractorAI : MonoBehaviour
{
    public TractorRole role = TractorRole.Herbicide;
    public Transform barn;

    public float herbicide = 10f;
    public float herbicideMax = 10f;
    public float fuel = 100f;
    public float fuelMax = 100f;

    public float workTime = 1.0f;
    public float stoppingDistance = 0.8f;

    public float moveSpeed = 3f;     // avanza por transform.right
    public float turnSpeed = 120f;   // grados/s

    FieldManager manager;
    Plant currentTarget;
    Coroutine workCoroutine;
    bool isWorking = false;

    float barnRadius = 20f;

    // ==== Q-LEARNING ====
    public bool useQLearning = true;
    public float decisionInterval = 1.0f;

    QLearningAgent qAgent;
    int lastState = 0;
    int lastAction = 2; // por default: ir al barn
    float decisionTimer = 0f;
    float accumulatedReward = 0f;

    void Start()
    {
        manager = FindObjectOfType<FieldManager>();

        if (useQLearning)
            qAgent = new QLearningAgent(8, 3); // 8 estados, 3 acciones
    }

    void Update()
    {
        if (isWorking) return;

        // Si está en el barn → recarga
        if (IsAtBarn())
        {
            fuel = fuelMax;
            if (role == TractorRole.Herbicide)
                herbicide = herbicideMax;

            if (SimulationMetrics.instance != null)
            {
                SimulationMetrics.instance.OnRefilled();
                SimulationMetrics.instance.AddReward(2f);
            }
            accumulatedReward += 2f;

            currentTarget = null;
            return;
        }

        // Si ya casi no trae gas/herbicida → prioridad absoluta: barn
        if (NeedsRefill())
        {
            currentTarget = null;
            MoveCarLike(barn.position);
            return;
        }

        // ====== Q-LEARNING LOOP ======
        if (useQLearning && qAgent != null)
        {
            decisionTimer += Time.deltaTime;
            // castigo por tiempo (no hacer nada es malo)
            accumulatedReward -= Time.deltaTime * 0.01f;

            if (decisionTimer >= decisionInterval)
            {
                int s = GetCurrentState();
                qAgent.UpdateQ(lastState, lastAction, accumulatedReward, s);

                accumulatedReward = 0f;
                decisionTimer = 0f;

                int a = qAgent.ChooseAction(s);
                lastState = s;
                lastAction = a;

                ChooseTargetFromAction(a);
            }
        }
        else
        {
            // Fallback clásico: si no hay RL, usar greedy normal
            if (currentTarget == null)
                currentTarget = manager.GetClosestValidPlant(transform.position, role);
        }

        // Si la acción elegida fue ir al barn
        if (currentTarget == null && lastAction == 2 && barn != null)
        {
            MoveCarLike(barn.position);
            return;
        }

        // Si no hay target (y no estamos yendo al barn), buscar uno por rol
        if (currentTarget == null)
        {
            currentTarget = manager.GetClosestValidPlant(transform.position, role);
            if (currentTarget == null)
            {
                // nada que hacer → moverse un poco hacia el barn
                if (barn != null) MoveCarLike(barn.position);
                return;
            }
        }

        // Movimiento hacia el objetivo
        MoveCarLike(currentTarget.transform.position);

        float dist = DistanceFlat(transform.position, currentTarget.transform.position);
        if (dist <= stoppingDistance)
        {
            if (workCoroutine == null)
            {
                isWorking = true;
                workCoroutine = StartCoroutine(DoWorkOnPlant(currentTarget));
            }
        }
    }

    // ===================== ESTADO PARA Q-LEARNING =====================

    int GetCurrentState()
    {
        // Estado con 3 bits:
        // bit 2 (4): hay infectadas alcanzables
        // bit 1 (2): hay cosechables alcanzables
        // bit 0 (1): necesita recargar
        int s = 0;

        Plant nearestInfected = manager.GetClosestValidPlant(transform.position, TractorRole.Herbicide);
        Plant nearestHarvest  = manager.GetClosestValidPlant(transform.position, TractorRole.Harvester);

        if (nearestInfected != null) s |= 1 << 2; // 4
        if (nearestHarvest  != null) s |= 1 << 1; // 2;
        if (NeedsRefill())           s |= 1 << 0; // 1

        return s;
    }

    void ChooseTargetFromAction(int action)
    {
        switch (action)
        {
            case 0: // ir a infectada
                currentTarget = manager.GetClosestValidPlant(transform.position, TractorRole.Herbicide);
                break;
            case 1: // ir a cosechable
                currentTarget = manager.GetClosestValidPlant(transform.position, TractorRole.Harvester);
                break;
            case 2: // ir al barn
                currentTarget = null; // usamos barn.position en Update
                break;
        }
    }

    // ===================== LÓGICA BÁSICA =====================

    bool NeedsRefill()
    {
        // margen de seguridad de fuel
        if (role == TractorRole.Herbicide)
            return fuel <= 5f || herbicide <= 0f;

        return fuel <= 5f;
    }

    bool IsAtBarn()
    {
        if (barn == null) return false;
        return Vector3.Distance(transform.position, barn.position) < barnRadius;
    }

    float DistanceFlat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void MoveCarLike(Vector3 targetPos)
    {
        if (fuel <= 0f) return;

        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        Vector3 forward = transform.right;
        float angle = Vector3.SignedAngle(forward, dir.normalized, Vector3.up);

        // Girar
        if (Mathf.Abs(angle) > 5f)
        {
            float turn = Mathf.Sign(angle) * turnSpeed * Time.deltaTime;
            if (Mathf.Abs(turn) > Mathf.Abs(angle)) turn = angle;
            transform.Rotate(0f, turn, 0f);
            return;
        }

        // Avanzar
        Vector3 move = transform.right * moveSpeed * Time.deltaTime;
        transform.position += move;

        if (SimulationMetrics.instance != null)
            SimulationMetrics.instance.AddDistance(move.magnitude);

        fuel -= Time.deltaTime * 0.5f;
        if (fuel < 0f) fuel = 0f;
    }

    // ===================== TRABAJO EN PLANTA =====================

    IEnumerator DoWorkOnPlant(Plant p)
    {
        if (p == null)
        {
            isWorking = false;
            currentTarget = null;
            workCoroutine = null;
            yield break;
        }

        float t = 0f;
        while (t < workTime)
        {
            if (p == null)
            {
                isWorking = false;
                currentTarget = null;
                workCoroutine = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }

        bool success = false;

        if (role == TractorRole.Herbicide && p.state == PlantState.Infected)
        {
            p.SetState(PlantState.Cured);
            herbicide = Mathf.Max(0f, herbicide - 1f);

            if (SimulationMetrics.instance != null)
            {
                SimulationMetrics.instance.OnCured();
                SimulationMetrics.instance.AddReward(10f);
            }
            accumulatedReward += 10f;
            success = true;
        }
        else if (role == TractorRole.Harvester &&
                (p.state == PlantState.Healthy || p.state == PlantState.Cured))
        {
            p.OnHarvested();

            if (SimulationMetrics.instance != null)
            {
                SimulationMetrics.instance.OnHarvested();
                SimulationMetrics.instance.AddReward(10f);
            }
            accumulatedReward += 10f;
            success = true;
        }
        else
        {
            // target inválido
            if (SimulationMetrics.instance != null)
                SimulationMetrics.instance.AddReward(-2f);
            accumulatedReward -= 2f;
        }

        fuel = Mathf.Max(0f, fuel - 2f);

        isWorking = false;
        currentTarget = null;
        workCoroutine = null;
    }
}
