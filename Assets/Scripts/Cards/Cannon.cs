using UnityEngine;

public class Cannon : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 3;
        this.health = 6;
        this.extraText = "When this attacks: It gets Stunned.";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            attacker.StunStatusRPC(true, logged, attacker.name);
    }
}
