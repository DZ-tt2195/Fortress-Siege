using UnityEngine;

public class Vampirism : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "When one of your Troops here attacks the other player: You get +4 Health.";
        this.artistText = "Martin Hoffmann\nDominion: Nocturne\n(Vampire)";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity.player == attacker.player && entity.currentRow == attacker.currentRow && defender is PlayerBase enemyBase)
            enemyBase.ChangeHealthRPC(4, logged, this.name);
    }
}
