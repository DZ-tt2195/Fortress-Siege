using UnityEngine;

public class GrowShroom : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 2;
        this.health = 2;
        this.abilityValue = 4;
        this.extraText = "When you play another troop: It gets +2 Power +2 Health.";
        Math();
    }

    public override void OtherCardPlayed(Player player, Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop troop)
            troop.ChangeStatsRPC(2, 2, logged);
    }
}
