using UnityEngine;

public class Catapult : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 1;
        this.health = 3;
        this.extraText = "When you play another Troop: The other player loses 1 Health.";
        this.artistText = "Matthias Catrein\nDominion: Empires\n(Catapult)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop && thisEntity != playedEntity)
            Manager.inst.OpposingPlayer(thisEntity.player).myBase.ChangeHealthRPC(-1, logged, this.name);
    }
}
