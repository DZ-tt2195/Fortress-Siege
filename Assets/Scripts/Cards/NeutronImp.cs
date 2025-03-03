using UnityEngine;

public class NeutronImp : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 3;
        this.health = 2;
        this.extraText = "When any player plays an Environment: This does an attack.";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (playedEntity is Environment)
        {
            MovingTroop troop = (MovingTroop)thisEntity;
            troop.Attack(logged);
        }
    }
}
