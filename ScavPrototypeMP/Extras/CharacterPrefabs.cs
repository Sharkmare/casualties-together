using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Together;

internal static class CharacterPrefabs {
    static readonly string[] ResourcePaths = {
        "Special/Characters/Experiment",
        "Special/Characters/Milky",
        "Special/Characters/Dune",
    };

    static readonly GameObject[] byCharacter = new GameObject[ResourcePaths.Length];

    internal static GameObject For(int characterType) {
        if (characterType >= 0 && characterType < byCharacter.Length && byCharacter[characterType] != null) return byCharacter[characterType];
        return ServerMain.CHARACTER_PREFAB;
    }

    internal static string Layout(Body body) {
        var limbNames = body.limbs.Select(limb => limb.name);
        int hinges = body.GetComponentsInChildren<HingeJoint2D>(true).Length;
        return $"limbs={body.limbs.Length}[{string.Join(",", limbNames)}] hinges={hinges} slots={body.slots.Length}";
    }

    internal static bool AnimatorsInside(Body body, Transform root) {
        return IsInside(root, body.bodyAnimator) && IsInside(root, body.armsAnimator);
    }

    static bool IsInside(Transform root, Component component) => component != null && component.transform.IsChildOf(root);

    internal static void Build() {
        var reference = ServerMain.CHARACTER_PREFAB != null ? ServerMain.CHARACTER_PREFAB.GetComponentInChildren<Body>(true) : null;
        if (reference == null) return;
        string referenceLayout = Layout(reference);
        log.l($"reference character prefab: charType={reference.charType} {referenceLayout} animatorsInside={AnimatorsInside(reference, ServerMain.CHARACTER_PREFAB.transform)} root={ServerMain.CHARACTER_PREFAB.name} bodyParent={reference.transform.parent?.name}");

        var inactiveParent = new GameObject("TogetherPrefabParking");
        inactiveParent.SetActive(false);
        UnityEngine.Object.DontDestroyOnLoad(inactiveParent);

        for (int characterType = 0; characterType < ResourcePaths.Length; characterType++) {
            try {
                var asset = Resources.Load<GameObject>(ResourcePaths[characterType]);
                var assetBody = asset != null ? asset.GetComponentInChildren<Body>(true) : null;
                if (assetBody == null) {
                    log.warn($"character {characterType}: no Body in {ResourcePaths[characterType]}");
                    continue;
                }
                var copy = UnityEngine.Object.Instantiate(asset, inactiveParent.transform, false);
                copy.SetActive(false);
                copy.transform.SetParent(null, false);
                UnityEngine.Object.DontDestroyOnLoad(copy);
                var copyBody = copy.GetComponentInChildren<Body>(true);
                string layout = Layout(copyBody);
                bool animatorsInside = AnimatorsInside(copyBody, copy.transform);
                bool sameLayout = layout == referenceLayout;
                log.l($"character {characterType} prefab: charType={copyBody.charType} {layout} animatorsInside={animatorsInside} sameLayoutAsReference={sameLayout}");
                if (!sameLayout || !animatorsInside) {
                    log.warn($"character {characterType} prefab is not used, falling back to the reference prefab (layout differs or animators are outside the body)");
                    UnityEngine.Object.Destroy(copy);
                    continue;
                }
                ClientMain.RecordHingeStates(copy);
                byCharacter[characterType] = copy;
            } catch (Exception exception) {
                log.warn($"CharacterPrefabs.Build: {exception}");
            }
        }
    }
}
