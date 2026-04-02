using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

[RequireComponent(typeof(GameManager))]
public class GameWorldState : MonoBehaviour
{
    private readonly HashSet<string> collectedPickupIds = new();
    private readonly HashSet<EnemyMovement> registeredEnemies = new();
    private readonly HashSet<DoorInteract> registeredDoors = new();
    private readonly HashSet<PickUpScript> registeredPickups = new();

    public void RegisterEnemy(EnemyMovement enemy)
    {
        if (enemy != null)
            registeredEnemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyMovement enemy)
    {
        if (enemy != null)
            registeredEnemies.Remove(enemy);
    }

    public void RegisterDoor(DoorInteract door)
    {
        if (door != null)
            registeredDoors.Add(door);
    }

    public void UnregisterDoor(DoorInteract door)
    {
        if (door != null)
            registeredDoors.Remove(door);
    }

    public void RegisterPickup(PickUpScript pickup)
    {
        if (pickup != null)
            registeredPickups.Add(pickup);
    }

    public void UnregisterPickup(PickUpScript pickup)
    {
        if (pickup != null)
            registeredPickups.Remove(pickup);
    }

    public void MarkPickupCollected(string pickupId)
    {
        if (!string.IsNullOrWhiteSpace(pickupId))
            collectedPickupIds.Add(pickupId);
    }

    public bool IsPickupCollected(string pickupId)
    {
        return !string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId);
    }

    public void ClearCollectedPickups()
    {
        collectedPickupIds.Clear();
    }

    public List<string> GetCollectedPickupIds()
    {
        return new List<string>(collectedPickupIds);
    }

    public void SetCollectedPickupIds(List<string> ids)
    {
        collectedPickupIds.Clear();

        if (ids == null)
            return;

        for (int i = 0; i < ids.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i]))
                collectedPickupIds.Add(ids[i]);
        }
    }

    public void FillWorldState(SaveData data)
    {
        if (data == null)
            return;

        CleanupRegistries();

        data.collectedPickups = GetCollectedPickupIds();

        foreach (EnemyMovement enemy in registeredEnemies)
        {
            if (enemy == null)
                continue;

            data.enemies.Add(new EnemyState
            {
                id = enemy.gameObject.name,
                pos = enemy.transform.position,
                rot = enemy.transform.rotation,
                chasingPlayer = enemy.IsChasingPlayer,
                returning = enemy.IsReturning,
                waypointIndex = enemy.CurrentWaypointIndex
            });
        }

        foreach (DoorInteract door in registeredDoors)
        {
            if (door == null)
                continue;

            data.doors.Add(new DoorState
            {
                id = door.DoorId,
                unlocked = door.IsUnlocked,
                open = door.IsOpen
            });
        }
    }

    public void ApplyWorldState(SaveData data)
    {
        if (data == null)
            return;

        SetCollectedPickupIds(data.collectedPickups);
        RestoreEnemies(data);
        RestoreDoors(data);
        ApplyCollectedPickupsInScene();
    }

    private void RestoreEnemies(SaveData data)
    {
        if (data.enemies == null)
            return;

        CleanupRegistries();

        Dictionary<string, EnemyState> map = new Dictionary<string, EnemyState>();
        for (int i = 0; i < data.enemies.Count; i++)
        {
            EnemyState state = data.enemies[i];
            if (state != null && !string.IsNullOrWhiteSpace(state.id))
                map[state.id] = state;
        }

        foreach (EnemyMovement enemy in registeredEnemies)
        {
            if (enemy == null)
                continue;

            if (map.TryGetValue(enemy.gameObject.name, out EnemyState state))
            {
                enemy.transform.SetPositionAndRotation(state.pos, state.rot);
                enemy.ApplySavedAIState(state.chasingPlayer, state.returning, state.waypointIndex);
            }
        }
    }

    private void RestoreDoors(SaveData data)
    {
        if (data.doors == null)
            return;

        CleanupRegistries();

        Dictionary<string, DoorState> map = new Dictionary<string, DoorState>();
        for (int i = 0; i < data.doors.Count; i++)
        {
            DoorState state = data.doors[i];
            if (state != null && !string.IsNullOrWhiteSpace(state.id))
                map[state.id] = state;
        }

        foreach (DoorInteract door in registeredDoors)
        {
            if (door == null)
                continue;

            if (map.TryGetValue(door.DoorId, out DoorState state))
                door.ApplySavedState(state.unlocked, state.open);
        }
    }

    private void ApplyCollectedPickupsInScene()
    {
        CleanupRegistries();

        foreach (PickUpScript pickup in registeredPickups)
        {
            if (pickup == null)
                continue;

            if (!string.IsNullOrWhiteSpace(pickup.PickupId) && collectedPickupIds.Contains(pickup.PickupId))
                pickup.gameObject.SetActive(false);
        }
    }

    private void CleanupRegistries()
    {
        registeredEnemies.RemoveWhere(item => item == null);
        registeredDoors.RemoveWhere(item => item == null);
        registeredPickups.RemoveWhere(item => item == null);
    }
}