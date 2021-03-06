using System;
using System.Collections.Generic;
using Grid;
using UnityEngine;

[RequireComponent(typeof(SelectionComponent))]
[RequireComponent(typeof(HealthBar))]
public abstract class Unit : MonoBehaviour, IUnit, IMoveable
{
    public static List<Unit> unitList;

    static Unit()
    {
        unitList = new List<Unit>();
    }

    public event Action onDeath;
    public event Action<float, float> onDamaged;

    
    protected bool isRotating = false;
    protected PositionInfo positionInfo;
    protected int nextX;
    protected int nextZ;
    protected Vector3 targetPosition;
    protected Vector3 nextPosition;
    protected Quaternion faceRotation;
    protected Transform modelTransform;
    protected StateManager state;
    protected TargetSelector ts;

    protected PathFinding pf;
    private float countDown = 0;
    private IUnit.dir faceDirection;
    private float maxSpeed = 3f;
    private bool isDead = false;
    private IPlaceableObj chasingObj;

    protected int teamNo;

    [SerializeField]
    protected CombatData defaultCombatData;
    protected CombatData currentCombatStatus;
    protected Animator animator;

    private void OnDestroy()
    {
        unitList.Remove(this);
    }
    protected void Awake()
    {
        unitList.Add(this);
        positionInfo = new PositionInfo();
        animator = transform.GetChild(0).GetComponent<Animator>();
        animator.SetBool("isRunning", false); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", false);
        targetPosition = transform.position;
        nextPosition = transform.position;
        modelTransform = transform.GetChild(0);
        faceRotation = modelTransform.rotation;
        pf = new PathFinding();
        
        currentCombatStatus = Instantiate<CombatData>(defaultCombatData);

        GetComponent<SelectionComponent>().enabled = false;

        InitializeStateManager();

        this.gameObject.tag = "Unit";
        this.gameObject.layer = 7;


        onDeath += (() => { 
            Debug.LogWarning("I am dead!!");
            isDead = true;
        });
    }

    public Vector2Int Position
    {
        get
        {
            return new Vector2Int(positionInfo.x, positionInfo.z);
        }
    }

    protected bool isObstacle;
    public bool IsObstacle {
        get
        {
            return isObstacle;
        }
        set
        {
            isObstacle = value;
        }
    }

    public Vector2Int Size {
        set
        {
            positionInfo.width = value.x;
            positionInfo.height = value.y;
        }
        get
        {
            return new Vector2Int(positionInfo.width, positionInfo.height);
        }
    }

    /// <summary>
    /// Check if targetPosition is occupied by other placeable object. 
    /// If yes, it find another blank grid and update the targetPosition and targetRotation.
    /// Note that it will not change the target position if it is chasing another object.
    /// </summary>
    void CheckTargetOccupationAndUpdate()
    {
        if(chasingObj != null)
        {
            return;
        }
        int x, z;
        GridSystem.current.getXZ(targetPosition, out x, out z);

        if (!GridSystem.current.checkOccupationExcept(x, z, positionInfo.width, positionInfo.height,this))
        {
            var blank = GridSystem.current.getBlankGrid(new Vector2Int(x, z), positionInfo.width, positionInfo.height);
            targetPosition = GridSystem.current.getWorldPosition(blank.x, blank.y);
        }
    }

    /// <summary>
    /// Change the TargetPosition.
    /// </summary>
    //TODO: minimum rotation.
    public void moveTo(Vector3 WorldPosition)
    {
        int x, z;
        GridSystem.current.getXZ(WorldPosition, out x, out z);
        targetPosition = GridSystem.current.getWorldPosition(x, z);

        if(!GridSystem.current.checkOccupationExcept(x, z, positionInfo.width, positionInfo.height,this))
        {
            var blank = GridSystem.current.getBlankGrid(new Vector2Int(x,z), positionInfo.width, positionInfo.height);
            targetPosition = GridSystem.current.getWorldPosition(blank.x, blank.y);
        }

        //faceDirection = getDir();
        //faceRotation = getDirRotation(faceDirection);
    }

