using UnityEngine;

public class Hex : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "Start of combat: If you have a Troop here, deal 3 damage to the other player.";
        this.artistText = "Julien Delval\nDominion: Nocturne\n(Plague)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        if (Manager.inst.FindMyTroop(entity.player, entity.currentRow) != null)
            Manager.inst.OpposingPlayer(entity.player).myBase.ChangeHealthRPC(-3, logged, this.name);
    }
}
