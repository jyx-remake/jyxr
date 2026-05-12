using Game.Core.Abstractions;
using Game.Core.Model;
using Game.Core.Model.Character;
using Game.Core.Story;

namespace Game.Application;

internal sealed class ApplicationPredicateLibrary
{
    private readonly GameSession _session;

    public ApplicationPredicateLibrary(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    private GameState State => _session.State;
    private AdventureState Adventure => State.Adventure;
    private IContentRepository ContentRepository => _session.ContentRepository;

    [GamePredicate("always")]
    private static bool Always() => true;

    [GamePredicate("should_finish")]
    private bool ShouldFinish(string storyId) => State.Story.IsStoryCompleted(storyId);

    [GamePredicate("should_not_finish")]
    private bool ShouldNotFinish(string storyId) => !State.Story.IsStoryCompleted(storyId);

    [GamePredicate("follow_story")]
    private bool FollowStory(string storyId) =>
        string.Equals(State.Story.LastStoryId, storyId, StringComparison.Ordinal);

    [GamePredicate("has_time_key")]
    private bool HasTimeKey(string key) => State.Story.HasTimeKey(key);

    [GamePredicate("not_has_time_key")]
    private bool NotHasTimeKey(string key) => !State.Story.HasTimeKey(key);

    [GamePredicate("have_item")]
    private bool HaveItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return false;
        }

