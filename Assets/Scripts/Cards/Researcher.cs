using UnityEngine;

public class Researcher : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 4;
        this.health = 3;
        this.extraText = "End of turn: The other player gets +1 Card.";
        this.artistText = "Lie Setiawan\nMTG: Strixhaven: School of Mages\n(Solve the Equation)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        Manager.inst.OpposingPlayer(entity.player).DrawCardRPC(1, logged, this.name);
    }
}