    public void moveTo(int x, int z)
    {
        Vector3 targetPosition = GridSystem.current.getWorldPosition(x, z);
        moveTo(targetPosition);
    }

    

    public Animator Animator
    {
        get
        {
            return animator;
        }
    }

    public float PhysicalAttack {
        get
        {
            if(currentCombatStatus)return currentCombatStatus.physicalAttack;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.physicalAttack = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    //Note that you need to convert float hp to integer.
    public float HP
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.hp;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.hp = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float MagicAttack
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.magicAttack;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.magicAttack = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float PhysicalDefence
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.physicalDefence;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.physicalDefence = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float MagicDefence
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.magicDefence;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.magicDefence = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float Mana
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.mana;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.mana = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float AttackInterval
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.attackInterval;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.attackInterval = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public int AttackRange
    {
        get
        {
            if (currentCombatStatus) return currentCombatStatus.attackRange;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (currentCombatStatus)
                currentCombatStatus.attackRange = value;
            else
                Debug.LogError("combatData is null");
        }
    }

    public float MaxSpeed
    {
        get
        {
            return maxSpeed;
        }
    }

    public int TeamNo { 
        get => teamNo;
        set{
            ts.TeamNo = value;
            teamNo = value;
        }
    }

    public IPlaceableObj ChasingObj { get => chasingObj; set => chasingObj = value; }

    public bool placeAt(int x, int z)
    {
        bool isSuccess = GridSystem.current.setValue(x, z, 100, this, positionInfo.width, positionInfo.height);
        if (isSuccess)
        {
            Vector3 truePosition = GridSystem.current.getWorldPosition(x, z);
            transform.position = truePosition;

            GridSystem.current.removeValue(positionInfo.x, positionInfo.z, positionInfo.width, positionInfo.height);

            positionInfo.x = x;
            positionInfo.z = z;
            return true;
        }
        return false;

    }

    public bool placeAt(Vector3 worldPosition)
    {
        bool isSuccess = GridSystem.current.setValue(worldPosition, 100, this, positionInfo.width, positionInfo.height);
        if (isSuccess)
        {
            int x, z;
            GridSystem.current.getXZ(worldPosition, out x, out z);
            Vector3 truePosition = GridSystem.current.getWorldPosition(x, z);
            transform.position = truePosition;

            GridSystem.current.removeValue(positionInfo.x, positionInfo.z, positionInfo.width, positionInfo.height);
            positionInfo.x = x;
            positionInfo.z = z;
            return true;
        }
        return false;

    }

    // StateMachine configuration.
    private void InitializeStateManager()
    {
        state = new StateManager();
        state.addStatus("Idle", new State()
            .OnStart(() => { animator.SetBool("isRunning", false); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", false); Debug.Log("Idle onStart"); })
            .OnStay(() => {
                // Unless the unit have a target to attack, it will follow the chasingObj.
                if(ts.getTarget(positionInfo.x, positionInfo.z, faceDirection) == null && chasingObj != null)
                {
                    moveToChasingObj();
                }            
            })
            .OnExit(() => Debug.Log(gameObject.name+" exit idle"))
            .addCondition("Die",()=> isDead)
            .addCondition("Rotate", () => !faceRotation.Equals(modelTransform.rotation))
            //TODO: debug here
            // Two conditons for units to run:
            // 1, target position is different from current position, and target is within the range.
            // 2, target position is different from current position, and unit is not chasing the object.
            .addCondition("Run", () => !targetPosition.Equals(transform.position) && (ts.getTarget(positionInfo.x, positionInfo.z,faceDirection)==null||chasingObj==null))
            .addCondition("Attack", () => ts.getTarget(positionInfo.x, positionInfo.z,faceDirection) != null && countDown <= 0)
            );

        state.addStatus("Rotate", new State()
            .addCondition("Die", () => isDead)
            .addCondition("Idle", () => targetPosition.Equals(transform.position) && faceRotation.Equals(modelTransform.rotation))
            .addCondition("Run", () => faceRotation.Equals(modelTransform.rotation) && !targetPosition.Equals(transform.position))
            .OnStart(() => { animator.SetBool("isRunning", false); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", true); Debug.Log("rotate onStart"); })
            .OnStay(() =>
            {
                Quaternion tempRotation = Quaternion.RotateTowards(modelTransform.rotation, faceRotation, currentCombatStatus.rotateSpeed * Time.deltaTime);
                modelTransform.rotation = tempRotation;
            })
            .OnExit(() => { Debug.Log(gameObject.name+" Rotate exit"); })
            );
        state.addStatus("Run", new State()
            .addCondition("Die", () => isDead)
            .addCondition("Rotate", () => !faceRotation.Equals(modelTransform.rotation))
            .addCondition("Idle", () => targetPosition.Equals(transform.position))
            .addCondition("Attack", () => ts.getTarget(positionInfo.x, positionInfo.z, faceDirection) != null && countDown <= 0)
            .OnStart(() => { animator.SetBool("isRunning", true); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", false); })
            .OnStay(() =>
            {
                if (nextPosition.Equals(transform.position))
                {
                    // Unless the unit have a target to attack, it will follow the chasingObj.
                    if (ts.getTarget(positionInfo.x, positionInfo.z, faceDirection) == null && chasingObj != null)
                    {
                        moveToChasingObj();
                    }

                    CheckTargetOccupationAndUpdate();

                    // TODO: update this logic so that find path can be called less frequently.
                    int targetX, targetY;
                    GridSystem.current.getXZ(targetPosition, out targetX, out targetY);
                    List<GridData> path;

                    if(chasingObj != null)
                    {
                        path = pf.FindPath(positionInfo.x, positionInfo.z, targetX, targetY,false);
                    }
                    else
                    {
                         path = pf.FindPath(positionInfo.x, positionInfo.z, targetX, targetY,true);
                    }
                    

                    // Update moveDirection and targetRotation
                    faceDirection = UnitUtil.GetDirWithPF(path);
                    faceRotation = UnitUtil.getDirRotation(faceDirection);
                    updateNextPositonWithPF(faceDirection);
                    faceDirection = UnitUtil.getDir(new Vector2Int(positionInfo.x, positionInfo.z), new Vector2Int(nextX, nextZ));
                    GridSystem.current.setValue(nextX, nextZ, 1, this, positionInfo.width, positionInfo.height);
                }
                else
                {
                    MoveToNextPosition();
                    GridSystem.current.getXZ(transform.position, out positionInfo.x, out positionInfo.z);
                }
            })
            .OnExit(() => { Debug.Log(gameObject.name+" Run exit"); })
        );

        // TODO: Substitute it so that it is the attack animation duration.
        float maxTime = currentCombatStatus.attackInterval;
        float attackDuration = maxTime;
        state.addStatus("Attack", new State()
            // TODO: add animation stop
            .addCondition("Idle", () =>  attackDuration<0)
            //.addCondition("Run", () => !targetPosition.Equals(transform.position) && attackDuration < 0)
            .addCondition("Rotate", () => !faceRotation.Equals(modelTransform.rotation) && attackDuration < 0)
            .OnStart(() => { animator.SetBool("isRunning", false); animator.SetBool("isAttacking", true); animator.SetBool("isRotating", false); })
            .OnStay(() =>
            {
                //Debug.Log("Attack stay"+ AttackInterval);
                attackDuration -= Time.deltaTime;
            })
            .OnExit(() =>
            {
                animator.SetBool("isAttacking", false);
                countDown = AttackInterval;
                attackDuration = maxTime;
                 Debug.Log(gameObject.name + " Attack exit"); 
            }

            ));

        state.addStatus("Die", new State()
            .addCondition("Finish",() => animator.GetCurrentAnimatorStateInfo(0).IsName("Die")&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1)
            .OnStart(()=> {
                animator.SetBool("isDead", true);

            })
            .OnStay(()=> {
                //Debug.LogWarning(animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
                //Debug.LogWarning(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            })
            );
        state.addStatus("Finish", new State()
            .OnStart(() =>
            {
                gameObject.AddComponent<BodyCollapse>();
                GridSystem.current.removeValue(positionInfo.x, positionInfo.z);
                Destroy(this);
            })
            .OnStay(() => {
                transform.position = transform.position - 0.5f * Time.deltaTime * Vector3.up;
            })
            .OnExit(() => { 
                Debug.Log(gameObject.name + " Die exit"); 
            }
            ));
        state.Name = "Idle";
    }

    private void moveToChasingObj()
    {
        targetPosition = GridSystem.current.getWorldPosition(chasingObj.Position.x, chasingObj.Position.y);
    }

    private void updateNextPositon()
    {
        switch (getDir())
        {
            case IUnit.dir.forward:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x, positionInfo.z + 1, positionInfo.width, positionInfo.height,this))
                {
                    nextX = positionInfo.x;
                    nextZ = positionInfo.z + 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x, positionInfo.z + 1), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.right:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x + 1, positionInfo.z, positionInfo.width, positionInfo.height,this))
                {
                    nextX = positionInfo.x + 1;
                    nextZ = positionInfo.z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x + 1, positionInfo.z), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.backward:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x, positionInfo.z - 1, positionInfo.width, positionInfo.height,this))
                {
                    nextX = positionInfo.x;
                    nextZ = positionInfo.z - 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x, positionInfo.z - 1), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.left:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x - 1, positionInfo.z, positionInfo.width, positionInfo.height,this))
                {
                    nextX = positionInfo.x - 1;
                    nextZ = positionInfo.z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x - 1, positionInfo.z), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
        }
        nextPosition = GridSystem.current.getWorldPosition(nextX, nextZ);
    }

    private void updateNextPositonWithPF(IUnit.dir dir)
    {
        switch (dir)
        {
            case IUnit.dir.forward:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x, positionInfo.z + 1, positionInfo.width, positionInfo.height, this))
                {
                    nextX = positionInfo.x;
                    nextZ = positionInfo.z + 1;
                }
                else
                {
                    // If the unit is chasing for another object and is just one step away from the target position,
                    // the unit will not look for a blank neibour grid to stay at.
                    if (chasingObj != null)
                    {
                        int targetX, targetZ;
                        GridSystem.current.getXZ(targetPosition, out targetX, out targetZ);
                        if (Mathf.Abs(targetX - positionInfo.x) + Mathf.Abs(targetZ - positionInfo.z) <= 1)
                        {
                            return;
                        }
                    }
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x, positionInfo.z + 1), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.right:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x + 1, positionInfo.z, positionInfo.width, positionInfo.height, this))
                {
                    nextX = positionInfo.x + 1;
                    nextZ = positionInfo.z;
                }
                else
                {
                    // If the unit is chasing for another object and is just one step away from the target position,
                    // the unit will not look for a blank neibour grid to stay at.
                    if (chasingObj != null)
                    {
                        int targetX, targetZ;
                        GridSystem.current.getXZ(targetPosition, out targetX, out targetZ);
                        if (Mathf.Abs(targetX - positionInfo.x) + Mathf.Abs(targetZ - positionInfo.z) <= 1)
                        {
                            return;
                        }
                    }
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x + 1, positionInfo.z), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.backward:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x, positionInfo.z - 1, positionInfo.width, positionInfo.height, this))
                {
                    nextX = positionInfo.x;
                    nextZ = positionInfo.z - 1;
                }
                else
                {
                    // If the unit is chasing for another object and is just one step away from the target position,
                    // the unit will not look for a blank neibour grid to stay at.
                    if (chasingObj != null)
                    {
                        int targetX, targetZ;
                        GridSystem.current.getXZ(targetPosition, out targetX, out targetZ);
                        if (Mathf.Abs(targetX - positionInfo.x) + Mathf.Abs(targetZ - positionInfo.z) <= 1)
                        {
                            return;
                        }
                    }
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x, positionInfo.z - 1), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.left:
                if (GridSystem.current.checkOccupationExcept(positionInfo.x - 1, positionInfo.z, positionInfo.width, positionInfo.height, this))
                {
                    nextX = positionInfo.x - 1;
                    nextZ = positionInfo.z;
                }
                else
                {
                    // If the unit is chasing for another object and is just one step away from the target position,
                    // the unit will not look for a blank neibour grid to stay at.
                    if (chasingObj != null)
                    {
                        int targetX, targetZ;
                        GridSystem.current.getXZ(targetPosition, out targetX, out targetZ);
                        if (Mathf.Abs(targetX - positionInfo.x) + Mathf.Abs(targetZ - positionInfo.z) <= 1)
                        {
                            return;
                        }
                    }
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(positionInfo.x - 1, positionInfo.z), positionInfo.width, positionInfo.height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
        }
        nextPosition = GridSystem.current.getWorldPosition(nextX, nextZ);
    }

    // Update transform.position until it equals to NextPosition. It will keep remove Value and set value to update GridVal.
    private void MoveToNextPosition()
    {
        GridSystem.current.removeValue(transform.position, positionInfo.width, positionInfo.height);

        //Make sure the unit will finally arrive exactly at the next position.
        if ((nextPosition - transform.position).magnitude > Time.deltaTime * currentCombatStatus.moveSpeed)
        {
            transform.position += Time.deltaTime * currentCombatStatus.moveSpeed * UnitUtil.getDirVector(faceDirection);
        }
        else
        {
            transform.position = nextPosition;
            //isMoving = false;
        }
        GridSystem.current.setValue(transform.position, 10, this, positionInfo.width, positionInfo.height);
    }

    // Animation Callback function.
    public float DealDamage( IUnit.DamageType type)
    {
        //Find the target unit
        var target = ts.getTarget(positionInfo.x, positionInfo.z, faceDirection);
        float damage = -1;
        if(target != null)
        {
            //deal damage
            damage = target.receiveDamage(type, currentCombatStatus);
        }
        //return actual damage
        return damage;
    }

    public float receiveDamage(IUnit.DamageType type, CombatData from)
    {
        float damage = 0f;
        //calculate actual damage
        switch (type)
        {
            case IUnit.DamageType.Magic:
                damage = (1 - MagicDefence) * from.magicAttack;
                break;
            case IUnit.DamageType.Physical:
                damage =Mathf.Clamp(from.physicalAttack - PhysicalDefence,0,10000f);
                break;
        }
        //apply damage
        HP = Mathf.Clamp(HP - damage,0.0f,defaultCombatData.hp);
        onDamaged.Invoke(defaultCombatData.hp,HP);

        //if the damage is greater than hp, then the unit will die.
        if (HP.Equals(0.0f)) onDeath.Invoke();

        return damage;
    }

    private void Update()
    {
        countDown -= Time.deltaTime;

        state.onUpdate();
    }

    // Convert the real target direction to four target direction.
    // TODO: substitute it with Path finding algorithm.
     IUnit.dir getDir()
    {
        float zDeg = Vector3.Angle(Vector3.forward, targetPosition - transform.position);
        float xDeg = Vector3.Angle(Vector3.right, targetPosition - transform.position);

        if (xDeg < 90f)
        {
            if (zDeg < 45f)
            {
                return IUnit.dir.forward;
            }
            else if (zDeg < 135f)
            {
                return IUnit.dir.right;
            }
            else
            {
                return IUnit.dir.backward;
            }
        }
        else
        {
            if (zDeg < 45f)
            {
                return IUnit.dir.forward;
            }
            else if (zDeg < 135f)
            {
                return IUnit.dir.left;
            }
            else
            {
                return IUnit.dir.backward;
            }
        }
    }

    /// <summary>
    /// This method will change TeamNo to a different one. 0->1 1->0.
    /// If teamNo is not 1 or 0, it will set it to 0.
    /// </summary>
    public void changeTeamNo()
    {
        if(TeamNo==0)TeamNo=1;
        else if(TeamNo==1)TeamNo=0;
        else TeamNo=0;
    }
}
