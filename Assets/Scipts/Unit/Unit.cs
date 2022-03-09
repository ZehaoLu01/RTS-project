using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour, IUnit
{
    public Unit obj;

    public event Action onDeath;
    public event Action<float, float> onDamaged;

    protected float rotateSpeed = 180f;
    protected bool isMoving = false;
    protected bool isRotating = false;
    protected int x;
    protected int z;
    protected int nextX;
    protected int nextZ;
    protected int width;
    protected int height;
    protected Vector3 targetPosition;
    protected Vector3 nextPosition;
    protected float trueSpeed = 2.0f;
    protected Quaternion faceRotation;
    protected Transform modelTransform;
    protected StateManager state;
    protected TargetSelector ts = new TargetSelector();

    protected PathFinding pf;
    private float countDown = 0;
    private IUnit.dir faceDirection;
    private float maxSpeed = 3f;
    private bool isDead = false;

    public CombatData defaultCombatData;
    public CombatData combatData;

    protected void Start()
    {
        animator.SetBool("isRunning", false); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", false);
        targetPosition = transform.position;
        nextPosition = transform.position;
        modelTransform = transform.GetChild(0);
        faceRotation = modelTransform.rotation;
        pf = new PathFinding();
        InitializeStateManager();
        combatData = Instantiate<CombatData>(defaultCombatData);

        onDeath += (() => { 
            Debug.LogWarning("I am dead!!");
            isDead = true;
        });

    }

    public Vector2Int Position
    {
        get
        {
            return new Vector2Int(x, z);
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
            width = value.x;
            height = value.y;
        }
        get
        {
            return new Vector2Int(width, height);
        }
    }

    /// <summary>
    /// Check if targetPosition is occupied by other placeable object. 
    /// If yes, it find another blank grid and update the targetPosition and targetRotation.
    /// </summary>
    void CheckTargetOccupationAndUpdate()
    {
        int x, z;
        GridSystem.current.getXZ(targetPosition, out x, out z);

        if (!GridSystem.current.checkOccupationExcept(x, z, width, height,this))
        {
            var blank = GridSystem.current.getBlankGrid(new Vector2Int(x, z), width, height);
            targetPosition = GridSystem.current.getWorldPosition(blank.x, blank.y);
        }
    }

    //TODO: minimum rotation.
    public void moveTo(Vector3 WorldPosition, float speed)
    {
        int x, z;
        GridSystem.current.getXZ(WorldPosition, out x, out z);
        targetPosition = GridSystem.current.getWorldPosition(x, z);

        if(!GridSystem.current.checkOccupationExcept(x, z,width,height,this))
        {
            var blank = GridSystem.current.getBlankGrid(new Vector2Int(x,z),width,height);
            targetPosition = GridSystem.current.getWorldPosition(blank.x, blank.y);
        }

        //faceDirection = getDir();
        //faceRotation = getDirRotation(faceDirection);
    }

    public void moveTo(int x, int z, float speed)
    {
        Vector3 targetPosition = GridSystem.current.getWorldPosition(x, z);
        moveTo(targetPosition, speed);
    }

    protected Animator animator;

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
            if(combatData)return combatData.physicalAttack;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.physicalAttack = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    //Note that you need to convert float hp to integer.
    public float HP
    {
        get
        {
            if (combatData) return combatData.hp;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.hp = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float MagicAttack
    {
        get
        {
            if (combatData) return combatData.magicAttack;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.magicAttack = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float PhysicalDefence
    {
        get
        {
            if (combatData) return combatData.physicalDefence;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.physicalDefence = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float MagicDefence
    {
        get
        {
            if (combatData) return combatData.magicDefence;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.magicDefence = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float Mana
    {
        get
        {
            if (combatData) return combatData.mana;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.mana = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float AttackInterval
    {
        get
        {
            if (combatData) return combatData.attackInterval;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.attackInterval = value;
            else
                Debug.LogError("combatData is null");
        }
    }
    public float AttackRange
    {
        get
        {
            if (combatData) return combatData.attackRange;
            else
            {
                Debug.LogError("combatData is null");
                return 0;
            }
        }
        set
        {
            if (combatData)
                combatData.attackRange = value;
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

    public bool placeAt(int x, int z)
    {
        bool isSuccess = GridSystem.current.setValue(x, z, 100, this, width, height);
        if (isSuccess)
        {
            Vector3 truePosition = GridSystem.current.getWorldPosition(x, z);
            transform.position = truePosition;

            GridSystem.current.removeValue(this.x, this.z, width, height);

            this.x = x;
            this.z = z;
            return true;
        }
        return false;

    }

    public bool placeAt(Vector3 worldPosition)
    {
        bool isSuccess = GridSystem.current.setValue(worldPosition, 100, this, width, height);
        if (isSuccess)
        {
            int x, z;
            GridSystem.current.getXZ(worldPosition, out x, out z);
            Vector3 truePosition = GridSystem.current.getWorldPosition(x, z);
            transform.position = truePosition;

            GridSystem.current.removeValue(this.x, this.z, width, height);
            this.x = x;
            this.z = z;
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
            .OnStay(() => { })
            .OnExit(() => Debug.Log("exit idle"))
            .addCondition("Die",()=> isDead)
            .addCondition("Rotate", () => !faceRotation.Equals(modelTransform.rotation))
            .addCondition("Run", () => !targetPosition.Equals(transform.position))
            .addCondition("Attack", () => ts.getTarget(obj) != null && countDown <= 0)
            );

        state.addStatus("Rotate", new State()
            .addCondition("Die", () => isDead)
            .addCondition("Idle", () => targetPosition.Equals(transform.position) && faceRotation.Equals(modelTransform.rotation))
            .addCondition("Run", () => faceRotation.Equals(modelTransform.rotation) && !targetPosition.Equals(transform.position))
            .OnStart(() => { animator.SetBool("isRunning", false); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", true); Debug.Log("rotate onStart"); })
            .OnStay(() =>
            {
                Quaternion tempRotation = Quaternion.RotateTowards(modelTransform.rotation, faceRotation, rotateSpeed * Time.deltaTime);
                modelTransform.rotation = tempRotation;
            })
            .OnExit(() => { Debug.Log("Rotate exit"); })
            );
        state.addStatus("Run", new State()
            .addCondition("Die", () => isDead)
            .addCondition("Rotate", () => !faceRotation.Equals(modelTransform.rotation))
            .addCondition("Idle", () => targetPosition.Equals(transform.position))
            .addCondition("Attack", () => ts.getTarget(obj) != null && countDown <= 0)
            .OnStart(() => {  animator.SetBool("isRunning", true); animator.SetBool("isAttacking", false); animator.SetBool("isRotating", false); ; Debug.Log("run onStart"); })
            .OnStay(() =>
            {
                if (nextPosition.Equals(transform.position))
                {
                    CheckTargetOccupationAndUpdate();

                    // TODO: update this logic so that find path can be called less frequently.
                    int targetX, targetY;
                    GridSystem.current.getXZ(targetPosition, out targetX, out targetY);
                    var path = pf.FindPath(x, z, targetX, targetY);

                    // Update moveDirection and targetRotation
                    faceDirection = GetDirWithPF(path);
                    faceRotation = getDirRotation(faceDirection);
                    updateNextPositonWithPF(faceDirection);

                    GridSystem.current.setValue(nextX, nextZ, 1, this, width, height);
                }
                else
                {
                    MoveToNextPosition();
                    GridSystem.current.getXZ(transform.position, out this.x, out this.z);
                }
            })
            .OnExit(() => { Debug.Log("Run exit"); })
        );

        // TODO: Substitute it so that it is the attack animation duration.
        float maxTime = 1.0f;
        float attackDuration = maxTime;
        state.addStatus("Attack", new State()
            // TODO: add animation stop
            .addCondition("Idle", () => targetPosition.Equals(transform.position)&&attackDuration<0)
            .addCondition("Run", () => !targetPosition.Equals(transform.position) && attackDuration < 0)
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
            }

            ));

        state.addStatus("Die", new State()
            .addCondition("Finish",() => animator.GetCurrentAnimatorStateInfo(0).IsName("Die")&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1)
            .OnStart(()=> {
                animator.SetBool("isDead", true);

            })
            .OnStay(()=> {
                Debug.LogWarning(animator.GetCurrentAnimatorStateInfo(0).IsName("Die"));
                Debug.LogWarning(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            })
            );
        state.addStatus("Finish", new State()
            .OnStart(() =>
            {
                gameObject.AddComponent<BodyCollapse>();
                GridSystem.current.removeValue(x, z);
                Destroy(this);
            })
            .OnStay(()=>{
                transform.position = transform.position - 0.5f * Time.deltaTime * Vector3.up;
            }));
        
        state.Name = "Idle";
    }

    private void updateNextPositon()
    {
        switch (getDir())
        {
            case IUnit.dir.forward:
                if (GridSystem.current.checkOccupationExcept(x, z + 1, width, height,this))
                {
                    nextX = x;
                    nextZ = z + 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x, z + 1), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.right:
                if (GridSystem.current.checkOccupationExcept(x + 1, z, width, height,this))
                {
                    nextX = x + 1;
                    nextZ = z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x + 1, z), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.backward:
                if (GridSystem.current.checkOccupationExcept(x, z - 1, width, height,this))
                {
                    nextX = x;
                    nextZ = z - 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x, z - 1), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.left:
                if (GridSystem.current.checkOccupationExcept(x - 1, z, width, height,this))
                {
                    nextX = x - 1;
                    nextZ = z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x - 1, z), width, height);
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
                if (GridSystem.current.checkOccupationExcept(x, z + 1, width, height, this))
                {
                    nextX = x;
                    nextZ = z + 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x, z + 1), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.right:
                if (GridSystem.current.checkOccupationExcept(x + 1, z, width, height, this))
                {
                    nextX = x + 1;
                    nextZ = z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x + 1, z), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.backward:
                if (GridSystem.current.checkOccupationExcept(x, z - 1, width, height, this))
                {
                    nextX = x;
                    nextZ = z - 1;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x, z - 1), width, height);
                    nextX = newXZ.x;
                    nextZ = newXZ.y;
                }
                break;
            case IUnit.dir.left:
                if (GridSystem.current.checkOccupationExcept(x - 1, z, width, height, this))
                {
                    nextX = x - 1;
                    nextZ = z;
                }
                else
                {
                    var newXZ = GridSystem.gridSystem.getBlankGrid(new Vector2Int(x - 1, z), width, height);
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
        GridSystem.current.removeValue(transform.position, width, height);

        //Make sure the unit will finally arrive exactly at the next position.
        if ((nextPosition - transform.position).magnitude > Time.deltaTime * trueSpeed)
        {
            transform.position += Time.deltaTime * trueSpeed * getDirVector(faceDirection);
        }
        else
        {
            transform.position = nextPosition;
            isMoving = false;
        }
        GridSystem.current.setValue(transform.position, 10, this, width, height);
    }

    // Animation Callback function.
    public float DealDamage( IUnit.DamageType type)
    {
        //Find the target unit
        var target = ts.getTarget(obj);
        //deal damage
        target.receiveDamage(type, combatData);
        //return actual damage
        return 0;
    }

    public bool receiveDamage(IUnit.DamageType type, CombatData from)
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
        HP = Mathf.Clamp(HP - damage,0.0f,100.0f);
        onDamaged.Invoke(defaultCombatData.hp,HP);

        //if the damage is greater than hp, then the unit will die.
        if (HP.Equals(0.0f)) onDeath.Invoke();

        return true;
    }

    private void Update()
    {
        countDown -= Time.deltaTime;

        state.onUpdate();
    }

    // Convert direction to Rotation.
    private Quaternion getDirRotation(IUnit.dir dir)
    {
        switch (dir)
        {
            case IUnit.dir.forward:
                return Quaternion.LookRotation(Vector3.forward);
            case IUnit.dir.right:
                return Quaternion.LookRotation(Vector3.right);
            case IUnit.dir.backward:
                return Quaternion.LookRotation(Vector3.back);
            case IUnit.dir.left:
                return Quaternion.LookRotation(Vector3.left);
        }
        Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + ": no matching direction");
        return Quaternion.LookRotation(Vector3.forward);
    }

    // Get the normalized direction vector from the enum direction.
    private Vector3 getDirVector(IUnit.dir dir)
    {
        switch (dir)
        {
            case IUnit.dir.forward:
                return Vector3.forward;
            case IUnit.dir.right:
                return Vector3.right;
            case IUnit.dir.backward:
                return Vector3.back;
            case IUnit.dir.left:
                return Vector3.left;
        }
        Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + ": no matching direction");
        return Vector3.forward;
    }

    // Convert a vector(from->to ) to the four direction.
    IUnit.dir getDir(Vector2Int from, Vector2Int to) 
    {
        var fromPosition = GridSystem.current.getWorldPosition(from.x, from.y);
        var toPosition = GridSystem.current.getWorldPosition(to.x, to.y);

        float zDeg = Vector3.Angle(Vector3.forward, toPosition - fromPosition);
        float xDeg = Vector3.Angle(Vector3.right, toPosition - fromPosition);

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
            else if(zDeg<135f)
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

    private IUnit.dir GetDirWithPF(List<GridData>path)
    {
        // If the unit already reached the targetPosition.
        if (path.Count == 1) return IUnit.dir.forward;

        var diff = path[path.Count - 2].Position - path[path.Count-1].Position;
        Debug.Log($"path[path.Count - 2]{path[path.Count - 2].Position},path[path.Count-1]:{path[path.Count - 1].Position}");
        if (diff == new Vector2Int(0, 1)) return IUnit.dir.forward;
        else if (diff == new Vector2Int(0, -1)) return IUnit.dir.backward;
        else if (diff == new Vector2Int(1, 0)) return IUnit.dir.right;
        else if (diff == new Vector2Int(-1, 0)) return IUnit.dir.left;
        else
        {
            Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + $"diff vector does not have an expected value. diff:{diff}");
            return IUnit.dir.forward;
        }

    }

}
