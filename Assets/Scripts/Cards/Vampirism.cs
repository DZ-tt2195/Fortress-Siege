using UnityEngine;

public class Vampirism : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 4;
        this.extraText = "When one of your Troops here attacks the other player: You get +4 Health.";
    }

    public override void CardAttacked(Entity entity, MovingTroop attacker, Entity defender, int logged)
    {
        if (entity.player == attacker.player && defender.player != attacker.player)
            attacker.player.myBase.ChangeHealthRPC(4, logged, this.name);
    }
}
