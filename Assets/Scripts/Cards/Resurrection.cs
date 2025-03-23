using UnityEngine;

public class Resurrection : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "When your Troop here is destroyed, return it to your hand.";
        this.artistText = "";
    }

    public override void WhenDestroy(Entity thisEntity, Entity destroyedEntity, int originalRow, int logged)
    {
        if (thisEntity.player == destroyedEntity.player && thisEntity.currentRow == originalRow && destroyedEntity is MovingTroop)
            thisEntity.player.BounceCardRPC(destroyedEntity, logged);
    }
}
