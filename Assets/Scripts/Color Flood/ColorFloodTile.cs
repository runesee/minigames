using UnityEngine;

public class ColorFloodTile : MonoBehaviour
{
    public int tileIndex;
    public ColorFloodGameState.Team ownerTeam = ColorFloodGameState.Team.None;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetColor(ColorFloodGameState.Team team, Material greenMaterial, Material blueMaterial, Material neutralMaterial)
    {
        ownerTeam = team;
        meshRenderer.material = team switch
        {
            ColorFloodGameState.Team.Green => greenMaterial,
            ColorFloodGameState.Team.Blue => blueMaterial,
            _ => neutralMaterial,
        };
    }
}
