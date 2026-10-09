# WB4 P2 — G4-RingConvert: FX bắn lượt 1 + Hold_Rocket sớm 1s
goal: Khi G4-RingConvert bắn lượt 1 (RandomSpreadShot) thì play Fx_gunshoot; effect tụ Hold_Rocket hiện sớm hơn hiện tại 1s (≈0.35s trước khi vòng hội tụ bắn, thay vì 0.65s sau).
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (§9 WB4 P2, §17 Mechanic Map, §15.6 bài học) + dò trực tiếp prefab/script

## T1 — Tạo helper DelayedActivateFX
owner: muse
depends: -
context:
Hold_Rocket hiện do UbhRingConvergeShot.IE_HoldFX bật: SetActive(false) → chờ holdFXDelay 0.65 → SetActive(true), chạy NGAY khi lượt 2 bắn. Không thể đặt delay âm trong lượt 2 → cần bật từ lượt 1.
Timeline hiện tại (WorldBossWeapon.IE_Sequence): t0 lượt 1 Attack → Ubh afterDelay 0.1 → ueEndShot (~t0.1) → delayAfterShot 1 → t≈1.1 lượt 2 bắn vòng → Hold_Rocket bật ở t≈1.75. Mục tiêu: t≈0.75 = ueEndShot lượt 1 + 0.65.
Helper: MonoBehaviour, field `GameObject target`, `[Min(0)] float delay`; `public void Play()` → StopAllCoroutines, coroutine: target.SetActive(false) → WaitForSeconds(delay) → target.SetActive(true). `public void Stop()` → StopAllCoroutines + target.SetActive(false). Component phải gắn trên GO KHÁC target (target tự tắt sẽ dừng coroutine).
verified:
UbhRingConvergeShot.cs:19-20 `[SerializeField] GameObject holdFX; [SerializeField, Min(0f)] float holdFXDelay = 0.65f;`
UbhRingConvergeShot.cs:79 `StartCoroutine(IE_HoldFX());` (cuối Update, cùng frame bắn vòng); :82-95 IE_HoldFX; `if (holdFX == null) yield break;`
WorldBossWeapon.cs:164-198 IE_Sequence: Attack → chờ isStepDone (OnEndShot) → delayAfterShot
BaseShotE.cs:154-159 OnStartShot invoke ueStartShot; :175-180 OnEndShot invoke ueEndShot + ActiveShotFX(false)
Không có class sẵn kiểu DelayActive/DelayEnable trong Assets (grep).
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/snapshots/INDEX.md
files: Assets/Project/WorldBoss/Use/Scripts/Enemies/WorldBoss-4/DelayedActivateFX.cs (mới)
done_when: compile 0 error; giữ CRLF nếu copy style từ file khác.

## T2 — Gắn Fx_gunshoot vào lượt 1
owner: claude
depends: -
context:
UbhShotE KHÔNG gọi OnBeingShot → mảng `shotFX` của BaseShotE không bao giờ bật với Ubh shot → không dùng shotFX. Dùng UnityEvent `ueStartShot` của RandomSpreadShot (useEvent 1 sẵn).
Fx_gunshoot đang active + playOnAwake → chỉ play 1 lần lúc boss spawn, không theo lượt bắn.
Việc: (1) tắt playOnAwake cho ParticleSystem root Fx_gunshoot + 3 con (Particle System (6)/(4)/(1)); (2) RandomSpreadShot.UbhShotE.ueStartShot thêm persistent call → Fx_gunshoot ParticleSystem.Play(bool=true). Sửa qua SerializedObject/UnityEventTools trong execute_code, rồi save prefab.
verified:
EnemyWB4_P2_A.prefab = Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P2_A.prefab
Path: EnemyWB4_P2_A/BodyRotate/G4-RingConvert/{Hold_Rocket, RingConvergeShot, Animation, RandomSpreadShot, Fx_gunshoot}
Fx_gunshoot GO fid 6814985140946450605 (line 37615), PS fid 8865046913122586159: looping 0, playOnAwake 1, lengthInSec 1, localPos (-0.53,-0.36)
RandomSpreadShot UbhShotE fid 8603413144246339447: useEvent 1, useInit 1, triggerAnimOnShot 1, ubhControl afterDelay 0.1; BoneFollower boneName "Ring" localPos (-0.50,-0.42)
G4-RingConvert WorldBossWeapon (fid 2169046535276770551) attackType 4 (SEQUENCE), shotSteps[0]=RandomSpreadShot delayAfterShot 1, [1]=RingConvergeShot delayAfterShot 0, isLoopSequence 1, sequenceCoolDown 3
refs: claude/worldboss-boss-library.md §15.6 (dựng bằng 1 lần execute_code, SerializedObject)
files: EnemyWB4_P2_A.prefab
done_when: Play: mỗi lần lượt 1 bắn thì Fx_gunshoot play (cả vòng lặp thứ 2); không play lúc spawn; Console 0 error.

