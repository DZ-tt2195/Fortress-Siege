using UnityEngine;

public class Rats : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 1;
        this.health = 1;
        this.extraText = "End of turn: This gets +1 Power and +1 Health.";
        this.artistText = "Ian Kirkpatrick\nDominion: Dark Ages\n(Rats)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        MovingTroop troop = (MovingTroop)entity;
        troop.ChangeStatsRPC(1, 1, logged, this.name);
    }
}
