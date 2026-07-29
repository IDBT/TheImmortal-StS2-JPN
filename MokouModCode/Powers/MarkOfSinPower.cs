using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MokouMod.MokouModCode.Potions;

namespace MokouMod.MokouModCode.Powers;

public class MarkOfSinPower : MokouModPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Applier")];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPotion<HouraiElixir>()];

    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override Decimal ModifyDamageMultiplicative(Creature? target, Decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || !props.IsPoweredAttack() || target.Monster == null || dealer != Applier)
            return 1M;
        return target.Monster.IntendsToAttack ? 1.5M : 2M;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ((StringVar)DynamicVars["Applier"]).StringValue = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, Applier.Player.NetId);
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Owner && Applier.Player is { RunState.CurrentRoom.RoomType: RoomType.Boss } && !Owner.HasPower<MinionPower>())
            await PotionCmd.TryToProcure(ModelDb.Potion<HouraiElixir>().ToMutable(), Applier.Player);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;
        await PowerCmd.TickDownDuration(this);
    }
}