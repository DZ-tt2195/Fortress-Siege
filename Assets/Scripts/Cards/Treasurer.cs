using UnityEngine;

public class Treasurer : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.power = 2;
        this.health = 4;
        this.extraText = "When this attacks: The other player loses 1 Health unused Coin you have.";
        this.artistText = "Claus Stephan\nDominion: Renaissance\n(Treasurer)";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity == attacker)
            Manager.inst.OpposingPlayer(entity.player).myBase.ChangeHealthRPC(-1 * entity.player.coins, logged, this.name);
    }
}
