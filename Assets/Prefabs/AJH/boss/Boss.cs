using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/****************************************************************************** 
 [ 코드 동작 방식 정리 ]
 - 스킬 및 이펙트는 StartBossCoroutine(stage1.IDangerStart(this), 10f) 이런식으로 재생할 코루틴과 종료시간을 같이 넘겨서 코루틴이 종료될 수 있게 처리
 - GimikAgain(); 기믹을 재실행 하는 함수
 - Danger 범위 지속시간은 DangerLine 프리팹 인스펙터에서 Time 값으로 설정

 [ ★ 씬 합치고 세팅해야 할 항목들 정리 ★ ]
 - 보스맵에서 벽으로 판탄되는 오브젝트에 Wall Layer를 추가해야 함 (이유는...뭐였지...? 아닐수도 있음)
 - public string[] playerTags3 = { "Player2", "Player3", "Player4","Player5" }; 여기 변수에 설정 된 태그에 맞게 하이어라키 플레이어 태그를 설정해야 함  
 - 보스맵 진입 시  [SerializeField] public bool bosssRoomStartCheck; true로 수정하는 로직 상의 후 넣어야함
  
****************************************************************************** */

/*  인터페이스  */
public interface IBossState
{
    void Enter(Boss boss);                                             // 상태에 진입할 때 호출되는 메서드
    void Execute(Boss boss);                                           // 상태가 활성화된 동안 매 프레임 호출되는 메서드
    void Exit(Boss boss);                                              // 상태에서 나갈 때 호출되는 메서드
}

/*  선언  */
public class Boss : MonoBehaviour
{
    [SerializeField] public bool bosssRoomStartCheck;

    public List<GameObject> players = new List<GameObject>();
    public string[] playerTags5 = { "Player2", "Player3", "Player4", "Player5" };

    private IBossState currentState;                                   // 현재 상태
    private IBossState previousState;                                  // 이전 상태

    public List<Vector3> setDangerPosition = new List<Vector3>();      // 기믹1 : 몬스터가 공격하는 장판 범위를 리스트에 넣어둠 (스킬 도 이 방향대로 나아가야 해서)
    public float Health { get; private set; }                          // 보스의 체력
    public float GimmickThreshold1 { get; private set; }               // 기믹 임계값 1
    public float GimmickThreshold2 { get; private set; }               // 기믹 임계값 2
    public float GimmickThreshold3 { get; private set; }               // 기믹 임계값 3
    private Animator animator;                                         // 애니메이터
    public bool IsUsingLaser { get; private set; }                     // 레이저 사용 여부

    private List<Coroutine> runningCoroutines = new List<Coroutine>(); // Running coroutine references!!!!

/*  초기화  */
    void Start()
    {
        bosssRoomStartCheck = false;                                   // 보스가 활동을 자동으로 하게 하지 않도록 초기화 
        Health = 100f;
        GimmickThreshold1 = 30f;                                       // 체력 임계값 설정
        GimmickThreshold2 = 50f;
        GimmickThreshold3 = 70f;
        animator = GetComponent<Animator>();
        ChangeState(new NoState());                                    // 초기 상태를 Normal 상태로 설정
    }

    void Update()
    {
        currentState?.Execute(this);                                   // 매 프레임 현재 상태의 Execute 메서드 실행
    }

    public void ChangeState(IBossState newState)
    {
        previousState = currentState;                                  // 이전 상태 저장
        currentState?.Exit(this);                                      // 현재 상태 종료
        currentState = newState;                                       // 새로운 상태 설정
        currentState.Enter(this);                                      // 새로운 상태 진입
    }
/*  보스 애니메이션 변경(공통)  */
    public void SetAnimation(string animationName)
    {
        animator.Play(animationName);                                  // 지정된 애니메이션 재생
    }
/*  애니메이션 이벤트 호출 함수(공통)  */
    public void OnGimmickAnimationEvent(string eventName)
    {
        if (eventName == "Stage1_start")
        {
            if (currentState is Stage1 stage1)
            {   // 1-1. 코루틴 실행을 위해 공통 코루틴 함수인 StartBossCoroutine으로 넘김
                StartBossCoroutine(stage1.IDangerStart(this), 10f);     // 코루틴 시작 및 2초 후 중지
            }
        }
        else if (eventName == "Stage1_end")
        {
            if (currentState is Stage1 stage1)
            {   // 2-1. 코루틴 실행을 위해 공통 코루틴 함수인 StartBossCoroutine으로 넘김
                StartBossCoroutine(stage1.IDangerEnd(this), 10f);       // 코루틴 시작 및 2초 후 중지
            }
        }
        else if (eventName == "StartLaser")
        {
            StartLaser(GameObject.FindGameObjectWithTag("Player").transform.position); // 레이저 시작
        }
        else if (eventName == "ThrowRock")
        {
            ThrowRock(GameObject.FindGameObjectWithTag("Player").transform.position); // 바위 던지기
        }
        else if (eventName == "AnimationComplete")
        {
            SetAnimation("Idle");                                        // 애니메이션 완료 후 Idle 애니메이션 설정
        }
    }

/*  코루틴(공통)  */
    public void StartBossCoroutine(IEnumerator coroutine, float stopAfterSeconds)
    {   // 여기서 시작시킴
        Coroutine startedCoroutine = StartCoroutine(coroutine);
        // 실행시킨 코루틴값? 배열에 추가시킴
        runningCoroutines.Add(startedCoroutine);
        // 위에서 실행시켰던 코루틴을 종료타이머 설정시킴
        StartCoroutine(StopCoroutineAfterDelay(startedCoroutine, stopAfterSeconds));
    }