## T3 — Dời Hold_Rocket sớm 1s
owner: claude
depends: T1
context:
Gắn DelayedActivateFX lên GO G4-RingConvert (không gắn lên Hold_Rocket): target = Hold_Rocket, delay = 0.65.
RandomSpreadShot.UbhShotE.ueEndShot thêm persistent call → DelayedActivateFX.Play().
UbhRingConvergeShot.holdFX = None (tránh bật lại lần 2 ở t≈1.75). Giữ nguyên holdFXDelay.
Hold_Rocket đang m_IsActive 1 + playOnAwake → play lúc spawn; đặt m_IsActive 0 trong prefab (helper sẽ bật).
verified:
Hold_Rocket GO fid 653333748320325856 (line 360), PS fid 3896587953704193899: looping 0, playOnAwake 1, lengthInSec 1; con: Particle System, Center, Smoke
RingConvergeShot UbhRingConvergeShot fid 2169046534002765449: holdFX → Hold_Rocket (line 20379), holdFXDelay 0.65, turn3Delay 1, bulletNum 30, ubh afterDelay 2
refs: claude/worldboss-boss-library.md §9, §17
files: EnemyWB4_P2_A.prefab
done_when: Hold_Rocket chỉ bật 1 lần/chu kỳ, ở ≈0.65s sau khi lượt 1 kết thúc (≈1.0s sớm hơn trước); Console 0 error.

## T4 — Test timeline trong Play
owner: claude
depends: T2, T3
context:
Test theo §15.6: vào Play → đọc Console ngay; probe EditorApplication.update log timestamp: lượt 1 start, Fx_gunshoot isPlaying, Hold_Rocket activeSelf, lượt 2 bắn vòng (ueEndShot/đếm đạn pool). Test weapon riêng: weapon.Init(dmg,cd) + weapon.Play() qua execute_code. Chạy ≥2 chu kỳ (isLoopSequence).
Kỳ vọng: Fx_gunshoot ≈ t0; Hold_Rocket ≈ t0+0.75; vòng bắn ≈ t0+1.1; burst ≈ t0+2.1.
verified:
Node test: Assets/Project/[GD-Khoa]/TestEnemyNode/WB/WB4_P2.prefab
refs: claude/worldboss-boss-library.md §15.6
files: - (chỉ test, không sửa)
done_when: log timestamp khớp kỳ vọng ±0.1s ở 2 chu kỳ; Console 0 error; báo Linh kết quả.

## Câu hỏi mở
- "Sớm hơn 1s" hiểu là so với thời điểm hiện tại (t≈1.75 → t≈0.75), tức Hold_Rocket hiện 0.35s TRƯỚC khi vòng bắn. Nếu ý là hiện sớm hơn so với lúc vòng bắn 1s (t≈0.1, ngay khi lượt 1 xong) thì đặt delay = 0.
- Fx_gunshoot không gắn BoneFollower (đặt tĩnh gần nòng); RandomSpreadShot đi theo bone "Ring". Nếu thấy FX lệch nòng khi anim chạy → chuyển Fx_gunshoot làm con của RandomSpreadShot?
