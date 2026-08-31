# Cinemachine 전환 가이드

현재 `SmoothFollowCamera`를 Cinemachine으로 전환하는 방법을 설명합니다.

## 왜 Cinemachine인가?

### 현재 구현 (SmoothFollowCamera)
- 기본 SmoothDamp 기반
- 수평/수직 차등 감쇠
- MVP에는 충분하지만 제한적

### Cinemachine의 이점
- **데드존**: 플레이어가 일정 범위 내에서 움직일 때 카메라 고정
- **화면 흔들림**: 타격, 착지 시 카메라 셰이크
- **Look Ahead**: 이동 방향 미리 보기
- **Composer**: 정교한 프레이밍 제어
- **우선순위**: 여러 카메라 간 전환
- **타임라인 통합**: 컷씬 제작 용이

## 설치

### 1. Package Manager
Window > Package Manager
- Packages: Unity Registry
- 검색: "Cinemachine"
- Install

### 2. 확인
Cinemachine 메뉴가 상단에 생성됨

## 기본 설정

### 1. Main Camera 설정

현재 `SmoothFollowCamera` 컴포넌트 제거:
```
Main Camera > Inspector > Remove Component: Smooth Follow Camera
```

Cinemachine Brain 추가:
```
Main Camera > Add Component > Cinemachine Brain
```

설정:
- Update Method: `Smart Update`
- Blend Update Method: `Late Update`
- Default Blend: `EaseInOut` (1초)

### 2. Virtual Camera 생성

Hierarchy:
```
Create > Cinemachine > Virtual Camera
```

이름: `CM_PlayerFollow`

#### Body (Follow 설정)
- Follow: `Player` 드래그
- Body: `Framing Transposer`
  - Camera Distance: `10`
  - Screen X: `0.5` (중앙)
  - Screen Y: `0.5` (중앙)
  - Dead Zone Width: `0.1` (수평 데드존)
  - Dead Zone Height: `0.1` (수직 데드존)
  - Soft Zone Width: `0.8`
  - Soft Zone Height: `0.8`
  - Damping: X=`0.12`, Y=`0.20` (현재 값과 동일)

#### Aim (Look At 설정)
- Aim: `Do Nothing` (플레이어만 따라가면 됨)

### 3. Orthographic 설정

Main Camera:
- Projection: `Orthographic`
- Size: `5`

## 고급 기능

### 데드존 (추천)

플레이어가 중앙에서 조금 움직일 때 카메라가 따라가지 않음:
- Dead Zone Width: `0.15`
- Dead Zone Height: `0.15`

### Look Ahead (선택)

이동 방향을 미리 보여줌:
- Body: `Framing Transposer`
- Lookahead Time: `0.3`
- Lookahead Smoothing: `10`

### 화면 흔들림 (타격/착지 시)

#### Impulse Source (플레이어에 추가)
```csharp
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    private CinemachineImpulseSource _impulseSource;
    
    private void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    // 착지 시
    public void OnLanding()
    {
        _impulseSource.GenerateImpulse(0.5f);
    }
    
    // 공격 시
    public void OnAttack()
    {
        _impulseSource.GenerateImpulse(0.3f);
    }
}
```

Player GameObject:
- Add Component: `Cinemachine Impulse Source`
  - Raw Signal: `6D Shake`
  - Amplitude: `1.0`
  - Frequency: `2.0`

CM_PlayerFollow:
- Add Extension: `Cinemachine Impulse Listener`
  - Gain: `1.0`

## 카메라 영역 제한 (Confiner)

맵 경계 내로 카메라 제한:

### 1. Polygon Collider 2D 생성
```
Hierarchy > Create Empty: CameraBounds
Add Component: Polygon Collider 2D
Is Trigger: true
```

맵 경계에 맞게 폴리곤 편집

### 2. Confiner Extension
```
CM_PlayerFollow > Add Extension: Cinemachine Confiner 2D
Bounding Shape 2D: CameraBounds (Polygon Collider 2D)
Damping: 0
```

## 여러 카메라 전환

