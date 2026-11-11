namespace Together;

internal static class CharacterChoice {
    public const byte Unset = 0;
    public const byte NoChoice = CharacterTable.NoChoice;

    public static bool Chooses(byte ruleValue) => CharacterTable.IsKnown(ruleValue);

    public static byte NextInCycle(byte current) => CharacterTable.NextInCycle(current);

    public static string Label(byte value) => CharacterTable.IsKnown(value) ? CharacterTable.NameOf(value) : "game default";

    public static byte Encode(int id) => CharacterTable.Encode(id);

    public static bool TryDecode(byte encoded, out int id) => CharacterTable.TryDecode(encoded, out id);

    public static byte Resolve(byte ruleValue, byte reportedByPlayer) {
        if (CharacterTable.IsKnown(ruleValue)) return Encode(ruleValue);
        if (CharacterTable.IsKnown(reportedByPlayer)) return Encode(reportedByPlayer);
        return Unset;
    }
}
