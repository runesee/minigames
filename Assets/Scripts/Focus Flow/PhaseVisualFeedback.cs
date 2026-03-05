using UnityEngine;

public class PhaseVisualFeedback : MonoBehaviour
{
    [SerializeField] private MeshRenderer mainCircleRenderer;

    private void Start()
    {
        mainCircleRenderer.material.color = Color.grey;
    }
}
