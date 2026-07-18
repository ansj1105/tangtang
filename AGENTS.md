# likeTangTang Agent Memo

## Android APK Build Path

- Use Unity `2021.3.19f1` from `C:\work\Unity\Hub\Editor\2021.3.19f1\Editor\Unity.exe`.
- Windows build source is `C:\work\likeTangTang-build-src\LikeTangTang`; final copied APK is `C:\work\LikeTangTang.apk`.
- Keep the Windows Unity `Library/` cache. First IL2CPP builds are slow; cached rebuilds are much faster.
- Do not send Telegram artifacts unless the latest user message explicitly requests it.

## APK Asset Invariants

- Unity Editor Play success is not enough. Android APK can fail when Addressables keys, labels, or runtime types are missing even though Editor asset references resolve.
- Before closing any image/UI/gameplay asset bug, statically compare JSON `.sprite` references and animator controller references against Addressables groups plus supported aliases.
- Title startup must preload AlwaysKeep/NeedRelease locations, typed `Sprite` assets, typed `RuntimeAnimatorController` assets, then initialize data and preload data-driven sprites.
- Data-driven sprite coverage must include stage, creature, skill, skill evolution, equipment, material, drop item, evolution, and gem data.

## Known Compatibility Keys

- Preserve aliases from `EnchanStone_*` to `EnchantStone_*`.
- Preserve `EnchanStone_Rign.sprite` to `EnchantStone_Ring.sprite`.
- Preserve `Grove_03.sprite` to `Glove_03.sprite`.
- Preserve `EquipmentBox_Random.sprite` and `EquipmentBox_AllRandom.sprite` to `EqptBox_Icon.sprite`.
- Keep `Normal_Potion.sprite`, `Good_Potion.sprite`, `Best_Potion.sprite`, `Exp.sprite`, and `EqptBox_Icon.sprite` in the `Sprites` Addressables group.
- Keep `Player_Beta_Anim`, `Player_Gamma_Anim`, `Player_Delta_Anim`, and `Player_Epsilon_Anim` in the `Anim` Addressables group.

## Runtime Validation

- Use Windows ADB for installed APK logs and screenshots.
- Verify `mCurrentFocus` is `com.Jiuk.SlimeSurvivor/com.unity3d.player.UnityPlayerActivity` before trusting screenshots.
- Logcat should be checked for `InvalidKeyException`, `Missing sprite`, `missing animator`, and `E/Unity` before claiming the APK is clean.
- For Android hit effects, prefer `SpriteRenderer` color flash. Do not swap to a custom damaged material unless shader/material inclusion is verified in the APK.
