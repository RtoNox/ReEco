using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
    void Die();
    bool IsAlive();
}

public interface ITargetable
{
    Transform GetTransform();
    Team GetTeam();
    bool IsTargetable();
}

public interface IHealable
{
    void Heal(int amount);
    bool CanBeHealed();
}

public interface IInteractable
{
    void Interact(GameObject interactor);
    string GetInteractionText();
    bool CanInteract();
}

public enum Team
{
    Player,
    Enemy,
    Neutral
}

public interface ILootable
{
    LootData GetLootData();
    void OnLootCollected();
}

public interface ICollectible
{
    void Collect(GameObject collector);
    int GetValue();
    LootTier GetLootTier();
    string GetItemName();
}
public enum LootType
{
    Common,
    Rare,
    Epic,
    Environment,
    EnemyDrop
}