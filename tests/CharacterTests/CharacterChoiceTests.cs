using Together;

namespace Together.Tests;

internal static class CharacterChoiceTests {
    internal static void Run() {
        CharacterTable.ResetToVanilla();
        Program.Check(CharacterChoice.Resolve(255, 2) == 3 && CharacterChoice.Resolve(255, 0) == 1, "rule 255: each player's own report is used (encoded +1)");
        Program.Check(CharacterChoice.Resolve(2, 0) == 3 && CharacterChoice.Resolve(0, 2) == 1, "a rule value overrides what the player reported");
        Program.Check(CharacterChoice.Resolve(255, 255) == CharacterChoice.Unset && CharacterChoice.Resolve(255, 7) == CharacterChoice.Unset, "no report or an invalid report stays unset");
        Program.Check(CharacterChoice.Resolve(9, 1) == 2, "an invalid rule value falls back to the player's report");
        int decoded;
        Program.Check(CharacterChoice.TryDecode(3, out decoded) && decoded == 2, "encoded 3 decodes to Dune");
        Program.Check(!CharacterChoice.TryDecode(0, out _), "unset does not decode");
        Program.Check(CharacterChoice.NextInCycle(255) == 0 && CharacterChoice.NextInCycle(0) == 1 && CharacterChoice.NextInCycle(1) == 2 && CharacterChoice.NextInCycle(2) == 255, "the join screen button cycles default, Experiment, Milky, Dune");
        Program.Check(CharacterChoice.Label(2) == "Dune" && CharacterChoice.Label(255) == "game default" && CharacterChoice.Label(9) == "game default", "button labels");
        Program.Check(CharacterChoice.Resolve(255, 1) == 2 && CharacterChoice.Resolve(1, 2) == 2, "character 1 (Milky) resolves from a report and from the rule");
        Program.Check(CharacterChoice.TryDecode(2, out decoded) && decoded == 1 && CharacterChoice.Label(1) == "Milky", "character 1 decodes and is labelled Milky");
        Program.Check(!CharacterChoice.Chooses(CharacterChoice.NoChoice) && CharacterChoice.Chooses(2) && CharacterChoice.Chooses(0), "255 keeps each player's own character, 0 and 2 override");
        Program.Check(CharacterChoice.Chooses(1) && !CharacterChoice.Chooses(3), "rule values 0, 1 and 2 are characters, an unregistered 3 is not");
        bool differsByPlayer = CharacterChoice.Resolve(255, 2) != CharacterChoice.Resolve(255, 0);
        Program.Check(differsByPlayer, "two players with different reports resolve to different characters");
    }
}
