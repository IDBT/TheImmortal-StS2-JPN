using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MokouMod.MokouModCode.Enchantments;
using MokouMod.MokouModCode.Powers;

namespace MokouMod.MokouModCode.Cards.Rare;

public class ValiantHeart : MokouModCard
{
    public ValiantHeart() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
        WithCards(2);
        WithTip(typeof(VigorousEnchantment));
        WithTip(typeof(VigorPower));
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
    }

    protected override async Task OnPlayMokou(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.ApplySelf<ValiantHeartPower>(this, 1M);
        var enchantment = ModelDb.Enchantment<VigorousEnchantment>().ToMutable();
        var cards = (await CardSelectCmd.FromHand(choiceContext, Owner, new CardSelectorPrefs(new LocString("card_selection", "TO_ENCHANT_VIGOROUS"), DynamicVars.Cards.IntValue), (Func<CardModel, bool>)(model => enchantment.CanEnchant(model)), this)).ToList();
        if (cards.Count != 0)
            foreach (var card in cards)
            {
                var newEnchant = ModelDb.GetById<EnchantmentModel>(enchantment.Id).ToMutable();
                Enchant(newEnchant, card);
            }
    }
}