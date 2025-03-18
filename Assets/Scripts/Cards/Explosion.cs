using UnityEngine;

public class Explosion : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 1;
        this.extraText = "Start of combat: If both players have Troops here, Bounce them both.";
        this.artistText = "Tomasz Jedruszek\nMTG: War of the Spark\n(Casualties of War)";
    }

    public override void StartOfCombat(Entity entity, int logged)
    {
        Row row = Manager.inst.allRows[entity.currentRow];
        if (row.playerTroops[0] != null && row.playerTroops[1] != null)
        {
            Manager.inst.playersInOrder[0].BounceCardRPC(row.playerTroops[0], logged, this.name);
            Manager.inst.playersInOrder[1].BounceCardRPC(row.playerTroops[1], logged, this.name);
        }
    }
}
