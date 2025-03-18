using UnityEngine;

public class Bee : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 2;
        this.health = 2;
        this.extraText = "When this attacks another player's Troop: Stun them.";
        this.artistText = "Tom Wanerstrand\nMTG: Time Spiral\n(Unyaro Bees)";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker && defender is MovingTroop opposingTroop)
            opposingTroop.StatusEffectRPC(StatusEffect.Stunned, true, logged, this.name);
    }
}
