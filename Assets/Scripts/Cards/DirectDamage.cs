using UnityEngine;

public class DirectDamage : EnviroCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 2;
        this.abilityValue = 3;
        this.extraText = "Start of combat: If you have a Troop here, deal 3 damage to the other player.";
        Math();
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        Player opposingPlayer = Manager.inst.OpposingPlayer(entity.player);
        if (Manager.inst.allRows[entity.currentRow].playerTroops[entity.player.playerPosition] != null)
            opposingPlayer.myBase.ChangeHealthRPC(-3, logged, this.name);
    }
}
