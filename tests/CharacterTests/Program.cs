using System;

namespace Together.Tests;

internal static class Program {
    static int passed, failed;

    internal static void Check(bool condition, string name) {
        if (condition) { passed++; Console.WriteLine("PASS " + name); }
        else { failed++; Console.WriteLine("FAIL " + name); }
    }

    static int Main() {
        CharacterChoiceTests.Run();
        CharacterTableTests.Run();
        Console.WriteLine($"{passed} passed, {failed} failed");
        return failed == 0 ? 0 : 1;
    }
}
