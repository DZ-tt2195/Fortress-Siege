using UnityEngine;

public class Witch : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 2;
        this.health = 2;
        this.extraText = "When any player plays an Aura: You get +1 Health.";
        this.artistText = "Hans Krill\nDominion: Allies\n(Sorceress)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (playedEntity is MovingAura)
            thisEntity.player.myBase.ChangeHealthRPC(1, logged, this.name);
    }
}
