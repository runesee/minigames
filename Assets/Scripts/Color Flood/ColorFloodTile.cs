using UnityEngine;

public class ColorFloodTile : MonoBehaviour
{
    public int tileIndex;
    public Team ownerTeam = Team.None;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetColor(Team team, Material greenMaterial, Material blueMaterial, Material neutralMaterial)
    {
        ownerTeam = team;
        meshRenderer.material = team switch
        {
            Team.Green => greenMaterial,
            Team.Blue => blueMaterial,
            _ => neutralMaterial,
        };
    }
}
