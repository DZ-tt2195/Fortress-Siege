using UnityEngine;

public class Knight : TroopCard
{
    protected override void Awake()
    {
        base.Awake();
        this.bottomType = this.GetType();
        this.coinCost = 6;
        this.power = 3;
        this.health = 2;
        this.extraText = "When you play this: All your Troops become Shielded.";
        this.artistText = "Michael Watson\nDominion: Cornucopia & Guilds\n(Joust)";
    }

    public override void DonePlaying(Player player, Entity createdEntity, int logged)
    {
        foreach (Row row in Manager.inst.allRows)
        {
            MovingTroop troop = row.playerTroops[player.playerPosition];
            if (troop != null && !troop.statusDict[StatusEffect.Shielded])
                troop.StatusEffectRPC(StatusEffect.Shielded, true, logged);
        }
        base.DonePlaying(player, createdEntity, logged);
    }
}
