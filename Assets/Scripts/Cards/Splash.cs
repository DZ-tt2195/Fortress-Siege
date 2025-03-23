using UnityEngine;

public class Splash : AuraCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 3;
        this.extraText = "When you play a Troop here: All opposing Troops lose 1 Health.";
        //this.artistText = "Martin Hoffmann\nDominion: Nocturne\n(Vampire)";
    }

    public override void OtherCardPlayed(Entity thisEntity, Entity playedEntity, int logged)
    {
        if (thisEntity.player == playedEntity.player && playedEntity is MovingTroop && thisEntity.currentRow == playedEntity.currentRow)
        {
            foreach (Row row in Manager.inst.allRows)
            {
                MovingTroop troop = Manager.inst.FindOpposingTroop(playedEntity.player, row.position);
                if (troop != null)
                    troop.ChangeStatsRPC(0, -1, logged, this.name);
            }
        }
    }
}
