# TowerGame

Unity DOTS/Entities 기반의 2D 타워 디펜스 프로토타입입니다.  
일반 GameObject 기반의 타워 조작 UX와 ECS 기반의 대량 적·투사체 시뮬레이션을 결합해, 모바일 환경에서 대규모 적 처리 구조를 검증하는 것을 목표로 합니다.

## 주요 기능

- 웨이브 데이터 기반 적 생성 및 사각 경로 이동
- `Fast`, `Normal`, `Tank` 적 아키타입과 웨이브별 형태 순환
- 공간 그리드 기반 타워 타겟 탐색
- 거리 제곱(`distancesq`) 기반 투사체 피격 판정
- 빨간 타워의 범위 피해, 파란 타워의 둔화, 보라 타워의 다중 타겟 발사
- 타워 배치, 드래그 이동, 같은 타입·등급 타워 합성, 판매 및 재화 처리
- 적 처치 보상, 처치 수, 현재 적 수, 웨이브, FPS UGUI 표시
- 적 체력 비율에 따라 색상과 채움량을 변경하는 Entity Graphics 셰이더
- 적·투사체·폭발·둔화 이펙트의 베이킹된 Entity Graphics 프리팹 렌더링

## 기술 구성

| 영역 | 구성 |
| --- | --- |
| Engine | Unity `6000.5.6f1` |
| Rendering | URP `17.6.0`, Entities Graphics `6.5.0` |
| Simulation | Unity Entities `6.5.0`, Burst, Job, ECB |
| Input/UI | Input System, UGUI |
| Mobile | Android Vulkan 기준 테스트 |

## 구조

```text
GameObject / UGUI
  └─ 타워 배치 · 드래그 · 합성 · 선택 패널 · HUD
       └─ TowerAttack
            └─ EntityCommandBuffer로 Projectile Entity 생성

DOTS / Entities
  ├─ EnemySpawnSystem / EnemyWaveSystem
  ├─ EnemyMovementSystem
  ├─ EnemySpatialGridSystem
  ├─ ProjectileMovementSystem
  ├─ ExplosionDamageSystem / SlowEffectSystem
  └─ EnemyDeathSystem

Entity Graphics
  └─ 베이킹된 Enemy / Projectile / Explosion / Slow Effect 프리팹
```

타워 수는 제한적이라는 게임 규칙을 반영해 타워 조작은 GameObject로 유지했습니다.  
대량으로 증가하는 적·투사체·피격·사망 처리는 ECS와 `EntityCommandBuffer`로 처리합니다.

## 렌더링 베이크 도구

Unity 메뉴에서 아래 항목을 실행하면 Entity Graphics에 필요한 시각 프리팹을 한 번에 생성·갱신합니다.

```text
Tools > DOTS > Generate All Baked Visuals
```

생성 대상:

- 적 메시와 `EnemyHealthFill` 머티리얼
- 투사체 프리팹
- 폭발·둔화 이펙트 프리팹
- 전투 비주얼 참조 프리팹

## 실행 방법

1. Unity Hub에서 Unity `6000.5.6f1`으로 프로젝트를 엽니다.
2. 필요하면 `Tools > DOTS > Generate All Baked Visuals`를 실행합니다.
3. `Assets/Scenes/Main.unity`를 열고 Play 합니다.
4. 성능 테스트는 `Assets/Scenes/StressTest.unity`를 사용합니다.

## 성능 검증 기록

적 5만 개, 타워 없음 조건에서 Android 실기기 빌드로 측정했습니다.

| 기기 | 결과 |
| --- | --- |
| Galaxy S25 Edge | 약 38 FPS |
| Galaxy Note9 | 약 19 FPS |

Unity Profiler에서 느린 프레임은 `Gfx.WaitForPresentOnGfxThread`, `Gfx.PresentFrame`, `RenderLoop` 비중이 높게 나타났습니다. 현재 5만 적 동시 표시 조건의 주 병목은 DOTS 이동·그리드보다 GPU 렌더링/표시 비용으로 판단됩니다.

## 다음 검토 항목

- 화면 내 적 밀도 제한 및 거리 기반 렌더링 LOD
- 체력 셰이더와 투명 오버드로우 경량화
- 저사양 기기용 목표 프레임 및 표시 수 정책
- 적 유형·웨이브 콘텐츠 확장

