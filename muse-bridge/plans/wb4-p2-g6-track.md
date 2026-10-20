# WB4 P2 — G6-DamageZone: xoay cả thân + tâm ngắm bám trễ
goal: Súng 6 (G6-DamageZone) của EnemyWB4_P2_A khi track player thì xoay cả cụm BodyRotate; tâm ngắm AimingDamageZone đuổi theo player có độ trễ (chậm hơn player một chút), không dính cứng lên người player.
project: ~/wkspaces/WB
source: Project 1941 — claude/worldboss-boss-library.md (mục 9 WB4 P2, bảng đối chiếu P2 S6)

## T1 — AimingObject: thêm chế độ bám trễ (smooth follow)
owner: muse
depends: -
context:
Design S6: tia đỏ track player 1.5s (vòng tròn thu nhỏ) → 6 laser quay → nở ×3; CD 3. Yêu cầu mới: tâm ngắm đi chậm hơn player 1 tẹo, không gắn thẳng.
verified:
AimingObject.cs:4 public class AimingObject : MonoBehaviour (guid fbae3a5f6e510bc47b3b1e8948c3ccac, gắn trên AimingDamageZone.prefab)
AimingObject.cs:9 public virtual void Init(bool _followFighter=false, bool _followTarget=false, Transform _target=null, float _timeLive=0f, float _timeFollow=0f)
AimingObject.cs:23-26 Init đặt transform.position = targetTransform.position (snap lúc spawn)
AimingObject.cs:40-60 Update(): khi followTarget → transform.position = targetTransform.position (bám cứng mỗi frame)
Không có class nào kế thừa AimingObject; AimingObject dùng chung cho các shot khác → thay đổi phải mặc định giữ hành vi cũ.
refs: /Users/mobione/AGENTS/AI/Unity/knowledge/INDEX.md
files: Assets/Project/Enemies/Scripts/AimingObject.cs
done_when: Thêm serialize field mới (vd `followSmoothTime` hoặc `followSpeed`, mặc định 0 = bám cứng như cũ); khi >0 dùng Vector3.SmoothDamp/MoveTowards về target trong Update. Không đổi tên field/signature cũ. Compile 0 error.

## T2 — Bật bám trễ cho prefab AimingDamageZone
owner: claude
depends: T1
context:
Chỉ prefab tâm ngắm của G6 bật trễ; giá trị khởi điểm gợi ý smoothTime ~0.15–0.25s (chỉnh theo cảm giác, chờ Linh chốt).
verified:
AimingDamageZone.prefab (Assets/Project/GamePlay/Weapons/Prefabs/, guid e0b81bf7e97a644ad80604cb79940e0e) là aimingObjectPrefab của DamageZoneShot trong EnemyWB4_P2_A.prefab:20618
Prefab này chỉ được tham chiếu bởi EnemyWB4_P2_A.prefab (grep toàn Assets).
refs: claude/worldboss-boss-library.md §9
files: Assets/Project/GamePlay/Weapons/Prefabs/AimingDamageZone.prefab
done_when: Play scene test WB4_P2: tâm ngắm trôi theo player có độ trễ thấy được, không snap; vẫn ngừng bám sau timeFollow.

## T3 — G6 xoay cả cụm BodyRotate khi track
owner: claude
depends: -
context:
DamageZoneShotE gọi bodyRotate.Play() trong lúc aimingTime, Pause() sau đó và trong OnEndShot. Hiện bodyRotate trỏ nhầm vào RotationAimFighter trên chính GO DamageZoneShot → chỉ xoay điểm bắn, không xoay thân.
verified:
DamageZoneShotE.cs:13 [SerializeField] protected Rotation2D bodyRotate; :20 bodyRotate?.Play(); :23 & :41 bodyRotate?.Pause()
EnemyWB4_P2_A.prefab:20620 bodyRotate: {fileID: 2169046534229091765} = RotationAimFighter trên GO "DamageZoneShot" (speed 5, offsetAngle 0)
Hierarchy: EnemyWB4_P2_A > BodyRotate > G6-DamageZone > DamageZoneShot
GO "BodyRotate" có RotationAimFighter fileID 1200000000000000001 (speed 5, offsetAngle 90, isRotating 0)
RotationAimFighter.cs: Rotating() → transform.AutoRotationTarget(fighter.transform, speed, offsetAngle)
files: Assets/Project/WorldBoss/Addressable/Enemies/Prefabs/EnemyWB4_P2_A.prefab
done_when: DamageZoneShotE.bodyRotate trỏ tới RotationAimFighter của GO BodyRotate (fileID 1200000000000000001); bỏ/tắt RotationAimFighter thừa trên DamageZoneShot nếu không còn dùng. Play: trong 2.5s aiming cả cụm thân xoay theo player, sau đó dừng.

## T4 — Kiểm tra tổng hợp trong Play
owner: claude
depends: T2, T3
context:
Đo trong Play (không dùng số edit mode). Ghi kết quả vào claude/worldboss-boss-library.md bảng P2 S6.
files: (không sửa code) — cập nhật doc project
done_when: Console 0 error; G6 xoay thân theo player rồi dừng; tâm ngắm bám trễ; DamageZone spawn đúng vị trí tâm ngắm lúc kết thúc aim; G2/G4 vẫn bắn bình thường khi thân đã xoay.

## Câu hỏi mở
- BodyRotate là cha của cả G2-Closed, G4-RingConvert và G6 → xoay thân sẽ kéo theo hướng G2/G4. Có cần trả thân về góc 0 sau khi G6 bắn xong (hoặc khi G2/G4 bắn) không?
- Độ trễ tâm ngắm cụ thể bao nhiêu (smoothTime)? Và timeFollow hiện 2s, design ghi 1.5s — có sửa luôn không?
