using UnityEngine;

public class GrowShroom : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 1;
        this.abilityValue = 2;
        this.extraText = "When you play another Troop: It gets +1 Power and +1 Health.";
        Math();
    }

    public override void OtherCardPlayed(Player player, Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop troop)
            troop.ChangeStatsRPC(1, 1, logged, this.name);
    }
}
