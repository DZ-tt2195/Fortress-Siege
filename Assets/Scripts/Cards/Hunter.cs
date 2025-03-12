using UnityEngine;

public class Hunter : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.power = 2;
        this.health = 2;
        this.extraText = "When the other player plays a Troop: This moves to that row (if possible).";
        this.artistText = "Franz Vohwinkel\nDominion: Menagerie\n(Bounty Hunter)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player != playedEntity.player && playedEntity is MovingTroop newTroop)
        {
            if (Manager.inst.FindMyTroop(thisEntity.player, newTroop.currentRow) == null)
                thisEntity.MoveEntityRPC(newTroop.currentRow, logged);
        }
    }
}
