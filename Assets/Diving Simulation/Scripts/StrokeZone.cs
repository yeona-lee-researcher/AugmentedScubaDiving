/*
 * HandSide는 공통 손 입력과 UI가 함께 사용하는 왼손/오른손 이름입니다.
 * 양손이 각각의 영역에 들어오면 SimpleMovement에 진입 시각을 알립니다. Scene에서 두 컴포넌트를 켭니다.
 * 추적을 다시 얻었을 때는 손이 영역 밖에 있음을 확인한 후에만 새 진입을 받습니다.
 * 여기서는 몸을 움직이지 않습니다. SimpleMovement가 두 입력을 짝지어 기존 Move 한 번에 추진을 합칩니다.
 */
using UnityEngine; // 원본 trigger와 컴포넌트를 사용합니다.

public enum HandSide // 0/1 숫자 대신 읽기 쉬운 손 이름을 정의합니다.
{
    Left, // 첫 값 0은 왼손입니다.
    Right // 다음 값 1은 오른손입니다.
}

[RequireComponent(typeof(Collider))] // 영역의 바깥을 검사할 충돌 모양이 필요합니다.
public class StrokeZone : MonoBehaviour // 원본 script GUID와 Scene 필드 연결을 보존합니다.
{
    public HandSide handSide; // 이 영역이 어느 손에 속하는지 표시합니다.
    public Collider targetHandCollider; // 원본 손 collider 참조입니다.
    public SimpleMovement movement; // 원본 이동 컴포넌트 참조입니다.
    Collider zoneCollider; // 이 수영 영역의 보이지 않는 상자입니다.
    HandColliderFollower handFollower; // 손의 추적 세대와 실제 collider 유효성을 읽습니다.
    int trackingVersion = -1; // 이 영역이 마지막으로 확인한 손 추적 세대입니다.
    bool armed; // 유효한 손이 영역 밖에 있음을 본 뒤에만 다음 진입을 허용합니다.

    void Awake() // 고정된 두 참조를 한 번만 찾습니다.
    {
        zoneCollider = GetComponent<Collider>(); // 같은 오브젝트의 BoxCollider입니다.
        if (targetHandCollider) handFollower = targetHandCollider.GetComponent<HandColliderFollower>(); // 지정된 손의 추종기를 찾습니다.
    }

    void Update() // 재추적 직후 영역 안에서 생기는 가짜 진입을 막습니다.
    {
        if (!zoneCollider || !zoneCollider.enabled || !targetHandCollider || !handFollower || !handFollower.HasTrackedCollider) // 실제 추적 중인 활성 collider만 허용합니다.
        {
            armed = false; // 추적이 없으면 과거의 진입 허가를 지웁니다.
            return; // 영역 바깥 판정도 하지 않습니다.
        }
        if (trackingVersion != handFollower.TrackingVersion) // 손이 처음 보이거나 다시 추적된 경우입니다.
        {
            trackingVersion = handFollower.TrackingVersion; // 새 추적 세대를 기록합니다.
            armed = false; // 새 세대는 먼저 영역 밖으로 나와야 합니다.
        }
        if (!zoneCollider.bounds.Intersects(targetHandCollider.bounds)) armed = true; // 세계 AABB가 겹치지 않으면 확실히 바깥이므로 다음 진입을 허용합니다.
    }

    void OnTriggerEnter(Collider other) // 손 충돌체가 이 영역에 새로 들어온 물리 프레임에 호출됩니다.
    {
        if (!isActiveAndEnabled || !movement || !targetHandCollider || other != targetHandCollider) return; // 꺼진 영역이나 다른 물체의 진입은 무시합니다.
        if (!handFollower || !handFollower.HasTrackedCollider || trackingVersion != handFollower.TrackingVersion || !armed) return; // 재추적 뒤 바깥을 확인하지 않은 진입은 무시합니다.
        armed = false; // 한 번 받은 진입은 다시 영역 밖으로 나갈 때까지 반복하지 않습니다.
        movement.NotifyStroke(handSide); // 허용된 지정 손의 진입만 이동 관리자에 알립니다.
    }

    void OnDisable() // 영역을 껐다 다시 켜도 이전 허가가 남지 않게 합니다.
    {
        armed = false; // 새로운 바깥 위치 확인을 요구합니다.
        trackingVersion = -1; // 손의 현재 추적 세대도 다음 Update에서 다시 확인합니다.
    }
}
