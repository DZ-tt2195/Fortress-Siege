using UnityEngine;

public class Bomb : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.power = 5;
        this.health = 1;
        this.extraText = "End of each round: This loses 1 Health.";
        this.artistText = "Thomas Tang\nAll Shapes and Sizes\n(Bomb)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        MovingTroop troop = (MovingTroop)entity;
        troop.ChangeStatsRPC(0, -1, logged, this.name);
    }
}
