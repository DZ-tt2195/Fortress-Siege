using UnityEngine;

public class Blacksmith : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 1;
        this.extraText = "When you play another Troop: It gets +2 Health.";
        this.artistText = "Julien Delval\nDominion: Allies\n(Blacksmith)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop newTroop && thisEntity != playedEntity)
            newTroop.ChangeStatsRPC(0, 2, logged, this.name);
    }
}
