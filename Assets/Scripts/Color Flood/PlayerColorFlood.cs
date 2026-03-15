using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerColorFlood : BoostPlayer
{
    public NetworkVariable<Team> teamNet = new NetworkVariable<Team>(
        Team.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private float speedBoostTimer;
    private const float SpeedBoostDuration = 5f;
    private const float SpeedBoostMultiplier = 2f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        teamNet.OnValueChanged += OnTeamChanged;
        if (teamNet.Value != Team.None) ApplyTeamTint(teamNet.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        teamNet.OnValueChanged -= OnTeamChanged;
    }

    private void Update()
    {
        if (ColorFloodGameState.Instance != null && ColorFloodGameState.Instance.gameState.Value != GameState.Running) return;
        if (!IsOwner) return;

        if (speedBoostTimer > 0) speedBoostTimer -= Time.deltaTime;
        var (joystickOffset, input) = ParseInput();
        var (pedalSpeed, pedalAnimationSpeed) = GetSmoothedPedalSpeed();

        if (joystickOffset.sqrMagnitude > 0.01f)
        {
            bool isBoosted = speedBoostTimer > 0;
            isSprintingNet.Value = isBoosted || pedalSpeed > sprintSpeedThreshold;
            isWalkingNet.Value = !isSprintingNet.Value;
            isShowingBoostParticlesNet.Value = isBoosted || pedalSpeed > sprintSpeedThreshold;

            float moveSpeed = walkSpeed * pedalSpeed;
            if (speedBoostTimer > 0) moveSpeed *= SpeedBoostMultiplier;
            HandleMovement(joystickOffset, moveSpeed, pedalSpeed, pedalAnimationSpeed, false);
        }
        else
        {
            bool isBoosted = speedBoostTimer > 0;
            isWalkingNet.Value = false;
            isSprintingNet.Value = isBoosted;
            isShowingBoostParticlesNet.Value = isBoosted;
            if (animator != null) animator.speed = 1f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        if (ColorFloodGameState.Instance == null || ColorFloodGameState.Instance.gameState.Value != GameState.Running) return;

        SpeedBoostPickup speedPickup = other.GetComponent<SpeedBoostPickup>();
        if (speedPickup != null)
        {
            PowerUpSpawner.Instance.CollectSpeedBoostServerRpc(speedPickup.pickupId);
            return;
        }

        PaintBombPickup bombPickup = other.GetComponent<PaintBombPickup>();
        if (bombPickup != null)
        {
            PowerUpSpawner.Instance.CollectPaintBombServerRpc(bombPickup.pickupId);
            return;
        }

        if (teamNet.Value == Team.None) return;

        ColorFloodTile tile = other.GetComponent<ColorFloodTile>();
        if (tile == null) return;
        TileGrid.Instance.PaintTileServerRpc(tile.tileIndex, teamNet.Value);
    }

    [ClientRpc]
    public void GrantSpeedBoostClientRpc()
    {
        if (IsOwner) speedBoostTimer = SpeedBoostDuration;
    }

    public override void OnSkinColorChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        base.OnSkinColorChanged(previousValue, newValue);
        if (teamNet.Value != Team.None) ApplyTeamTint(teamNet.Value);
    }

    private void OnTeamChanged(Team previousValue, Team newValue)
    {
        ApplyTeamTint(newValue);
    }

    private void ApplyTeamTint(Team team)
    {
        if (PlayerSkinRenderer == null) return;
        Color tint = team switch
        {
            Team.Green => new Color(0.5f, 1f, 0.5f), // TODO : use default color values
            Team.Blue => new Color(0.5f, 0.7f, 1f),
            _ => Color.white,
        };
        PlayerSkinRenderer.material.color = tint;
    }

    [ClientRpc]
    public void TeleportClientRpc(Vector3 position, Team team)
    {
        rb.position = position;
        rb.rotation = Quaternion.Euler(0f, team == Team.Green ? 90f : -90f, 0f);
    }

    public override PlayerData GetPlayerData()
    {
        int tilesOwned = teamNet.Value == Team.Green
            ? ColorFloodGameState.Instance.greenTileCount.Value
            : ColorFloodGameState.Instance.blueTileCount.Value;

        return new PlayerData(
            guidNet.Value,
            nicknameNet.Value,
            colorNet.Value,
            tilesOwned,
            teamNet.Value == Team.Green ? 0f : 1f
        );
    }
}
