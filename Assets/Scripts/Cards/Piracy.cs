using UnityEngine;

public class Piracy : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.extraText = "When you play a Troop here: +2 Coin.";
        this.artistText = "Claus Stephan\nDominion: Seaside\n(Pirate)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop && thisEntity.currentRow == playedEntity.currentRow)
            thisEntity.player.CoinRPC(2, logged, this.name);
    }
}
