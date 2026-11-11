using System;
using UnityEngine;

namespace Together;

public static class TogetherCharacters {
    public const byte FirstCustomId = CharacterTable.FirstCustomId;
    public const byte MaxId = CharacterTable.MaxId;
    public const int ApiVersion = 1;

    public static event Action<Body, byte, ScavPlayer> BodyBuilt;
    public static event Action<ScavPlayer, byte, byte> CharacterChanged;

    public static uint TableFingerprint => CharacterTable.Fingerprint();

    public static bool Register(byte id, string displayName, byte baseCharacter) {
        if (CharacterTable.Register(id, displayName, baseCharacter, out string error)) {
            log.l($"character registered: id {id} \"{displayName}\" base {baseCharacter}");
            return true;
        }
        log.warn($"character registration refused for id {id}: {error}");
        return false;
    }

    public static bool IsRegistered(byte id) => CharacterTable.IsKnown(id);

    public static byte GetCharacterId(Body body) {
        if (body == null) return 0;
        var tag = body.GetComponent<CharacterIdTag>();
        return tag != null ? tag.Id : (byte)Mathf.Clamp(body.charType, 0, CharacterTable.HighestBaseCharacter);
    }

    internal static void RaiseBodyBuilt(Body body, byte id, ScavPlayer player) {
        var handlers = BodyBuilt;
        if (handlers == null) return;
        foreach (Action<Body, byte, ScavPlayer> handler in handlers.GetInvocationList()) {
            try { handler(body, id, player); } catch (Exception exception) { log.warn($"TogetherCharacters.BodyBuilt handler failed: {exception}"); }
        }
    }

    internal static void RaiseCharacterChanged(ScavPlayer player, byte oldId, byte newId) {
        var handlers = CharacterChanged;
        if (handlers == null) return;
        foreach (Action<ScavPlayer, byte, byte> handler in handlers.GetInvocationList()) {
            try { handler(player, oldId, newId); } catch (Exception exception) { log.warn($"TogetherCharacters.CharacterChanged handler failed: {exception}"); }
        }
    }
}

public sealed class CharacterIdTag : MonoBehaviour {
    public byte Id;
}