        return ContentRepository.TryGetItem(itemId, out var item) &&
            State.Inventory.ContainsStack(item, quantity);
    }

    [GamePredicate("not_have_item")]
    private bool NotHaveItem(string itemId, int quantity = 1) => !HaveItem(itemId, quantity);

    [GamePredicate("have_money", "silver_at_least")]
    private bool HaveSilverAtLeast(int amount) => State.Currency.Silver >= amount;

    [GamePredicate("gold_at_least")]
    private bool HaveGoldAtLeast(int amount) => State.Currency.Gold >= amount;

    [GamePredicate("have_yuanbao")]
    private bool HaveYuanbao(int amount) => State.Currency.Gold >= amount;

    [GamePredicate("friendCount")]
    private bool FriendCountAtLeast(int count) => State.Party.Members.Count >= count;

    [GamePredicate("current_map")]
    private bool CurrentMap(string mapId) =>
        string.Equals(State.Location.CurrentMapId, mapId, StringComparison.Ordinal);

    [GamePredicate("event_completed", "event_finished")]
    private bool EventCompleted(string eventKey) => State.MapEventProgress.IsCompleted(eventKey);

    [GamePredicate("event_not_completed", "event_not_finished")]
    private bool EventNotCompleted(string eventKey) => !State.MapEventProgress.IsCompleted(eventKey);

    [GamePredicate("time_slot", "in_time")]
    private bool InTime(params string[] timeSlots)
    {
        if (timeSlots.Length == 0)
        {
            throw new InvalidOperationException("Predicate 'time_slot' requires at least one time slot.");
        }

        foreach (var timeSlotText in timeSlots)
        {
            if (!TryParseTimeSlot(timeSlotText, out var timeSlot))
            {
                throw new InvalidOperationException($"Invalid time slot '{timeSlotText}'.");
            }

            if (State.Clock.TimeSlot == timeSlot)
            {
                return true;
            }
        }

        return false;
    }

    [GamePredicate("not_in_time")]
    private bool NotInTime(params string[] timeSlots) => !InTime(timeSlots);

    [GamePredicate("key_in_team")]
    private bool KeyInTeamActive(string characterId) => _session.PartyService.ContainsActiveMemberId(characterId);

    [GamePredicate("key_not_in_team")]
    private bool KeyNotInTeamActive(string characterId) => !_session.PartyService.ContainsActiveMemberId(characterId);

    [GamePredicate("in_team")]
    private bool NameInTeamActive(string characterName) => _session.PartyService.ContainsActiveMemberName(characterName);

    [GamePredicate("not_in_team")]
    private bool NameNotInTeamActive(string characterName) => !_session.PartyService.ContainsActiveMemberName(characterName);

    [GamePredicate("character_level_less_than")]
    private bool CharacterLevelLessThan(string characterIdOrName, int threshold) =>
        TryFindPartyMember(characterIdOrName, out var character) && character.Level < threshold;

    [GamePredicate("level_greater_than")]
    private bool CharacterLevelGreaterThan(string characterIdOrName, int threshold) =>
        TryFindPartyMember(characterIdOrName, out var character) && character.Level >= threshold;

    [GamePredicate("shenfa_greater_than")]
    private bool ShenfaGreaterThan(string characterIdOrName, int threshold) =>
        TryFindPartyMember(characterIdOrName, out var character) &&
        character.GetBaseStat(StatType.Shenfa) > threshold;

    [GamePredicate("character_skill_less_than")]
    private bool CharacterSkillLessThan(string characterIdOrName, string skillId, int threshold)
    {
        if (!TryFindPartyMember(characterIdOrName, out var character))
        {
            return 0 < threshold;
        }

        return GetCharacterSkillLevel(character, skillId) < threshold;
    }

    [GamePredicate("character_skill_more_than")]
    private bool CharacterSkillMoreThan(string characterIdOrName, string skillId, int threshold)
    {
        if (!TryFindPartyMember(characterIdOrName, out var character))
        {
            return 0 >= threshold;
        }

        return GetCharacterSkillLevel(character, skillId) >= threshold;
    }

    [GamePredicate("skill_less_than")]
    private bool MainCharacterSkillLessThan(string skillId, int threshold)
    {
        var mainCharacter = GetMainCharacter();
        return (mainCharacter is null ? 0 : GetCharacterSkillLevel(mainCharacter, skillId)) < threshold;
    }

    [GamePredicate("jianfa_less_than")]
    private bool JianfaLessThan(string characterIdOrName, int threshold) =>
        CharacterStatLessThan(characterIdOrName, StatType.Jianfa, threshold);

    [GamePredicate("daofa_less_than")]
    private bool DaofaLessThan(string characterIdOrName, int threshold) =>
        CharacterStatLessThan(characterIdOrName, StatType.Daofa, threshold);

    [GamePredicate("quanzhang_less_than")]
    private bool QuanzhangLessThan(string characterIdOrName, int threshold) =>
        CharacterStatLessThan(characterIdOrName, StatType.Quanzhang, threshold);

    [GamePredicate("qimen_less_than")]
    private bool QimenLessThan(string characterIdOrName, int threshold) =>
        CharacterStatLessThan(characterIdOrName, StatType.Qimen, threshold);

    [GamePredicate("exceed_day")]
    private bool ExceedDay(int days) => State.Clock.TotalDays >= days;

    [GamePredicate("not_exceed_day")]
    private bool NotExceedDay(int days) => State.Clock.TotalDays < days;

    [GamePredicate("in_round")]
    private bool InRound(int round) => Adventure.Round == round;

    [GamePredicate("not_in_round")]
    private bool NotInRound(int round) => Adventure.Round != round;

    [GamePredicate("zhoumu_greater_than")]
    private bool RoundAtLeast(int round) => Adventure.Round >= round;

    [GamePredicate("game_mode")]
    private bool GameMode(string modeId) => Adventure.IsDifficulty(modeId);

    [GamePredicate("in_menpai", "in_sect")]
    private bool InSect(string sectId) => Adventure.IsInSect(sectId);

    [GamePredicate("not_in_menpai", "not_in_sect")]
    private bool NotInSect(string sectId) => !Adventure.IsInSect(sectId);

    [GamePredicate("probability")]
    private static bool Probability(int percentage)
    {
        if (percentage is < 0 or > 100)
        {
            throw new InvalidOperationException("Predicate 'probability' requires probability between 0 and 100.");
        }

        return Random.Shared.Next(100) < percentage;
    }

    [GamePredicate("daode_more_than")]
    private bool MoralityMoreThan(double threshold) => Adventure.Morality > threshold;

    [GamePredicate("daode_less_than")]
    private bool MoralityLessThan(double threshold) => Adventure.Morality < threshold;

    [GamePredicate("haogan_more_than")]
    private bool FavorabilityMoreThan(double threshold) => Adventure.Favorability > threshold;

    [GamePredicate("haogan_less_than")]
    private bool FavorabilityLessThan(double threshold) => Adventure.Favorability < threshold;

    [GamePredicate("rank")]
    private bool RankAtLeast(double threshold) => Adventure.Rank >= threshold;

    [GamePredicate("dingli_greater_than")]
    private bool DingliGreaterThan(double threshold) => GetMainCharacterStat(StatType.Dingli) > threshold;

    [GamePredicate("dingli_less_than")]
    private bool DingliLessThan(double threshold) => GetMainCharacterStat(StatType.Dingli) < threshold;

    [GamePredicate("wuxing_greater_than")]
    private bool WuxingGreaterThan(double threshold) => GetMainCharacterStat(StatType.Wuxing) > threshold;

    [GamePredicate("wuxing_less_than")]
    private bool WuxingLessThan(double threshold) => GetMainCharacterStat(StatType.Wuxing) < threshold;

    [GamePredicate("in_newbie_task")]
    // Newbie task state is not modeled yet; keep related map events disabled until it has real state.
    private static bool InNewbieTask() => false;

    private CharacterInstance? GetMainCharacter() => State.Party.Members.FirstOrDefault();

    private double GetMainCharacterStat(StatType statType) => GetMainCharacter()?.GetBaseStat(statType) ?? 0d;

    private bool TryFindPartyMember(string idOrName, out CharacterInstance character)
    {
        foreach (var member in State.Party.Members)
        {
            if (string.Equals(member.Id, idOrName, StringComparison.Ordinal) ||
                string.Equals(member.Name, idOrName, StringComparison.Ordinal) ||
                string.Equals(member.Definition.Name, idOrName, StringComparison.Ordinal))
            {
                character = member;
                return true;
            }
        }

        character = null!;
        return false;
    }

    private static int GetCharacterSkillLevel(CharacterInstance character, string skillId) =>
        character.GetExternalSkillLevel(skillId) ??
        character.GetInternalSkillLevel(skillId) ??
        character.GetFormSkills()
            .FirstOrDefault(skill => string.Equals(skill.Definition.Id, skillId, StringComparison.Ordinal))
            ?.Level ??
        0;

    private bool CharacterStatLessThan(string characterIdOrName, StatType statType, int threshold)
    {
        if (!TryFindPartyMember(characterIdOrName, out var character))
        {
            return 0 < threshold;
        }

        return character.GetBaseStat(statType) < threshold;
    }

    private static bool TryParseTimeSlot(string value, out TimeSlot timeSlot)
    {
        if (Enum.TryParse(value, ignoreCase: true, out timeSlot))
        {
            return true;
        }

        timeSlot = value switch
        {
            "子" => TimeSlot.Zi,
            "丑" => TimeSlot.Chou,
            "寅" => TimeSlot.Yin,
            "卯" => TimeSlot.Mao,
            "辰" => TimeSlot.Chen,
            "巳" => TimeSlot.Si,
            "午" => TimeSlot.Wu,
            "未" => TimeSlot.Wei,
            "申" => TimeSlot.Shen,
            "酉" => TimeSlot.You,
            "戌" => TimeSlot.Xu,
            "亥" => TimeSlot.Hai,
            _ => default,
        };

        return value is "子" or "丑" or "寅" or "卯" or "辰" or "巳" or "午" or "未" or "申" or "酉" or "戌" or "亥";
    }
}
