using UnityEngine;

public class Treasurer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.power = 6;
        this.health = 1;
        this.extraText = "End of turn: Deal 1 damage to the opposing player per unused Coin you have.";
        this.artistText = "Claus Stephan\nDominion: Renaissance\n(Treasurer)";
    }

    public override void EndOfTurn(Entity entity, int logged)
    {
        Manager.inst.OpposingPlayer(entity.player).myBase.ChangeHealthRPC(-1 * entity.player.coins, logged, this.name);
    }
}
