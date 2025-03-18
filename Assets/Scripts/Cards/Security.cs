using UnityEngine;

public class Security : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "End of turn: If there are no opposing Troops here, +1 Card.";
        this.artistText = "Eric J. Carter\nDominion\n(Sentry)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        if (Manager.inst.FindOpposingTroop(entity.player, entity.currentRow) == null)
            entity.player.DrawCardRPC(1, logged, this.name);
    }
}
