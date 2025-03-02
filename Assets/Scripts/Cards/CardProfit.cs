using UnityEngine;

public class CardProfit : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "End of turn: If there are no opposing Troops here, +1 Card.";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        if (Manager.inst.FindOpposingTroop(entity.player, entity.currentRow) == null)
            entity.player.DrawCardRPC(1, logged, this.name);
    }
}
