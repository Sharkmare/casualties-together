using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Together;

internal static class CharacterTable {
    public const byte FirstCustomId = 3;
    public const byte MaxId = 254;
    public const byte NoChoice = 255;
    public const byte HighestBaseCharacter = 2;

    public readonly struct Entry {
        public readonly byte Id;
        public readonly string DisplayName;
        public readonly byte BaseCharacter;

        public Entry(byte id, string displayName, byte baseCharacter) {
            Id = id;
            DisplayName = displayName;
            BaseCharacter = baseCharacter;
        }
    }

    static readonly object sync = new object();
    static readonly SortedDictionary<byte, Entry> entries = new SortedDictionary<byte, Entry>();
    static readonly HashSet<byte> warnedUnknown = new HashSet<byte>();

    static CharacterTable() {
        ResetToVanilla();
    }

    public static void ResetToVanilla() {
        lock (sync) {
            entries.Clear();
            warnedUnknown.Clear();
            entries[0] = new Entry(0, "Experiment", 0);
            entries[1] = new Entry(1, "Milky", 1);
            entries[2] = new Entry(2, "Dune", 2);
        }
    }

    public static bool Register(byte id, string displayName, byte baseCharacter, out string error) {
        error = null;
        if (id < FirstCustomId) error = "ids 0, 1 and 2 are the vanilla characters";
        else if (id > MaxId) error = "the highest usable id is " + MaxId + " (255 means each player keeps their own)";
        else if (baseCharacter > HighestBaseCharacter) error = "the base character must be 0, 1 or 2";
        else if (string.IsNullOrWhiteSpace(displayName)) error = "the display name is empty";
        if (error != null) return false;
        lock (sync) {
            if (entries.ContainsKey(id)) {
                error = "id " + id + " is already registered as " + entries[id].DisplayName;
                return false;
            }
            entries[id] = new Entry(id, displayName.Trim(), baseCharacter);
            return true;
        }
    }

    public static bool IsKnown(int id) {
        if (id < 0 || id > MaxId) return false;
        lock (sync) return entries.ContainsKey((byte)id);
    }

    public static int BaseOf(int id) {
        if (id < 0 || id > MaxId) return 0;
        lock (sync) return entries.TryGetValue((byte)id, out var entry) ? entry.BaseCharacter : 0;
    }

    public static string NameOf(int id) {
        if (id < 0 || id > MaxId) return "game default";
        lock (sync) return entries.TryGetValue((byte)id, out var entry) ? entry.DisplayName : "unknown " + id;
    }

    public static IReadOnlyList<byte> Ids() {
        lock (sync) return entries.Keys.ToList();
    }

    public static byte NextInCycle(byte current) {
        lock (sync) {
            foreach (var id in entries.Keys) {
                if (current == NoChoice || id > current) return id;
            }
            return NoChoice;
        }
    }

    public static byte Encode(int id) => (byte)(id + 1);

    public static bool TryDecode(byte encoded, out int id) {
        id = encoded - 1;
        return encoded != 0;
    }

    public static uint Fingerprint() {
        lock (sync) {
            uint hash = 2166136261;
            foreach (var entry in entries.Values) {
                foreach (var b in Encoding.UTF8.GetBytes(entry.Id + ":" + entry.DisplayName + ":" + entry.BaseCharacter + ";")) {
                    hash = (hash ^ b) * 16777619;
                }
            }
            return hash;
        }
    }

    public static bool FirstSightingOfUnknown(int id) {
        if (id < 0 || id > MaxId) return false;
        lock (sync) return !entries.ContainsKey((byte)id) && warnedUnknown.Add((byte)id);
    }
}
