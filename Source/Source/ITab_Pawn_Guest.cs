using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    public class ITab_Pawn_Guest : ITab_Pawn_Visitor
    {
        private static readonly string txtRecruitmentPenalty = "RecruitmentPenalty";
        private static readonly string txtFactionGoodwill = "FactionGoodwill".Translate();
        private static readonly string txtHospitality = "Hospitality".Translate();
        private static readonly string txtMakeDefault = "MakeDefault".Translate();
        private static readonly string txtSendAway = "SendAway".Translate();
        private static readonly string txtSendAwayQuestion = "SendAwayQuestion";
        private static readonly string txtMakeDefaultTooltip = "MakeDefaultTooltip".Translate();
        private static readonly string txtSendAwayTooltip = "SendAwayTooltip".Translate();
        internal static readonly string txtImproveTooltip = "ImproveTooltip".Translate();
        internal static readonly string txtMakeFriendsTooltip = "TryRecruitTooltip".Translate();

        protected static readonly Vector2 buttonSize = new Vector2(120f, 30f);
        private static Listing_Standard listingStandard = new Listing_Standard();

        public ITab_Pawn_Guest()
        {
            labelKey = "TabGuest";
            tutorTag = "Guest";
            size = new Vector2(500f, 450f);
        }

        public override bool IsVisible => SelPawn.IsGuest() || SelPawn.IsTrader();

        protected override void FillTab()
        {
            if (Multiplayer.IsRunning)
                Multiplayer.WatchBegin();

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20).ContractedBy(10f);
            listingStandard.Begin(rect);
            {
                if (SelPawn.IsTrader())
                {
                    FillTabTrader();
                }
                else
                {
                    FillTabGuest(rect);
                }
            }
            listingStandard.End();

            if (Multiplayer.IsRunning)
                Multiplayer.WatchEnd();
        }

        private void FillTabTrader()
        {
            listingStandard.Label("IsATrader".Translate().AdjustedFor(SelPawn));
        }

        private void FillTabGuest(Rect rect)
        {
            //ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.PrisonerTab, KnowledgeAmount.GuiFrame);
            var title = SelPawn.royalty?.MostSeniorTitle;
            var isRoyal = title != null;
            var friends = isRoyal ? SelPawn.GetFriendsSeniorityInColony() : SelPawn.GetFriendsInColony();
            var friendsRequired = isRoyal ? RequiredSeniority : RequiredFriends;
            var friendPercentage = 100f*friends/friendsRequired;

            var tryImprove = SelPawn.ImproveRelationship();
            var tryMakeFriends = SelPawn.MakeFriends();

            listingStandard.ColumnWidth = size.x - 20;

            var comp = SelPawn.CompGuest();
            var willOnlyJoinByForce = comp.WillOnlyJoinByForce;

            Multiplayer.guestFields?.Watch(comp);

            // If the lord is not on the map it's invalid!
            if (comp?.lord != null && comp.lord.ownedPawns.Contains(SelPawn) && SelPawn.Map.lordManager.lords.Contains(comp.lord))
            {
                listingStandard.Gap();
                string labelStay = "AreaToStay".Translate();
                string labelBuy = "AreaToBuy".Translate();
                var rectStayLabel = listingStandard.GetRect(Text.CalcHeight(labelStay, listingStandard.ColumnWidth));
                var rectStay = listingStandard.GetRect(24);
                var rectBuyLabel = listingStandard.GetRect(Text.CalcHeight(labelBuy, listingStandard.ColumnWidth));
                var rectBuy = listingStandard.GetRect(24);

                DialogUtility.LabelWithTooltip(labelStay, "AreaToStayTooltip".Translate(), rectStayLabel);
                GenericUtility.DoAreaRestriction(rectStay, comp.GuestArea, SetAreaRestriction, AreaUtility.AreaAllowedLabel_Area);
                DialogUtility.LabelWithTooltip(labelBuy, "AreaToBuyTooltip".Translate(), rectBuyLabel);
                GenericUtility.DoAreaRestriction(rectBuy, comp.ShoppingArea, SetAreaShopping, GenericUtility.GetShoppingLabel);

                var rectImproveRelationship = listingStandard.GetRect(Text.LineHeight);
                DialogUtility.CheckboxLabeled(listingStandard, "ImproveRelationship".Translate(), ref tryImprove, rectImproveRelationship, false, txtImproveTooltip);
                
                var rectMakeFriends = listingStandard.GetRect(Text.LineHeight);
                DialogUtility.CheckboxLabeled(listingStandard, "MakeFriends".Translate(), ref tryMakeFriends, rectMakeFriends, false, txtMakeFriendsTooltip);

                comp.entertain = tryImprove;
                comp.makeFriends = tryMakeFriends;

                listingStandard.Gap(50);

                var rectSetDefault = new Rect(rect.xMax - buttonSize.x - 10, 160, buttonSize.x, buttonSize.y);
                var rectSendHome = new Rect(rect.xMin - 10, 160, buttonSize.x, buttonSize.y);
                DialogUtility.DrawButton(() => SetAllDefaults(SelPawn), txtMakeDefault, rectSetDefault, txtMakeDefaultTooltip);
                DialogUtility.DrawButton(() => SendHomeDialog(SelPawn.GetLord()), txtSendAway, rectSendHome, txtSendAwayTooltip);

                var rectRecruitButton = new Rect(rect.xMin - 10 + 10 + buttonSize.x, 160, buttonSize.x, buttonSize.y);
                DialogUtility.DrawRecruitButton(rectRecruitButton, SelPawn, friends >= friendsRequired, isRoyal, willOnlyJoinByForce);

                // Highlight defaults
                if (Mouse.IsOver(rectSetDefault))
                {
                    Widgets.DrawHighlight(rectStay);
                    Widgets.DrawHighlight(rectStayLabel);
                    Widgets.DrawHighlight(rectBuy);
                    Widgets.DrawHighlight(rectBuyLabel);
                    Widgets.DrawHighlight(rectImproveRelationship);
                    Widgets.DrawHighlight(rectMakeFriends);
                }
            }

            if (SelPawn.Faction != null)
            {
                listingStandard.Label(txtRecruitmentPenalty.Translate(SelPawn.RecruitPenalty().ToString("##0"), SelPawn.ForcedRecruitPenalty().ToString("##0")));
                listingStandard.Label(txtFactionGoodwill + ": " + SelPawn.Faction.PlayerGoodwill.ToString("##0"));
            }

            listingStandard.Gap();

            if (!willOnlyJoinByForce)
            {
                if (isRoyal)
                    listingStandard.Label($"{"SeniorityRequirement".Translate(friends / 100, friendsRequired / 100)}:");
                else
                    listingStandard.Label($"{"FriendsRequirement".Translate(friends, friendsRequired)}:");

                listingStandard.Slider(Mathf.Clamp(friendPercentage, 0, 100), 0, 100);
            }

            if (isRoyal && willOnlyJoinByForce)
            {
                listingStandard.Label(ColoredText.StripTags("CanNeverRecruit".Translate().AdjustedFor(SelPawn)).Colorize(Color.red));
            }
            if (willOnlyJoinByForce)
            {
                listingStandard.Label(ColoredText.StripTags("OnlyJoinsByForce".Translate().AdjustedFor(SelPawn)).Colorize(Color.red));
            }
            else if (friendPercentage <= 99)
            {
                // Remove color from AdjustedFor and then Colorize
                listingStandard.Label(ColoredText.StripTags("NotEnoughFriends".Translate(SelPawn.GetMinRecruitOpinion()).AdjustedFor(SelPawn)).Colorize(Color.red));
            }
            else
            {
                listingStandard.Label("CanNowBeRecruited".Translate().AdjustedFor(SelPawn));
            }

            // Will only have score while "checked in", becomes 0 again when guest leaves
            if (SelPawn.GetVisitScore(out var score))
            {
                listingStandard.Label(txtHospitality + ":");
                listingStandard.Slider(score, 0f, 1f);
            }
        }

        private int RequiredFriends => GuestUtility.FriendsRequired(SelPawn.MapHeld) + SelPawn.GetEnemiesInColony();
        private int RequiredSeniority => GuestUtility.RoyalFriendsSeniorityRequired(SelPawn) + SelPawn.GetRoyalEnemiesSeniorityInColony();

        private void SetAreaRestriction(Area area)
        {
            foreach (var pawn in SelPawn.GetLord().ownedPawns)
            {
                pawn.CompGuest().GuestArea = area;
            }
        }

        private void SetAreaShopping(Area area)
        {
            foreach (var pawn in SelPawn.GetLord().ownedPawns)
            {
                pawn.CompGuest().ShoppingArea = area;
            }
        }

        public static void RecruitDialog(Pawn pawn, bool forced)
        {
            var penalty = forced ? pawn.ForcedRecruitPenalty() : pawn.RecruitPenalty();
            var hostileWarning = GetHostileWarning(pawn, penalty);
            var acidifierWarning = GetAcidifierWarning(pawn);

            var text = (forced ? "ForceRecruitQuestion" : "RecruitQuestion").Translate(penalty.ToString("##0"), hostileWarning, acidifierWarning, new NamedArgument(pawn, "PAWN"));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => GuestUtility.Recruit(pawn, penalty, forced)));
        }

        private static string GetHostileWarning(Pawn pawn, int penalty)
        {
            int finalGoodwill = Mathf.Clamp(pawn.Faction.PlayerGoodwill - penalty, -100, 100);

            var text = finalGoodwill <= DiplomacyTuning.BecomeHostileThreshold ? "ForceRecruitWarning".Translate() : TaggedString.Empty;
            return ColoredText.Colorize(text, ColoredText.FactionColor_Hostile);
        }

        private static string GetAcidifierWarning(Pawn pawn)
        {
            var text = pawn.HasDeathAcidifier() ? "ForceAcidifierWarning".Translate(new NamedArgument(pawn, "PAWN")) : TaggedString.Empty;
            return ColoredText.Colorize(text, ColoredText.FactionColor_Hostile);
        }

        private static void SendHomeDialog(Lord lord)
        {
            var text = txtSendAwayQuestion.Translate(new NamedArgument(lord.faction, "FACTION"));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, () => SendHome(lord)));
        }

        internal static void SendHome(Lord lord)
        {
            if (lord == null)
            {
                Log.Warning("lord == null");
                return;
            }

            foreach (var pawn in lord.ownedPawns)
            {
                var compGuest = pawn.CompGuest();
                if (compGuest == null)
                {
                    Log.Warning(pawn.LabelShortCap + " has no compGuest.");
                    continue;
                }

                compGuest.sentAway = true;
            }
        }

        internal static void SetAllDefaults(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            if (map == null) return;

            var mapComp = map.GetMapComponent();

            if (pawn.CompGuest() != null)
            {
                mapComp.defaultEntertain = pawn.CompGuest().entertain;
                mapComp.defaultMakeFriends = pawn.CompGuest().makeFriends;
            }
            
            mapComp.defaultAreaRestriction = pawn.GetGuestArea();
            mapComp.defaultAreaShopping = pawn.GetShoppingArea();

            var guests = GuestUtility.GetAllGuests(map);
            foreach (var guest in guests)
            {
                var comp = guest.CompGuest();
                if (comp != null)
                {
                    comp.entertain = mapComp.defaultEntertain;
                    comp.makeFriends = mapComp.defaultMakeFriends;
                    comp.GuestArea = mapComp.defaultAreaRestriction;
                    comp.ShoppingArea = mapComp.defaultAreaShopping;
                }
            }
        }
    }
}
