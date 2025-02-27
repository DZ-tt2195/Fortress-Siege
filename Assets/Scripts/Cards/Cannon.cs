using UnityEngine;

public class Cannon : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 3;
        this.health = 7;
        this.abilityValue = -3;
        this.extraText = "When this attacks: It gets Stunned.";
        Math();
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            attacker.StunStatusRPC(true, logged, attacker.name);
    }
}
