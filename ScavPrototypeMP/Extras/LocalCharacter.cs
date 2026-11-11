namespace Together;

internal static class LocalCharacter {
    public const string SettingKey = "my_character";

    public static int SessionOverride = CharacterChoice.NoChoice;

    public static byte Current {
        get {
            if (CharacterTable.IsKnown(SessionOverride)) return (byte)SessionOverride;
            int saved = SettingsJsonThing.json.GetInt(SettingKey, CharacterChoice.NoChoice);
            return CharacterTable.IsKnown(saved) ? (byte)saved : CharacterChoice.NoChoice;
        }
    }

    public static byte Hint {
        get {
            if (CharacterChoice.Chooses(Current)) return Current;
            int menuPick = PreRunScript.characterSelected;
            return CharacterTable.IsKnown(menuPick) ? (byte)menuPick : CharacterChoice.NoChoice;
        }
    }

    public static void Cycle() {
        SessionOverride = CharacterChoice.NoChoice;
        SettingsJsonThing.json.SetInt(SettingKey, CharacterChoice.NextInCycle(Current));
        CharacterSetup.SendChoiceToServer();
    }
}
