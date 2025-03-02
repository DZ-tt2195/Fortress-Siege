using UnityEngine;

public class PeaPod : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 1;
        this.health = 1;
        this.extraText = "When this attacks the other player: This gets +1 Power and +1 Health.";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker && attacker.player != defender.player)
            attacker.ChangeStatsRPC(1, 1, logged, this.name);
    }
}
