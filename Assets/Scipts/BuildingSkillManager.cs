﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class BuildingSkillManager : SkillManager
{
    PositionInfo positionInfo;
    [SerializeField]
    List<GameObject> trainableUnits;
    public int testNum;

    public List<GameObject> TrainableUnits { get => trainableUnits; set => trainableUnits = value; }
    public PositionInfo PositionInfo { get => positionInfo; set => positionInfo = value; }

    public BuildingSkillManager(PositionInfo positionInfo)
    {
        this.PositionInfo = positionInfo;
    }

    public override void RegisterUICallback(SkillUIManager manager)
    {
        int i = 0;
        for(int j = 0; j < uIData.skills.Length; j++)
        {
            if (uIData.skills[j] != null)
            {
                if (i < TrainableUnits.Count)
                {
                    manager.RegisterClickCallback(j, UseSkill, i);
                    i++;
                }
            }
        }
    }

    public override void UseSkill(int index)
    {
        if (index < 0 || index >= TrainableUnits.Count) return;
        produce(index);
    }
    /// <summary>
    /// generate a unit immediately around the building
    /// </summary>
    /// <param name="index">the index of the unit in trainable unit list</param>
    public void produce(int index)
    {
        if (index < 0 || index > TrainableUnits.Count)
        {
            Debug.LogError(System.Reflection.MethodBase.GetCurrentMethod().Name + " :index out of range");
        }

        Vector2Int TargetGrid = GridSystem.current.getBlankGrid(new Vector2Int(PositionInfo.x, PositionInfo.z), PositionInfo.width, PositionInfo.height);
        Vector3 targetPosition = GridSystem.current.getWorldPosition(TargetGrid.x, TargetGrid.y);
        if (GridSystem.current.checkOccupation(TargetGrid.x, TargetGrid.y))
        {
            GameObject unit = GameObject.Instantiate(TrainableUnits[index], targetPosition, Quaternion.identity);
            Unit placeableComponent = unit.GetComponent<Unit>();
            //Can I update the grid date at another place?
            //GridSystem.current.setValue(TargetGrid.x, TargetGrid.y, 99, placeableComponent, placeableComponent.Size.x, placeableComponent.Size.y);
            placeableComponent.placeAt(TargetGrid.x, TargetGrid.y);

        }
    }
}
