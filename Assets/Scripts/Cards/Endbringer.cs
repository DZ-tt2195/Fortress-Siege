using UnityEngine;

public class Endbringer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 6;
        this.power = 5;
        this.health = 6;
        this.extraText = "End of turn: Both players get +1 Card and -2 Health.";
        this.artistText = "";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        foreach (Player player in Manager.inst.playersInOrder)
        {
            player.DrawCardRPC(1, logged, this.name);
            player.myBase.ChangeHealthRPC(-2, logged, this.name);
        }    
    }
}
