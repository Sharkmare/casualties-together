using System.Linq;
using Together;

namespace Together.Tests;

internal static class CharacterTableTests {
    internal static void Run() {
        CharacterTable.ResetToVanilla();
        uint vanillaFingerprint = CharacterTable.Fingerprint();
        Program.Check(CharacterTable.Ids().SequenceEqual(new byte[] { 0, 1, 2 }) && CharacterTable.NameOf(0) == "Experiment" && CharacterTable.NameOf(1) == "Milky" && CharacterTable.NameOf(2) == "Dune", "the table starts as the three vanilla characters");
        Program.Check(CharacterTable.BaseOf(0) == 0 && CharacterTable.BaseOf(1) == 1 && CharacterTable.BaseOf(2) == 2, "vanilla ids are their own base");

        string error;
        Program.Check(!CharacterTable.Register(2, "Clone", 0, out error) && error.Contains("vanilla"), "a vanilla id cannot be re-registered");
        Program.Check(!CharacterTable.Register(255, "Sentinel", 0, out error) && error.Contains("254"), "255 is the keep-your-own sentinel and cannot be a character");
        Program.Check(!CharacterTable.Register(3, "BadBase", 3, out error) && error.Contains("base"), "a base above 2 is refused");
        Program.Check(!CharacterTable.Register(3, "  ", 0, out error), "an empty name is refused");
        Program.Check(CharacterTable.Ids().Count == 3 && CharacterTable.Fingerprint() == vanillaFingerprint, "refused registrations change nothing");

        Program.Check(CharacterTable.Register(3, "Portal", 0, out error) && CharacterTable.Register(254, "Last", 2, out error), "ids 3 and 254 register");
        Program.Check(!CharacterTable.Register(3, "Again", 1, out error) && error.Contains("already"), "a duplicate id is refused");
        Program.Check(CharacterTable.IsKnown(3) && CharacterTable.BaseOf(254) == 2 && CharacterTable.NameOf(3) == "Portal", "registered ids resolve to their base and name");

        Program.Check(CharacterTable.Encode(254) == 255 && CharacterTable.Encode(0) == 1, "ids encode as id plus one, 254 is the highest that fits a byte");
        int decoded;
        Program.Check(CharacterTable.TryDecode(255, out decoded) && decoded == 254 && !CharacterTable.TryDecode(0, out _), "encoded 255 is id 254 and 0 means unset");

        Program.Check(CharacterTable.BaseOf(77) == 0 && !CharacterTable.IsKnown(77), "an unknown id falls back to base 0");
        Program.Check(CharacterTable.FirstSightingOfUnknown(77) && !CharacterTable.FirstSightingOfUnknown(77) && !CharacterTable.FirstSightingOfUnknown(3), "an unknown id is reported once, a known one never");

        Program.Check(CharacterTable.NextInCycle(255) == 0 && CharacterTable.NextInCycle(2) == 3 && CharacterTable.NextInCycle(3) == 254 && CharacterTable.NextInCycle(254) == 255, "the button cycle runs through every registered id and back to default");

        Program.Check(CharacterChoice.Resolve(255, 3) == 4 && CharacterChoice.Resolve(254, 0) == 255 && CharacterChoice.Resolve(255, 77) == CharacterChoice.Unset, "rule and report resolve with registered ids and ignore unknown ones");

        uint withTwo = CharacterTable.Fingerprint();
        Program.Check(withTwo != vanillaFingerprint, "registering characters changes the fingerprint");
        CharacterTable.ResetToVanilla();
        CharacterTable.Register(254, "Last", 2, out error);
        CharacterTable.Register(3, "Portal", 0, out error);
        Program.Check(CharacterTable.Fingerprint() == withTwo, "the fingerprint does not depend on registration order");
        CharacterTable.ResetToVanilla();
        CharacterTable.Register(3, "Portal", 1, out error);
        CharacterTable.Register(254, "Last", 2, out error);
        Program.Check(CharacterTable.Fingerprint() != withTwo, "a different base changes the fingerprint");
        CharacterTable.ResetToVanilla();
        Program.Check(CharacterTable.Fingerprint() == vanillaFingerprint, "the vanilla fingerprint is stable");
    }
}
