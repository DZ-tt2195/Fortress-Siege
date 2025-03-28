using UnityEngine;

public class Wizard : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 5;
        this.power = 3;
        this.health = 3;
        this.extraText = "When any player plays an Aura: This does an attack.";
        this.artistText = "Harald Lieske\nDominion: Allies\n(Conjurer)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (playedEntity is MovingAura)
        {
            MovingTroop troop = (MovingTroop)thisEntity;
            troop.Attack(true, logged);
        }
    }
}
