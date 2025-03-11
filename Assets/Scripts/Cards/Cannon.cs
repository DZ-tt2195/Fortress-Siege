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
        this.artistText = "Julian Kok Joon Wen\nMTG: Dominaria United\n(Artillery Blast)";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            attacker.StatusEffectRPC(StatusEffect.Stunned, true, logged, attacker.name);
    }
}
