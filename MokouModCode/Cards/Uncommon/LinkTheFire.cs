using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MokouMod.MokouModCode.Scripts;

namespace MokouMod.MokouModCode.Cards.Uncommon;

public class LinkTheFire : MokouModCard
{
    public LinkTheFire() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
        WithVar("RegenTransfer", 3, 1);
        WithPower<RegenPower>(2);
        WithKeyword(CardKeyword.Retain, UpgradeType.Add);
        WithKeywords(MokouModKeywords.Ember, CardKeyword.Exhaust);
        WithTip(new TooltipSource(card => new HoverTip(new LocString("cards", Id.Entry + ".extraTipTitle"), new LocString("cards", Id.Entry + ".extraTipDescription"))));
    }

    public override Character.MokouMod.Animation Anim => Character.MokouMod.Animation.SpellChannel;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. Gain Regen if Ember is active
        if (EmberActive)
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, DynamicVars["RegenPower"].IntValue, Owner.Creature, this);

        var maxTransfer = DynamicVars["RegenTransfer"].IntValue;
        int amountToApply;

        if (cardPlay.Target.Player?.Character.Id.ToString() == "CHARACTER.KEINEMOD-KEINE_MOD")
        {
            // Keine gets full Regen without draining it from Mokou
            amountToApply = maxTransfer;
        }
        else
        {
            // Calculate transfer amount (capped by Mokou's current Regen)
            amountToApply = Math.Min(Owner.Creature.GetPowerAmount<RegenPower>(), maxTransfer);

            // Deduct transferred Regen from Mokou
            if (amountToApply > 0 && Owner.Creature.GetPower<RegenPower>() is { } selfRegen)
                await PowerCmd.ModifyAmount(choiceContext, selfRegen, -amountToApply, Owner.Creature, this);
        }

        // 2. Play visual effect on target
        if (amountToApply > 0)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireBurningVfx.Create(cardPlay.Target, 0.75f, false));
            await PowerCmd.Apply<RegenPower>(choiceContext, cardPlay.Target, amountToApply, Owner.Creature, this);
        }
    }
}