    private IEnumerator StopCoroutineAfterDelay(Coroutine coroutine, float delay)
    {
        // 종료타이머 시간만큼 기다림
        yield return new WaitForSeconds(delay);
        // 코루틴 종료
        StopCoroutine(coroutine);
        runningCoroutines.Remove(coroutine);
        Debug.Log("Coroutine stopped after " + delay + " seconds");
    }

/*  보스 상태 체크 (공통)  */
    public void CheckHealthAndChangeState()
    {
        if (Health < GimmickThreshold1)
        {
            ChangeState(new GimmickState3()); // 가장 높은 임계값부터 체크
        }
        else if (Health < GimmickThreshold2)
        {
            ChangeState(new GimmickState2());
        }
        else if (Health < GimmickThreshold3)
        {
            ChangeState(new GimmickState1());
        }
    }

/*  기믹2 패턴  */
    #region Stage2
    public void StartLaser(Vector3 position)
    {
        Debug.Log("Starting Laser at Position: " + position);
        IsUsingLaser = true; // 레이저 사용 여부 설정
        // 레이저 시작 로직 구현
    }

    public void UpdateLaserPosition(Vector3 position)
    {
        if (IsUsingLaser)
        {
            Debug.Log("Updating Laser Position to: " + position);
            // 레이저 위치 업데이트 로직 구현
        }
    }

    public void FireLaser()
    {
        Debug.Log("Firing Laser"); // 레이저 발사 로그
        // 레이저 발사 로직 구현
    }

    public void GimikAgain()
    {
        Debug.Log("Stopping Laser");
        IsUsingLaser = false; // 레이저 사용 여부 해제
        // 레이저 중지 로직 구현

        // 현재 기믹을 다시 실행하기 위해 코루틴 시작
        StartCoroutine(WaitAndReExecuteGimmick());
    }

    private IEnumerator WaitAndReExecuteGimmick()
    {
        IBossState currentGimmickState = currentState; // 현재 상태 저장
        float waitTime = 10f; // 대기 시간 설정 (초 단위)
        Debug.Log("Waiting for " + waitTime + " seconds before re-executing the gimmick.");
        yield return new WaitForSeconds(waitTime); // 지정된 시간 대기

        if (currentState == currentGimmickState) // 상태가 변경되지 않았을 경우에만 현재 기믹 상태를 다시 실행
        {
            ChangeState(currentState); // 현재 상태로 다시 진입
        }
    }

    public void ThrowRock(Vector3 targetPosition)
    {
        Debug.Log("Throwing Rock at Position: " + targetPosition);
        // 바위 던지기 로직 구현
    }
    #endregion
}

/* 보스 대기 상태 */
public class NoState : IBossState
{
    public void Enter(Boss boss)
    {
        boss.SetAnimation("Idle1");
    }

    public void Execute(Boss boss)
    {
        if (boss.bosssRoomStartCheck) boss.ChangeState(new Stage1());
    }

    public void Exit(Boss boss)
    {
    }
}

/* 보스 Stage1 Class */
public class Stage1 : IBossState
{
    private GameObject activeDangerLine;

    public void Enter(Boss boss)
    {
        foreach (string tag in boss.playerTags5)
        {
            GameObject player = GameObject.FindGameObjectWithTag(tag);
            if (player != null)
            {
                boss.players.Add(player);
            }
        }
        boss.SetAnimation("Stage1"); // Idle 애니메이션 설정
    }

    public void Execute(Boss boss)
    {
        Debug.Log("test");
    }

    public void Exit(Boss boss)
    {
        Debug.Log("Exiting Normal State");
    }

