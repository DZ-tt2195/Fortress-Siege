using UnityEngine;

public class KiteFlyer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 1;
        this.health = 3;
        this.abilityValue = 2;
        this.extraText = "When this attacks: +1 Card.";
        Math();
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            attacker.player.DrawCardRPC(1, logged);
    }
}
