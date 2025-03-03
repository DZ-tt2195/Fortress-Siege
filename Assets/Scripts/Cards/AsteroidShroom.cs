using UnityEngine;

public class AsteroidShroom : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 1;
        this.health = 3;
        this.extraText = "When you play another Troop: Deal 1 damage to the other player.";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop && thisEntity != playedEntity)
            Manager.inst.OpposingPlayer(thisEntity.player).myBase.ChangeHealthRPC(-1, logged, this.name);
    }
}
