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
        this.extraText = "When you play another Troop: It gets +1 Power and +1 Health.";
    }

    public override void OtherCardPlayed(Player player, Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop newTroop && thisEntity != playedEntity)
            newTroop.ChangeStatsRPC(1, 1, logged, this.name);
    }
}
