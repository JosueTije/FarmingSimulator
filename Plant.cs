using UnityEngine;

public enum PlantState { Healthy, Infected, Cured }

[RequireComponent(typeof(Collider))]
public class Plant : MonoBehaviour
{
    public PlantState state = PlantState.Healthy;
    public Renderer rend;
    public Color healthyColor = Color.green;
    public Color infectedColor = new Color(1f, 0.55f, 0f);
    public Color curedColor = new Color(0f, 0.4f, 0f);

    void Start()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        ApplyColor();
    }

    public void SetState(PlantState newState)
    {
        state = newState;
        ApplyColor();
    }

    void ApplyColor()
    {
        if (rend == null) return;

        if (state == PlantState.Healthy) rend.material.color = healthyColor;
        else if (state == PlantState.Infected) rend.material.color = infectedColor;
        else rend.material.color = curedColor;
    }

    public void OnHarvested()
    {
        Destroy(gameObject);
    }
}
