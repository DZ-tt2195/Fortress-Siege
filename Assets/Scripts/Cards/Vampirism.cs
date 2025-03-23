using UnityEngine;

public class Vampirism : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "When your Troop here attacks the other player: You get +1 Health per Power it has.";
        this.artistText = "Martin Hoffmann\nDominion: Nocturne\n(Vampire)";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity.player == attacker.player && entity.currentRow == attacker.currentRow && defender is PlayerBase)
            attacker.player.myBase.ChangeHealthRPC(attacker.calcPower, logged, this.name);
    }
}
