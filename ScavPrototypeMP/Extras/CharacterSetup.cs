using CasualtiesTogetherUtils;
using HarmonyLib;
using UnityEngine;

namespace Together;

internal static class CharacterSetup {
    static int BaseFor(int id, string who) {
        if (!CharacterTable.IsKnown(id) && CharacterTable.FirstSightingOfUnknown(id)) {
            log.warn($"character id {id} for {who} is not registered on this machine, using base 0 (the other side has a different character table)");
        }
        return CharacterTable.BaseOf(id);
    }

    static void Tag(Body body, int id) {
        var tag = body.GetComponent<CharacterIdTag>();
        if (tag == null) tag = body.gameObject.AddComponent<CharacterIdTag>();
        tag.Id = (byte)id;
    }

    internal static void ApplyBeforeAwake(Body body, int id, string who) {
        if (body == null) return;
        int baseCharacter = BaseFor(id, who);
        int previous = body.charType;
        body.charType = baseCharacter;
        Tag(body, id);
        log.l($"character for {who}: id {id}, charType {previous} -> {baseCharacter} (applied before Awake)");
    }

    internal static void ApplyToLiveBody(Body body, int id, string who, bool respectLoadedRun) {
        if (body == null) return;
        int baseCharacter = BaseFor(id, who);
        bool sameId = body.GetComponent<CharacterIdTag>() != null && TogetherCharacters.GetCharacterId(body) == id;
        if (sameId && body.charType == baseCharacter) return;
        if (respectLoadedRun && SaveSystem.loadedRun) {
            log.warn($"character rule {id} ignored for {who}: the run was loaded from a save");
            return;
        }
        int previous = body.charType;
        Tag(body, id);
        if (body.charType != baseCharacter) {
            body.charType = baseCharacter;
            body.skills.Setup(baseCharacter);
        }
        log.l($"character for {who}: id {id}, charType {previous} -> {baseCharacter} (skills re-setup)");
    }

    internal static void AfterBodyBuilt(Body body, int id, ScavPlayer player) {
        TogetherCharacters.RaiseBodyBuilt(body, (byte)id, player);
    }

    internal static byte LocalCharacterId(Body body) {
        if (RuleChoosesCharacter(out int ruleId)) return (byte)ruleId;
        byte chosen = LocalCharacter.Current;
        if (CharacterChoice.Chooses(chosen)) return chosen;
        return (byte)Mathf.Clamp(body.charType, 0, CharacterTable.HighestBaseCharacter);
    }

    internal static byte Server_ResolveFor(ScavPlayer plr) {
        byte reported = plr.IsLocal && plr.IsHost && plr.ReportedCharacter == CharacterChoice.NoChoice ? LocalCharacter.Hint : plr.ReportedCharacter;
        return CharacterChoice.Resolve(Multiplayer.rules.PlayerCharacter, reported);
    }

    internal static void Server_Publish(ScavPlayer plr) {
        if (!plr.TryGetNetBody(out var netBody) || netBody.syncBody == null) return;
        byte resolved = Server_ResolveFor(plr);
        byte oldEncoded = netBody.syncBody.characterSelected.Value;
        if (oldEncoded == resolved) return;
        netBody.syncBody.characterSelected.Value = resolved;
        log.l($"character for {plr.playerName} published as {resolved - 1}");
        if (!plr.IsLocal && CharacterChoice.TryDecode(resolved, out int id)) {
            ApplyToLiveBody(netBody.body, id, plr.playerName, false);
            if (CharacterChoice.TryDecode(oldEncoded, out int oldId)) {
                TogetherCharacters.RaiseCharacterChanged(plr, (byte)oldId, (byte)id);
            }
        }
    }

    internal static void ReportOwnCharacter(int id) {
        if (!CharacterTable.IsKnown(id) || !Net.IsRunning) return;
        if (Net.IsServer) {
            var local = ScavPlayer.LocalPlayer;
            if (local == null) return;
            local.ReportedCharacter = (byte)id;
            Server_Publish(local);
        } else if (Net.IsClient) {
            var writer = Net.CreateWriter(NetmsgId.SERVER_ReportCharacter);
            writer.Put((byte)id);
            Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableOrdered, writer);
            log.l($"reported own character {id} to the server");
        }
    }

    internal static void SendChoiceToServer() {
        if (!Net.IsRunning || !Net.IsClient) return;
        byte hint = LocalCharacter.Hint;
        if (!CharacterTable.IsKnown(hint)) return;
        var writer = Net.CreateWriter(NetmsgId.SERVER_ReportCharacter);
        writer.Put(hint);
        Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableOrdered, writer);
    }

    [ServerReceiver((byte)NetmsgId.SERVER_ReportCharacter)]
    static void Server_ReportCharacter(knetid clientId, LiteNetLib.Utils.NetDataReader reader) {
        reader.Get(out byte reported);
        if (!ScavPlayer.TryGetPlayerFromClientId(clientId, out var plr)) return;
        if (!CharacterTable.IsKnown(reported)) {
            if (CharacterTable.FirstSightingOfUnknown(reported)) {
                log.warn($"{plr.playerName} reports character id {reported}, which is not registered on the host");
            }
            return;
        }
        plr.ReportedCharacter = reported;
        log.l($"{plr.playerName} reports character {reported}");
        Server_Publish(plr);
    }

    internal static void OnReplicatedCharacterChanged(SyncBody syncBody) {
        var netBody = syncBody.nb;
        if (netBody == null || netBody.body == null || netBody.IsBodyLocal()) return;
        if (!CharacterChoice.TryDecode(syncBody.characterSelected.Value, out int id)) return;
        byte oldId = TogetherCharacters.GetCharacterId(netBody.body);
        ApplyToLiveBody(netBody.body, id, netBody.name, false);
        if (oldId != id) {
            var parentPlayer = syncBody.GetParent<BasePlayer>();
            TogetherCharacters.RaiseCharacterChanged(parentPlayer != null ? parentPlayer.player : null, oldId, (byte)id);
        }
    }

    internal static bool RuleChoosesCharacter(out int id) {
        byte value = Multiplayer.rules.PlayerCharacter;
        id = value;
        return CharacterChoice.Chooses(value);
    }

    internal static string Describe(Body body) {
        var skills = body.skills;
        return $"id={TogetherCharacters.GetCharacterId(body)} charType={body.charType} STR={skills.STR} RES={skills.RES} INT={skills.INT} gainMult={skills.STRGainMult}/{skills.RESGainMult}/{skills.INTGainMult}";
    }
}

[HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.SpawnCharacter))]
static class PlayerCamera_SpawnCharacter_ChoicePatch {
    static void Prefix(ref int chr) {
        if (!Net.IsRunning || SaveSystem.loadedRun) return;
        int chosen;
        if (CharacterSetup.RuleChoosesCharacter(out chosen)) {
            chr = CharacterTable.BaseOf(chosen);
        } else if (CharacterChoice.Chooses(LocalCharacter.Current)) {
            chr = CharacterTable.BaseOf(LocalCharacter.Current);
        } else {
            return;
        }
        log.l($"local character spawn: {chr}");
    }
}
