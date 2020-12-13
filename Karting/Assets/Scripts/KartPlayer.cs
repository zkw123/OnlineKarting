﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
public enum DriftDirection
{
    None,
    Left,
    Right,
}
public enum DriftLevel
{
    One,
    Two,
    Three
}

[System.Obsolete]
public class KartPlayer : NetworkBehaviour
{
    
    public Rigidbody kartRigidbody;

    [Header("输入相关")]
    float v_Input;
    float h_Input;

    [Header("力的大小")]
    public float currentForce;
    public float normalForce = 80;  //通常状态力
    public float boostForce = 130;  //加速时力大小
    public float jumpForce = 10;    //跳时添加的力    
    public float gravity = 40;      //在空中时往下加的力

    //力的方向
    Vector3 forceDir_Horizontal;
    float verticalModified;         //前后修正系数

    [Header("转弯相关")]
    public bool isDrifting;
    public DriftDirection driftDirection = DriftDirection.None;
    [Tooltip("由h_Input以及漂移影响")]
    public Quaternion rotationStream;   //用于最终旋转
    public float turnSpeed = 60;
    
    //Drift()
    Quaternion m_DriftOffset = Quaternion.identity;
    public DriftLevel driftLevel;

    [Header("地面检测")]
    public Transform frontHitTrans;
    public Transform rearHitTrans;
    public Transform transform;
    public bool isGround;
    public bool isGroundLastFrame;
    public float groundDistance = 0.7f;//根据车模型自行调节

    [Header("特效")]
    public Transform wheelsParticeleTrans;
    public ParticleSystem[] wheelsParticeles;
    public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;
    [Header("漂移颜色有关")]
    public Color[] driftColors;
    public float driftPower = 0;

    [Header("联机有关")]
    public static bool GameStart = false;
    [SyncVar] public bool GameOver = false;
    [SyncVar] public bool PlayerIn = false;
    public GameFlowManager g;
    public PickupObject p;
    /*
    public class Send_data : NetworkManager
    {
        public override void OnServerConnect(NetworkConnection Conn)
        {
            if (Conn.hostId >= 0)
            {
                this.PlayerIn = true;
                GameFlowManager.playerin = true;
                Debug.Log("New Player has joined");
            }
        }
    }
    */
    void Start()
    {
        g = GameObject.Find("GameManager").GetComponent<GameFlowManager>();
        p = GameObject.Find("Checkpoint2").GetComponent<PickupObject>();
        forceDir_Horizontal = transform.forward;
        rotationStream = kartRigidbody.rotation;

        //漂移时车轮下粒子特效
        wheelsParticeles = wheelsParticeleTrans.GetComponentsInChildren<ParticleSystem>();
        StopDriftParticle();
    }
   /*
    private void OnPlayerConnected(NetworkPlayer player)
    {
        PlayerIn = true;
        GameFlowManager.playerin = true;
    }
   */
    void Update()
    {
        if (PlayerIn)
        {
            GameFlowManager.playerin = true;
        }
        if (!isLocalPlayer)
        {
            return;
        }
        if (p.t != null)
        {
            Debug.Log("GameOver");
            Debug.Log(p.t);
            GameOver = true;
            if (p.t == this.gameObject.transform.FindChild("KartCollider"))
            {
                Debug.Log("success");
                g.EndGame(true);
            }
            else
            {
                Debug.Log("fail");
                g.EndGame(false);
            }
        }
        if (NetworkServer.active)
        {
            List<NetworkIdentity> valueList = NetworkServer.objects.Values.ToList();
            int playerCount = valueList.Count(item => item.localPlayerAuthority);
            if(playerCount>=2)
            {
                PlayerIn = true;
                
            }
        }
       
        TimeManager.IsOver = GameOver;
        //输入相关
        if (GameStart)
        {
            v_Input = Input.GetAxisRaw("Vertical");     //竖直输入
            h_Input = Input.GetAxisRaw("Horizontal");   //水平输入
                                                        //按下空格起跳
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isGround)   //如果在地上
                {

                    Jump();
                }
            }