    // 1-3 IDangerStart 코루틴 실행
    public IEnumerator IDangerStart(Boss boss)
    {
        yield return null;
        boss.transform.LookAt(boss.players[Random.Range(0, boss.players.Count)].transform);
        DangerLineStart(boss);
    }
    // 1-4 코루틴이 돌아가면 아래 함수가 실행 됨
    void DangerLineStart(Boss boss)
    {
        foreach (GameObject player in boss.players)
        {
            if (player != null)
            {
                GameObject activeDangerLine = PoolManager.Instance.GetPoolObject(PoolObjectType.DangerLine);
                DangerLine dangerLineComponent = activeDangerLine.GetComponent<DangerLine>();

                if (dangerLineComponent != null)
                {
                    Vector3 direction = (player.transform.position - boss.transform.position).normalized;
                    float extendLength = 5f;
                    Vector3 extendedEndPosition = player.transform.position + direction * extendLength;
                    boss.setDangerPosition.Add(extendedEndPosition);
                    dangerLineComponent.EndPosition = extendedEndPosition;
                    activeDangerLine.transform.position = boss.transform.position;
                    activeDangerLine.SetActive(true);
                    //여기서도 코루틴 종료시간 까지 넣어서 실행
                    boss.StartBossCoroutine(ReturnDangerLineToPool(activeDangerLine, dangerLineComponent.GetComponent<TrailRenderer>().time), dangerLineComponent.GetComponent<TrailRenderer>().time);
                }
            }
        }
    }

    public IEnumerator ReturnDangerLineToPool(GameObject dangerLine, float time)
    {
        yield return new WaitForSeconds(time);
        PoolManager.Instance.CoolObject(dangerLine, PoolObjectType.DangerLine);
        dangerLine.SetActive(false);
        DangerLine dangerLineComponent = dangerLine.GetComponent<DangerLine>();
        if (dangerLineComponent != null)
        {
            dangerLineComponent.cleartr(); 
        }
    }

    public IEnumerator IDangerEnd(Boss boss)
    {
        DangerLineEnd(boss);
        yield return null;
    }

    void DangerLineEnd(Boss boss)
    {
        foreach (Vector3 setDangerPositions in boss.setDangerPosition)
        {
            if (setDangerPositions != null)
            {
                GameObject activeDangerAttack = PoolManager.Instance.GetPoolObject(PoolObjectType.DangerAttack);
                activeDangerAttack.transform.position = boss.transform.position;
                activeDangerAttack.transform.LookAt(setDangerPositions);
                Vector3 eulerAngles = activeDangerAttack.transform.rotation.eulerAngles;
                eulerAngles.x = 0;
                activeDangerAttack.transform.rotation = Quaternion.Euler(eulerAngles);
                activeDangerAttack.SetActive(true);
            }
        }
    }
}

/* 보스 실행On Stage2 */
public class GimmickState1 : IBossState
{
    private Transform playerTransform; // 플레이어의 Transform

    public void Enter(Boss boss)
    {
        Debug.Log("Entering Gimmick State 1");
        boss.SetAnimation("Gimmick1"); // Gimmick1 애니메이션 설정
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; // 플레이어의 Transform 가져오기
    }

    public void Execute(Boss boss)
    {
        // 실시간으로 플레이어의 위치를 추적하고 필요 시 레이저 발사 계속
        if (boss.IsUsingLaser)
        {
            boss.UpdateLaserPosition(playerTransform.position); // 레이저 위치 업데이트
            boss.FireLaser(); // 레이저 발사
        }
    }

    public void Exit(Boss boss)
    {
        Debug.Log("Exiting Gimmick State 1");
        boss.GimikAgain(); // 상태 종료 시 레이저 중지
    }
}

/* 보스 실행On Stage3 */
public class GimmickState2 : IBossState
{
    private Transform playerTransform; // 플레이어의 Transform

    public void Enter(Boss boss)
    {
        Debug.Log("Entering Gimmick State 2");
        boss.SetAnimation("Gimmick2"); // Gimmick2 애니메이션 설정
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; // 플레이어의 Transform 가져오기
    }

    public void Execute(Boss boss)
    {
        // 필요 시 추가 로직 추가
    }

    public void Exit(Boss boss)
    {
        Debug.Log("Exiting Gimmick State 2");
        boss.GimikAgain(); // 상태 종료 시 레이저 중지
    }
}

/* 보스 실행On Stage4 */
public class GimmickState3 : IBossState
{
    private Transform playerTransform; // 플레이어의 Transform

    public void Enter(Boss boss)
    {
        Debug.Log("Entering Gimmick State 3");
        boss.SetAnimation("Gimmick3"); // Gimmick3 애니메이션 설정
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform; // 플레이어의 Transform 가져오기
    }

    public void Execute(Boss boss)
    {
        // 필요 시 추가 로직 추가
    }

    public void Exit(Boss boss)
    {
        Debug.Log("Exiting Gimmick State 3");
        boss.GimikAgain(); // 상태 종료 시 레이저 중지
    }
}