### 전투 줌인 카메라 예시

```
Hierarchy > Create > Cinemachine > Virtual Camera: CM_BattleZoom
```

설정:
- Priority: `10` (기본은 10, 이게 더 높으면 우선)
- Body: `Framing Transposer`
  - Camera Distance: `7` (더 가까이)
  - Damping: 더 빠르게

코드로 전환:
```csharp
public CinemachineVirtualCamera normalCamera;  // Priority 10
public CinemachineVirtualCamera battleCamera;  // Priority 5

void StartBattle()
{
    battleCamera.Priority = 15; // 더 높게 설정
}

void EndBattle()
{
    battleCamera.Priority = 5;  // 원래대로
}
```

## 현재 SmoothFollowCamera와 비교

| 기능 | SmoothFollowCamera | Cinemachine |
|------|-------------------|-------------|
| 기본 추적 | ✅ | ✅ |
| 차등 감쇠 | ✅ | ✅ |
| 데드존 | ❌ | ✅ |
| Look Ahead | ❌ | ✅ |
| 화면 흔들림 | ❌ | ✅ |
| 경계 제한 | ❌ | ✅ |
| 여러 카메라 | ❌ | ✅ |
| 컷씬 통합 | ❌ | ✅ |

## 권장 설정 (카타나 제로 스타일)

```
CM_PlayerFollow:
  Body: Framing Transposer
    Camera Distance: 10
    Screen X: 0.5
    Screen Y: 0.5
    Dead Zone Width: 0.1
    Dead Zone Height: 0.08
    Soft Zone Width: 0.8
    Soft Zone Height: 0.6
    Damping X: 0.12
    Damping Y: 0.20
    Lookahead Time: 0.2
    Lookahead Smoothing: 8
  
  Aim: Do Nothing
  
  Extensions:
    - Cinemachine Impulse Listener (Gain: 0.8)
    - Cinemachine Confiner 2D (Bounding Shape: CameraBounds)
```

## 전환 체크리스트

- [ ] Cinemachine 패키지 설치
- [ ] Main Camera에서 SmoothFollowCamera 제거
- [ ] Cinemachine Brain 추가
- [ ] Virtual Camera 생성 및 설정
- [ ] 데드존/감쇠 값 조정
- [ ] (선택) Impulse Source 추가
- [ ] (선택) Confiner 설정
- [ ] 테스트 및 미세 조정

## 성능

Cinemachine은 최적화가 잘 되어 있어 성능 영향 미미:
- Virtual Camera: 약간의 추가 연산
- Brain: 프레임당 1회만 실행
- 모바일에서도 문제없음

## 마이그레이션 스크립트 (선택)

기존 카메라 설정을 자동으로 Cinemachine으로 변환:

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Cinemachine;

public class CameraMigrationTool : EditorWindow
{
    [MenuItem("Tools/Migrate to Cinemachine")]
    static void Migrate()
    {
        var mainCam = Camera.main;
        var oldCam = mainCam.GetComponent<SmoothFollowCamera>();
        
        if (oldCam == null)
        {
            Debug.LogError("SmoothFollowCamera not found!");
            return;
        }
        
        // Brain 추가
        var brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
        brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.SmartUpdate;
        
        // Virtual Camera 생성
        var vcamGO = new GameObject("CM_PlayerFollow");
        var vcam = vcamGO.AddComponent<CinemachineVirtualCamera>();
        
        // Follow 설정
        var transposer = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        transposer.m_XDamping = GameConstants.CAM_DAMP_X / 0.1f; // 대략 변환
        transposer.m_YDamping = GameConstants.CAM_DAMP_Y / 0.1f;
        transposer.m_CameraDistance = 10f;
        
        // 기존 컴포넌트 제거
        DestroyImmediate(oldCam);
        
        Debug.Log("Migration complete!");
    }
}
#endif
```

## 참고 자료

- [Cinemachine 공식 문서](https://docs.unity3d.com/Packages/com.unity.cinemachine@latest)
- [Cinemachine 튜토리얼](https://learn.unity.com/tutorial/cinemachine)
