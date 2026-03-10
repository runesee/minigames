using Unity.Netcode;
using UnityEngine;

public class TileGrid : NetworkBehaviour
{
    public static TileGrid Instance { get; private set; }

    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float tileSize = 1f;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float TileSize => tileSize;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material neutralMaterial;

    private ColorFloodTile[] tiles;

    private ColorFloodGameState.Team[] tileOwnership;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        tiles = new ColorFloodTile[gridWidth * gridHeight];
        tileOwnership = new ColorFloodGameState.Team[gridWidth * gridHeight];

        float gridOriginX = -(gridWidth * tileSize) / 2f + tileSize / 2f;
        float gridOriginZ = -(gridHeight * tileSize) / 2f + tileSize / 2f;

        for (int row = 0; row < gridHeight; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                int index = row * gridWidth + col;
                Vector3 position = new Vector3(
                    gridOriginX + col * tileSize,
                    0f,
                    gridOriginZ + row * tileSize
                );

                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tileObject.name = $"Tile_{index}";

                ColorFloodTile tile = tileObject.GetComponent<ColorFloodTile>();
                tile.tileIndex = index;
                tile.SetColor(ColorFloodGameState.Team.None, greenMaterial, blueMaterial, neutralMaterial);

                tiles[index] = tile;
                tileOwnership[index] = ColorFloodGameState.Team.None;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PaintTileServerRpc(int tileIndex, ColorFloodGameState.Team team)
    {
        if (tileIndex < 0 || tileIndex >= tileOwnership.Length) return;
        if (tileOwnership[tileIndex] == team) return;

        ColorFloodGameState.Team previousOwner = tileOwnership[tileIndex];
        tileOwnership[tileIndex] = team;

        if (previousOwner == ColorFloodGameState.Team.Green)
            ColorFloodGameState.Instance.greenTileCount.Value--;
        else if (previousOwner == ColorFloodGameState.Team.Blue)
            ColorFloodGameState.Instance.blueTileCount.Value--;

        if (team == ColorFloodGameState.Team.Green)
            ColorFloodGameState.Instance.greenTileCount.Value++;
        else if (team == ColorFloodGameState.Team.Blue)
            ColorFloodGameState.Instance.blueTileCount.Value++;

        UpdateTileColorClientRpc(tileIndex, team);
    }

    [ClientRpc]
    private void UpdateTileColorClientRpc(int tileIndex, ColorFloodGameState.Team team)
    {
        if (tiles == null || tileIndex < 0 || tileIndex >= tiles.Length) return;
        tiles[tileIndex].SetColor(team, greenMaterial, blueMaterial, neutralMaterial);
    }
}
