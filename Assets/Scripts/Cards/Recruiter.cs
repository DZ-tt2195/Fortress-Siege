using UnityEngine;

public class Recruiter : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 1;
        this.health = 2;
        this.extraText = "When this attacks: +1 Card.";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            attacker.player.DrawCardRPC(1, logged, this.name);
    }
}