            //按住空格，并且有水平输入：开始漂移
            if (Input.GetKey(KeyCode.LeftShift) && h_Input != 0)
            {
                
                
                if (isGround && !isDrifting && kartRigidbody.velocity.sqrMagnitude > 5)
                {
                    StartDrift();   //开始漂移
                }
            }
            
            //放开空格：漂移结束
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                if (isDrifting)
                {
                    Boost(boostForce);//加速
                    StopDrift();//停止漂移
                }
            }
        }
    }

    private void FixedUpdate()
    {
        //车转向
        
        if (!isLocalPlayer)
            return;
        
        CheckGroundNormal();        //检测是否在地面上，并且使车与地面保持水平
        Turn();                     //输入控制左右转向

        //起步时力大小递增
        IncreaseForce();
        //漂移加速后/松开加油键力大小时递减
        ReduceForce();


        //如果在漂移
        if (isDrifting)
        {
            CalculateDriftingLevel();   //计算漂移等级
            ChangeDriftColor();         //根据漂移等级改变颜色
        }

        //根据上述情况，进行最终的旋转和加力
        kartRigidbody.MoveRotation(rotationStream);
        //计算力的方向
        CalculateForceDir();
        //移动
        AddForceToMove();
    }
 
    //计算加力方向
    public void CalculateForceDir()
    {
        //往前加力
        if (v_Input > 0)
        {
            verticalModified = 1;
        }
        else if (v_Input < 0)//往后加力
        {
            verticalModified = -1;
        }

        forceDir_Horizontal = m_DriftOffset * transform.forward;
    }
   
    //加力移动
    public void AddForceToMove()
    {
        //计算合力
        Vector3 tempForce = verticalModified * currentForce * forceDir_Horizontal;

        if (!isGround)  //如不在地上，则加重力
        {
            tempForce = tempForce + gravity * Vector3.down;
        }
        kartRigidbody.AddForce(tempForce, ForceMode.Force);
    }  

    //检测是否在地面上，并且使车与地面保持水平
    public void CheckGroundNormal()
    {
        //从车头中心附近往下打射线,长度比发射点到车底的距离长一点
        RaycastHit frontHit;
        bool hasFrontHit = Physics.Raycast(frontHitTrans.position, -transform.up, out frontHit, groundDistance, LayerMask.GetMask("Ground"));
        if (hasFrontHit)
        {
            Debug.DrawLine(frontHitTrans.position, frontHitTrans.position - transform.up * groundDistance, Color.red);
        }
        //从车尾中心附近往下打射线
        RaycastHit rearHit;
        bool hasRearHit = Physics.Raycast(rearHitTrans.position, -transform.up, out rearHit, groundDistance, LayerMask.GetMask("Ground"));
        if (hasRearHit)
        {
            Debug.DrawLine(rearHitTrans.position, rearHitTrans.position - transform.up * groundDistance, Color.red);
        }
        isGroundLastFrame = isGround;
        if (hasFrontHit || hasRearHit)//判断是否在地面
        {
            isGround = true;
        }
        else
        {
            isGround = false;
        }
        
        //使车与地面水平
        Vector3 tempNormal = (frontHit.normal + rearHit.normal).normalized;
        Quaternion quaternion = Quaternion.FromToRotation(transform.up, tempNormal);
        rotationStream = quaternion * rotationStream;
    }

    //力递减
    public void ReduceForce()
    {
        float targetForce = currentForce;
        if (isGround && v_Input == 0)
        {
            targetForce = 0;
        }
        else if (currentForce > normalForce)    //用于加速后回到普通状态
        {
            targetForce = normalForce;
        }

        if (currentForce <= normalForce)
        {
            DisableTrail();
        }
        currentForce = Mathf.MoveTowards(currentForce, targetForce,30 * Time.fixedDeltaTime);//每秒60递减，可调
    }

    //力递增
    public void IncreaseForce()
    {
        float targetForce = currentForce;
        if (v_Input != 0 && currentForce < normalForce)
        {
            currentForce = Mathf.MoveTowards(currentForce, normalForce, 60 * Time.fixedDeltaTime);//每秒80递增
        }
    }

    public void Turn()
    {
        //只能在移动时转弯
        if (kartRigidbody.velocity.sqrMagnitude <= 0.1)
        {
            return;
        }

        //漂移时自带转向
        if (driftDirection == DriftDirection.Left)
        {
            rotationStream = rotationStream * Quaternion.Euler(0, -40 * Time.fixedDeltaTime, 0);
        }
        else if (driftDirection == DriftDirection.Right)
        {
            rotationStream = rotationStream * Quaternion.Euler(0, 40 * Time.fixedDeltaTime, 0);
        }

        //后退时左右颠倒
        float modifiedSteering = Vector3.Dot(kartRigidbody.velocity, transform.forward) >= 0 ? h_Input : -h_Input;

        //输入可控转向：如果在漂移，可控角速度为30，否则平常状态为60.
        turnSpeed = driftDirection != DriftDirection.None ? 30 : 60;
        float turnAngle = modifiedSteering * turnSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0, turnAngle, 0);

        rotationStream = rotationStream * deltaRotation;//局部坐标下旋转,这里有空换一个简单的写法
    }

    public void Jump()
    {
        kartRigidbody.AddForce(jumpForce * transform.up, ForceMode.Impulse);
    }


    //开始漂移并且决定漂移朝向
    public void StartDrift()
    {
        Debug.Log("Start Drift");
        isDrifting = true;
        
        //根据水平输入决定漂移时车的朝向，因为合速度方向与车身方向不一致，所以为加力方向添加偏移
        if (h_Input < 0)
        {
            driftDirection = DriftDirection.Left;
            //左漂移时，合速度方向为车头朝向的右前方，偏移具体数值需结合实际自己调试
            m_DriftOffset = Quaternion.Euler(0f, 20, 0f);
        }
        else if (h_Input > 0)
        {
            driftDirection = DriftDirection.Right;
            m_DriftOffset = Quaternion.Euler(0f, -20, 0f);
        }

        //播放漂移粒子特效
        PlayDriftParticle();
    }

    //计算漂移等级
    public void CalculateDriftingLevel()
    {
        driftPower += Time.fixedDeltaTime;
        //0.7秒提升一个漂移等级
        if (driftPower < 0.7)   
        {
            driftLevel = DriftLevel.One;
        }
        else if (driftPower < 1.4)
        {
            driftLevel = DriftLevel.Two;
        }
        else
        {
            driftLevel = DriftLevel.Three;
        }
    }


    //停止漂移
    public void StopDrift()
    {
        isDrifting = false;
        driftDirection = DriftDirection.None;
        driftPower = 0;
        m_DriftOffset = Quaternion.identity;
        StopDriftParticle();
    }

    //加速
    public void Boost(float boostForce)
    {
        currentForce = (1 + (int)driftLevel / 5) * boostForce;
        EnableTrail();
    }

    //播放粒子特效
    public void PlayDriftParticle()
    {
        foreach (var tempParticle in wheelsParticeles)
        {
            tempParticle.Play();
        }
    }

    //粒子颜色随漂移等级改变
    public void ChangeDriftColor()
    {
        foreach (var tempParticle in wheelsParticeles)
        {
            var t = tempParticle.main;
            t.startColor = driftColors[(int)driftLevel];
        }
    }

    //停止播放粒子特效
    public void StopDriftParticle()
    {
        foreach (var tempParticle in wheelsParticeles)
        {
            tempParticle.Stop();
        }
    }

    public void EnableTrail()
    {
        leftTrail.enabled = true;
        rightTrail.enabled = true;
    }

    public void DisableTrail()
    {
        leftTrail.enabled = false;
        rightTrail.enabled = false;
    }
}
