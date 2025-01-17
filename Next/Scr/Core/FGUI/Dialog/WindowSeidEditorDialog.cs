﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairyGUI;
using SkySwordKill.Next.FGUI.Component;
using SkySwordKill.NextFGUI.NextCore;
using SkySwordKill.NextModEditor.Mod;
using SkySwordKill.NextModEditor.Mod.Data;
using UnityEngine;

namespace SkySwordKill.Next.FGUI.Dialog;

public class WindowSeidEditorDialog : WindowDialogBase
{
    public class SeidNodeInfo
    {
        public bool IsSeid;
        public bool InSeidList;
        public string NodeName;
        public string NodeIcon;
        public int SeidID;
    }

    private WindowSeidEditorDialog()
        : base("NextCore", "WinSeidEditorDialog")
    {

    }

    public string Title { get; set; }
    public ModWorkshop Mod { get;private set; }
    public int OwnerId { get;private set; }
    public Dictionary<int, ModSeidMeta> SeidMetas { get;private set; }
    public IModSeidDataGroup SeidGroup { get;private set; }
    public List<int> SeidList { get;private set; }
    public List<int> AllSeidList { get;private set; }
    public Action OnClose { get;private set; }
    public bool Editable {
        get => _editable;
        set
        {
            _editable = value;
            Inspector.Editable = value;
            Inspector.Refresh();
            RefreshButtonState();
        }
    }

    public UI_WinSeidEditorDialog SeidEditor => contentPane as UI_WinSeidEditorDialog;
    public CtlPropertyInspector Inspector { get; set; }
    public GButton BtnAdd => SeidEditor.m_btnAdd;
    public GButton BtnRemove => SeidEditor.m_btnRemove;
    public GButton BtnEnable => SeidEditor.m_btnEnable;
    public GButton BtnDisable => SeidEditor.m_btnDisable;
    public GButton BtnMoveUp => SeidEditor.m_btnMoveUp;
    public GButton BtnMoveDown => SeidEditor.m_btnMoveDown;

    private int? CurrentSeidId { get; set; }
    private bool _editable;
    private GTreeNode _nodeSeidList;
    private GTreeNode _nodeAllSeidList;

    public static WindowSeidEditorDialog CreateDialog(string title,ModWorkshop mod,int ownerId, IModSeidDataGroup seidGroup,Dictionary<int, ModSeidMeta> seidMetas,
        List<int> seidList, Action onClose)
    {
        var window = new WindowSeidEditorDialog();

        window.modal = true;
        window.Title = title;
        window.Mod = mod;
        window.OwnerId = ownerId;
        window.SeidGroup = seidGroup;
        window.SeidMetas = seidMetas;
        window.SeidList = seidList;
        window.AllSeidList = new List<int>();
        window.OnClose = onClose;

        window.Show();

        return window;
    }

    protected override void OnInit()
    {
        base.OnInit();

        SeidEditor.m_frame.title = Title;
        SeidEditor.m_frame.m_closeButton.onClick.Add(Hide);

        Inspector = new CtlPropertyInspector(SeidEditor.m_inspector);


        foreach (var seid in SeidList)
        {
            if (!AllSeidList.Contains(seid))
            {
                AllSeidList.Add(seid);
            }
        }
        
        foreach (var pair in SeidGroup.DataGroups)
        {
            var seidId = pair.Key;
            var seid = SeidGroup.GetSeid(OwnerId, seidId);
            if (seid != null && !AllSeidList.Contains(seidId))
            {
                AllSeidList.Add(seidId);
            }
        }
        
        AllSeidList.Sort();

        SeidEditor.m_list.treeNodeRender = OnTreeNodeRender;
        SeidEditor.m_list.onClickItem.Add(OnClickSeidItem);

        BtnAdd.onClick.Add(OnClickAdd);
        BtnRemove.onClick.Add(OnClickRemove);
        BtnEnable.onClick.Add(OnClickEnable);
        BtnDisable.onClick.Add(OnClickDisable);
        BtnMoveUp.onClick.Add(OnClickMoveUp);
        BtnMoveDown.onClick.Add(OnClickMoveDown);

        Refresh();
        UnselectTargetSeid();
    }
    
    protected override void OnKeyDown(EventContext context)
    {
        base.OnKeyDown(context);
        
        if (context.inputEvent.keyCode == KeyCode.Escape)
        {
            Hide();
        }
    }

    private void Refresh()
    {
        SeidEditor.m_list.rootNode.RemoveChildren();
        _nodeSeidList = AddSeidList("特性列表", SeidList, true);
        _nodeAllSeidList = AddSeidList("所有特性", AllSeidList, false);
    }

    /// <summary>
    /// 获取选中的特性位置
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private bool GetSelectedSeid(out SeidNodeInfo info)
    {
        var node = SeidEditor.m_list.GetSelectedNode();
        info = null;

        // 非Seid节点不能删除
        if (!(node?.data is SeidNodeInfo nodeInfo) || !nodeInfo.IsSeid)
        {

            return false;
        }

        info = nodeInfo;
        return true;
    }
    
    /// <summary>
    /// 获取特性在SeidList里的位置，不存在则返回-1
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private bool GetSeidIndexInSeidList(out int index)
    {
        var node = SeidEditor.m_list.GetSelectedNode();
        index = -1;

        // 非Seid节点不能删除
        if (!(node?.data is SeidNodeInfo nodeInfo) || !nodeInfo.IsSeid)
        {

            return false;
        }

        index = _nodeSeidList.GetChildIndex(node);
        return true;
    }

    private void SelectSeid(int seidId)
    {
        foreach (var nodeList in new []{_nodeSeidList, _nodeAllSeidList})
        {
            for (int index = 0;index < nodeList.numChildren;index++)
            {
                var node = nodeList.GetChildAt(index);
                if(node.data is SeidNodeInfo nodeInfo && nodeInfo.IsSeid && nodeInfo.SeidID == seidId)
                {
                    SeidEditor.m_list.SelectNode(node);
                    SetTargetSeid(seidId);
                    return;
                }
            }
        }
        UnselectTargetSeid();
    }
    
    private void SelectSeidInSeidListByIndex(int index)
    {
        var node = _nodeSeidList.GetChildAt(index);
        if(node.data is SeidNodeInfo nodeInfo && nodeInfo.IsSeid)
        {
            SeidEditor.m_list.SelectNode(node);
            SetTargetSeid(nodeInfo.SeidID);
            return;
        }
        
        UnselectTargetSeid();
    }

    private void SelectSeidInAllSeidList(int seidId)
    {
        for (int i = 0; i < _nodeAllSeidList.numChildren; i++)
        {
            var node = _nodeAllSeidList.GetChildAt(i);
            if(node.data is SeidNodeInfo nodeInfo && nodeInfo.IsSeid && nodeInfo.SeidID == seidId)
            {
                SeidEditor.m_list.SelectNode(node);
                SetTargetSeid(nodeInfo.SeidID);
                return;
            }
        }
        
        UnselectTargetSeid();
    }

    private void OnClickAdd(EventContext context)
    {
        var metas = SeidMetas.Select(pair => (IModData)pair.Value).ToList();
        metas.ModSort();

        WindowTableSelectorDialog.CreateDialog("创建特性",
            new List<TableInfo>()
            {
                new TableInfo("ID", TableInfo.DEFAULT_GRID_WIDTH * 0.4f, obj => ((ModSeidMeta)obj).Id.ToString()),
                new TableInfo("特性名称", TableInfo.DEFAULT_GRID_WIDTH * 1.8f, obj => ((ModSeidMeta)obj).Name),
                new TableInfo("特性描述", TableInfo.DEFAULT_GRID_WIDTH * 2f, obj => ((ModSeidMeta)obj).Desc),
            },
            null,
            false,
            metas,
            false,
            ids =>
            {
                var seidId = ids[0];
                AddSeid(seidId);
                SelectSeid(seidId);
            });
    }

    private void OnClickRemove(EventContext context)
    {
        if (GetSelectedSeid(out var nodeInfo))
        {
            WindowConfirmDialog.CreateDialog("提示",$"即将完全删除特性【{nodeInfo.SeidID} {nodeInfo.NodeName}】，是否确认？", true,
                () =>
                {
                    RemoveSeid(nodeInfo.SeidID);
                    Refresh();
                });
        }
    }

    private void OnClickDisable(EventContext context)
    {
        if (GetSeidIndexInSeidList(out var index))
        {
            var seidId = SeidList[index];
            DisableSeidByIndex(index);
            SelectSeidInAllSeidList(seidId);
        }
    }

    private void OnClickEnable(EventContext context)
    {
        if (GetSelectedSeid(out var nodeInfo))
        {
            EnableSeid(nodeInfo.SeidID);
            SelectSeidInSeidListByIndex(SeidList.Count - 1);
        }
    }

    private void OnClickMoveUp(EventContext context)
    {
        if (GetSeidIndexInSeidList(out var index))
        {
            SelectSeidInSeidListByIndex(SeidMoveUpByIndex(index));
        }
    }

    private void OnClickMoveDown(EventContext context)
    {
        if (GetSeidIndexInSeidList(out var index))
        {
            SelectSeidInSeidListByIndex(SeidMoveDownByIndex(index));
        }
    }

    private void AddSeid(int seidId)
    {
        if(SeidList.Contains(seidId))
        {
            WindowConfirmDialog.CreateDialog("提示", "该特性已存在！是否继续添加？", true, () =>
            {
                AddSeidWithoutCheck(seidId);
            });
            return;
        }
        AddSeidWithoutCheck(seidId);
    }

    private void AddSeidWithoutCheck(int seidId)
    {
        SeidList.Add(seidId);
        if (!AllSeidList.Contains(seidId))
        {
            AllSeidList.Add(seidId);
            AllSeidList.Sort();
        }
        SeidGroup.GetOrCreateSeid(OwnerId, seidId);
        Refresh();
    }

    private void RemoveSeid(int seidId)
    {
        if (SeidList.Contains(seidId))
        {
            SeidList.RemoveAll(target => target == seidId);
        }

        if (AllSeidList.Contains(seidId))
        {
            AllSeidList.RemoveAll(target => target == seidId);
        }

        SeidGroup.RemoveSeid(OwnerId, seidId);
        Refresh();
    }

    private void EnableSeid(int seidId)
    {
        if(AllSeidList.Contains(seidId))
        {
            SeidList.Add(seidId);
        }
        Refresh();
    }

    private void DisableSeidByIndex(int index)
    {
        if (index >= 0 && index < SeidList.Count)
        {
            SeidList.RemoveAt(index);
            Refresh();
        }
    }

    private int SeidMoveUpByIndex(int index)
    {
        if(index == 0 || index >= SeidList.Count)
            return 0;
        var seidId = SeidList[index];
        SeidList.RemoveAt(index);
        SeidList.Insert(index - 1, seidId);
        Refresh();
        return index - 1;
    }

    private int SeidMoveDownByIndex(int index)
    {
        if(index < 0 || index == SeidList.Count - 1)
            return SeidList.Count - 1;
        var seidId = SeidList[index];
        SeidList.RemoveAt(index);
        SeidList.Insert(index + 1, seidId);
        Refresh();
        return index + 1;
    }

    protected override void OnHide()
    {
        OnClose();
        base.OnHide();
    }

    private void OnClickSeidItem(EventContext context)
    {
        var item = context.data as GObject;
        var node = item?.treeNode;


        if (node.data is SeidNodeInfo nodeInfo && nodeInfo.IsSeid)
        {
            SetTargetSeid(nodeInfo.SeidID);
        }
        else
        {
            UnselectTargetSeid();
        }
    }

    private void RefreshButtonState()
    {
        if (Editable)
        {
            BtnAdd.enabled = true;
            if (GetSelectedSeid(out var seidInfo))
            {
                BtnRemove.enabled = true;

                var inSeidList = seidInfo.InSeidList;
                
                BtnMoveUp.enabled = inSeidList;
                BtnMoveDown.enabled = inSeidList;
                BtnEnable.enabled = !inSeidList;
                BtnDisable.enabled = inSeidList;
            }
            else
            {
                BtnRemove.enabled = false;
                BtnMoveUp.enabled = false;
                BtnMoveDown.enabled = false;
                BtnEnable.enabled = false;
                BtnDisable.enabled = false;
            }
        }
        else
        {
            BtnAdd.enabled = false;
            BtnRemove.enabled = false;
            BtnMoveUp.enabled = false;
            BtnMoveDown.enabled = false;
            BtnEnable.enabled = false;
            BtnDisable.enabled = false;
        }
    }

    private void UnselectTargetSeid()
    {
        Inspector.Clear();
        CurrentSeidId = null;
        RefreshButtonState();
    }

    private void SetTargetSeid(int seidId)
    {
        Inspector.Clear();
        CurrentSeidId = seidId;
        RefreshButtonState();

        if(SeidMetas.TryGetValue(seidId,out var seidMeta))
        {
            Inspector.AddDrawer(new CtlTitleDrawer(seidMeta.Name));
            Inspector.AddDrawer(new CtlTextDrawer(seidMeta.Desc));

            var seidData = SeidGroup.GetOrCreateSeid(OwnerId, seidId);
            foreach (var seidProperty in seidMeta.Properties)
            {
                switch (seidProperty.Type)
                {
                    case ModSeidPropertyType.Int:
                    {
                        CreateIntDrawer(seidProperty, seidData);
                        break;
                    }
                    case ModSeidPropertyType.IntArray:
                    {
                        CreateIntArrayDrawer(seidProperty, seidData);
                        break;
                    }
                    case ModSeidPropertyType.Float:
                    {
                        CreateFloatDrawer(seidProperty, seidData);
                        break;
                    }
                    case ModSeidPropertyType.String:
                    {
                        CreateStringDrawer(seidProperty, seidData);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        Inspector.Refresh();
    }

    private void CreateIntDrawer(ModSeidProperty seidProperty, ModSeidData seidData)
    {
        CtlPropertyDrawerBase drawer;
        var sInt = seidData.GetToken<ModSInt>(seidProperty.ID);
        if (seidProperty.SpecialDrawer.Contains("BuffDrawer"))
        {
            var intPropertyDrawer = new CtlIntBindTablePropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value,
                buff =>
                {
                    var buffData = Mod.FindBuff(buff);
                    if (buffData != null)
                    {
                        return $"【{buff} {buffData.Name}】{buffData.Desc}";
                    }

                    return $"【{buff}  ？】";
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModBuffData)getData).Id.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModBuffData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModBuffData)getData).Desc),
                },
                () => new List<IModData>(Mod.GetAllBuffData(true)));
            drawer = intPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("SkillDrawer"))
        {
            var intPropertyDrawer = new CtlIntBindTablePropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value,
                value =>
                {
                    var skillData = Mod.FindSkill(value);
                    if (skillData != null)
                    {
                        return $"【{skillData.Id}({skillData.SkillPkId}) {skillData.Name}】{skillData.Desc}";
                    }

                    return $"【{value}(?)  ？】";
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Id.ToString()),
                    new TableInfo("神通唯一ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).SkillPkId.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModSkillData)getData).Desc),
                },
                () => new List<IModData>(Mod.GetAllSkillData(true)),
                modData => ((ModSkillData)modData).Id);
            drawer = intPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("SkillPKDrawer"))
        {
            var intPropertyDrawer = new CtlIntBindTablePropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value,
                value =>
                {
                    var skillData = Mod.FindSkillBySkillPkId(value);
                    if (skillData != null)
                    {
                        return $"【{skillData.Id}({skillData.SkillPkId}) {skillData.Name}】{skillData.Desc}";
                    }

                    return $"【?({value})  ？】";
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Id.ToString()),
                    new TableInfo("神通唯一ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).SkillPkId.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModSkillData)getData).Desc),
                },
                () => new List<IModData>(Mod
                    .GetAllSkillData(true)
                    .GroupBy(skillData => skillData.SkillPkId)
                    .Select(d =>
                        d.OrderByDescending(skill => skill.SkillLv).First())
                ),
                modData => ((ModSkillData)modData).SkillPkId);
            drawer = intPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("StaticSkillPKDrawer"))
        {
            var intPropertyDrawer = new CtlIntBindTablePropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value,
                value =>
                {
                    var staticSkillData = Mod.FindStaticSkillBySkillPkId(value);
                    if (staticSkillData != null)
                    {
                        return $"【{staticSkillData.Id}({staticSkillData.SkillPkId}) {staticSkillData.Name}】{staticSkillData.Desc}";
                    }

                    return $"【?({value})  ？】";
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModStaticSkillData)getData).Id.ToString()),
                    new TableInfo("功法唯一ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModStaticSkillData)getData).SkillPkId.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModStaticSkillData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModStaticSkillData)getData).Desc),
                },
                () => new List<IModData>(Mod
                    .GetAllStaticSkillData(true)
                    .GroupBy(skillData => skillData.SkillPkId)
                    .Select(d =>
                        d.OrderByDescending(skill => skill.SkillLv).First())
                ),
                modData => ((ModStaticSkillData)modData).SkillPkId);
            drawer = intPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("SeidDrawer"))
        {
            var intPropertyDrawer = new CtlIntBindTablePropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value,
                tagSeidId =>
                {
                    if (SeidGroup.DataGroups.TryGetValue(tagSeidId, out var tagSeidGroup))
                    {
                        var meta = tagSeidGroup.MetaData;
                        return $"【{tagSeidId} {meta.Name}】{meta.Desc}";
                    }

                    return $"【{tagSeidId}  ？】";
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSeidMeta)getData).Id.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSeidMeta)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModSeidMeta)getData).Desc),
                },
                () => new List<IModData>(
                    SeidGroup.DataGroups.Values.Select(seidDataGroup => seidDataGroup.MetaData)).ModSort());
            drawer = intPropertyDrawer;
        }
        else
        {
            var intPropertyDrawer = new CtlIntPropertyDrawer(seidProperty.Desc,
                value => sInt.Value = value,
                () => sInt.Value);
            drawer = intPropertyDrawer;
        }
        Inspector.AddDrawer(drawer);
        CreateIntExtraDrawer(drawer, seidProperty, sInt);
    }

    private void CreateIntExtraDrawer(CtlPropertyDrawerBase drawer, ModSeidProperty seidProperty, ModSInt sInt)
    {
        foreach (var drawerId in seidProperty.SpecialDrawer)
        {
            switch (drawerId)
            {
                case "SeidDrawer":
                case "SkillDrawer":
                case "SkillPKDrawer":
                case "StaticSkillPKDrawer":
                case "BuffDrawer":
                    continue;
                case "BuffTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.BuffDataBuffTypes.Select(type => $"{type.TypeID} {type.TypeName}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.BuffDataBuffTypes[index].TypeID;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.BuffDataBuffTypes.FindIndex(type => type.TypeID == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                case "LevelTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.LevelTypes.Select(type => $"{type.TypeID} {type.Desc}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.LevelTypes[index].TypeID;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.LevelTypes.FindIndex(type => type.TypeID == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                case "BuffRemoveTriggerTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.BuffDataRemoveTriggerTypes.Select(type =>
                            $"{type.TypeID} {type.TypeName}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.BuffDataRemoveTriggerTypes[index].TypeID;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.BuffDataRemoveTriggerTypes.FindIndex(type =>
                            type.TypeID == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                case "AttackTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.AttackTypes.Select(type =>
                            $"{type.Id} {type.Desc}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.AttackTypes[index].Id;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.AttackTypes.FindIndex(type =>
                            type.Id == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                case "ElementTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.ElementTypes.Select(type =>
                            $"{type.Id} {type.Desc}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.ElementTypes[index].Id;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.ElementTypes.FindIndex(type =>
                            type.Id == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                case "TargetTypeDrawer":
                {
                    var dropdownPropertyDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.TargetTypes.Select(type =>
                            $"{type.TypeID} {type.TypeName}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.TargetTypes[index].TypeID;
                            sInt.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.TargetTypes.FindIndex(type =>
                            type.TypeID == sInt.Value));
                    Inspector.AddDrawer(dropdownPropertyDrawer);
                    drawer.AddChainDrawer(dropdownPropertyDrawer);
                    break;
                }
                default:
                    Main.LogWarning($"未知的特殊绘制器 {drawerId}");
                    break;
            }
        }
    }

    private void CreateIntArrayDrawer(ModSeidProperty seidProperty, ModSeidData seidData)
    {
        CtlPropertyDrawerBase drawer;
        var sIntArray = seidData.GetToken<ModSIntArray>(seidProperty.ID);

        if (seidProperty.SpecialDrawer.Contains("BuffArrayDrawer"))
        {
            var intArrayPropertyDrawer = new CtlIntArrayBindTablePropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value,
                buffs =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < buffs.Count; index++)
                    {
                        var buff = buffs[index];
                        var buffData = Mod.FindBuff(buff);
                        if (buffData != null)
                        {
                            sb.Append($"【{buff} {buffData.Name}】{buffData.Desc}");
                        }
                        else
                        {
                            sb.Append($"【{buff}  ？】");
                        }
                        if(index != buffs.Count - 1)
                            sb.Append("\n");
                    }

                    return sb.ToString();
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModBuffData)getData).Id.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModBuffData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModBuffData)getData).Desc),
                },
                () => new List<IModData>(Mod.GetAllBuffData()));
            drawer = intArrayPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("SkillPkArrayDrawer"))
        {
            var intPropertyDrawer = new CtlIntArrayBindTablePropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value,
                value =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < value.Count; index++)
                    {
                        var skillId = value[index];
                        var skillData = Mod.FindSkillBySkillPkId(skillId);
                        if (skillData != null)
                        {
                            sb.Append($"【{skillData.Name}({skillData.SkillPkId})】{skillData.Desc}");
                        }
                        else
                        {
                            sb.Append($"【？({skillId})】");
                        }
                        if(index != value.Count - 1)
                            sb.Append("\n");
                    }

                    return sb.ToString();
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Id.ToString()),
                    new TableInfo("神通ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).SkillPkId.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSkillData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModSkillData)getData).Desc),
                },
                () => new List<IModData>(Mod
                    .GetAllSkillData(true)
                    .GroupBy(skillData => skillData.SkillPkId)
                    .Select(d =>
                        d.OrderByDescending(skill => skill.SkillLv).First())
                ),
                modData => ((ModSkillData)modData).SkillPkId);
            drawer = intPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("ItemArrayDrawer"))
        {
            var intArrayPropertyDrawer = new CtlIntArrayBindTablePropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value,
                items =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < items.Count; index++)
                    {
                        var item = items[index];
                        var itemData = Mod.FindItem(item);
                        if (itemData != null)
                        {
                            sb.Append($"【{item} {itemData.Name}】{itemData.Desc}");
                        }
                        else
                        {
                            sb.Append($"【{item}  ？】");
                        }
                        if(index != items.Count - 1)
                            sb.Append("\n");
                    }

                    return sb.ToString();
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModItemData)getData).Id.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModItemData)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModItemData)getData).Desc),
                },
                () => new List<IModData>(Mod.GetAllItemData()));
            drawer = intArrayPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("SeidArrayDrawer"))
        {
            var intArrayPropertyDrawer = new CtlIntArrayBindTablePropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value,
                seidList =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < seidList.Count; index++)
                    {
                        var tagSeidId = seidList[index];
                        if (SeidGroup.DataGroups.TryGetValue(tagSeidId, out var tagSeidGroup))
                        {
                            var meta = tagSeidGroup.MetaData;
                            sb.Append($"【{tagSeidId} {meta.Name}】{meta.Desc}");
                        }
                        else
                        {
                            sb.Append($"【{tagSeidId}  ？】");
                        }
                        if(index != seidList.Count - 1)
                            sb.Append("\n");
                    }

                    return sb.ToString();
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSeidMeta)getData).Id.ToString()),
                    new TableInfo("名称",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModSeidMeta)getData).Name),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModSeidMeta)getData).Desc),
                },
                () => new List<IModData>(
                    SeidGroup.DataGroups.Values.Select(seidDataGroup => seidDataGroup.MetaData)).ModSort());
            drawer = intArrayPropertyDrawer;
        }
        else if (seidProperty.SpecialDrawer.Contains("ElementTypeArrayDrawer"))
        {
            var intArrayPropertyDrawer = new CtlIntArrayBindTablePropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value,
                items =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < items.Count; index++)
                    {
                        var item = items[index];
                        var itemData = ModEditorManager.I.ElementTypes.Find(elementType => elementType.Id == item);
                        if (itemData != null)
                        {
                            sb.Append($"【{itemData.Id}】{itemData.Desc}");
                        }
                        else
                        {
                            sb.Append($"【{item}】？");
                        }
                        if(index != items.Count - 1)
                            sb.Append("\n");
                    }

                    return sb.ToString();
                },
                new List<TableInfo>()
                {
                    new TableInfo("ID",
                        TableInfo.DEFAULT_GRID_WIDTH,
                        getData => ((ModElementType)getData).Id.ToString()),
                    new TableInfo("描述",
                        TableInfo.DEFAULT_GRID_WIDTH * 2,
                        getData => ((ModElementType)getData).Desc),
                },
                () => new List<IModData>(ModEditorManager.I.ElementTypes));
            drawer = intArrayPropertyDrawer;
        }
        else
        {
            var intArrayPropertyDrawer = new CtlIntArrayPropertyDrawer(seidProperty.Desc,
                value => sIntArray.Value = value,
                () => sIntArray.Value);

            drawer = intArrayPropertyDrawer;
        }

        Inspector.AddDrawer(drawer);
        CreateIntArrayExtraDrawer(drawer, seidProperty, sIntArray);
    }

    private void CreateIntArrayExtraDrawer(CtlPropertyDrawerBase drawer, ModSeidProperty seidProperty,
        ModSIntArray sIntArray)
    {
        foreach (var drawerId in seidProperty.SpecialDrawer)
        {
            switch (drawerId)
            {
                case "ItemArrayDrawer":
                case "SeidArrayDrawer":
                case "SkillPkArrayDrawer":
                case "BuffArrayDrawer":
                case "ElementTypeArrayDrawer":
                    continue;
                default:
                    Main.LogWarning($"未知的特殊绘制器 {drawerId}");
                    break;
            }
        }
    }

    private void CreateStringDrawer(ModSeidProperty seidProperty, ModSeidData seidData)
    {
        CtlPropertyDrawerBase drawer;
        var sString = seidData.GetToken<ModSString>(seidProperty.ID);

        var stringPropertyDrawer = new CtlStringPropertyDrawer(seidProperty.Desc,
            value => sString.Value = value,
            () => sString.Value);
        Inspector.AddDrawer(stringPropertyDrawer);
        drawer = stringPropertyDrawer;
        CreateStringExtraDrawer(drawer, seidProperty, sString);
    }

    private void CreateStringExtraDrawer(CtlPropertyDrawerBase drawer, ModSeidProperty seidProperty, ModSString sString)
    {
        foreach (var drawerId in seidProperty.SpecialDrawer)
        {
            switch (drawerId)
            {
                case "ComparisonOperatorTypeDrawer":
                {
                    var buffTypeDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.ComparisonOperatorTypes.Select(type =>
                            $"{type.TypeStrID} {type.TypeName}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.ComparisonOperatorTypes[index].TypeStrID;
                            sString.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.ComparisonOperatorTypes.FindIndex(type =>
                            type.TypeStrID == sString.Value));
                    Inspector.AddDrawer(buffTypeDrawer);
                    drawer.AddChainDrawer(buffTypeDrawer);
                    break;
                }
                case "ArithmeticOperatorTypeDrawer":
                {
                    var buffTypeDrawer = new CtlDropdownPropertyDrawer("",
                        () => ModEditorManager.I.ArithmeticOperatorTypes.Select(type =>
                            $"{type.TypeStrID} {type.TypeName}"),
                        index =>
                        {
                            var typeId = ModEditorManager.I.ArithmeticOperatorTypes[index].TypeStrID;
                            sString.Value = typeId;
                            drawer.Refresh();
                        },
                        () => ModEditorManager.I.ArithmeticOperatorTypes.FindIndex(type =>
                            type.TypeStrID == sString.Value));
                    Inspector.AddDrawer(buffTypeDrawer);
                    drawer.AddChainDrawer(buffTypeDrawer);
                    break;
                }
                default:
                    Main.LogWarning($"未知的特殊绘制器 {drawerId}");
                    break;
            }
        }
    }

    private void CreateFloatDrawer(ModSeidProperty seidProperty, ModSeidData seidData)
    {
        CtlPropertyDrawerBase drawer;
        var sFloat = seidData.GetToken<ModSFloat>(seidProperty.ID);

        var floatPropertyDrawer = new CtlFloatPropertyDrawer(seidProperty.Desc,
            value => sFloat.Value = value,
            () => sFloat.Value);

        Inspector.AddDrawer(floatPropertyDrawer);
        drawer = floatPropertyDrawer;
    }

    private void OnTreeNodeRender(GTreeNode node, GComponent item)
    {
        var btn = item.asButton;
        var nodeData = (SeidNodeInfo)node.data;

        btn.title = nodeData.NodeName;
        btn.icon = nodeData.NodeIcon;
    }

    private GTreeNode AddSeidList(string listName, List<int> seidList, bool inSeidList)
    {
        var listData = new SeidNodeInfo()
        {
            IsSeid = false,
            NodeName = listName,
            NodeIcon = "",
        };

        var listNode = new GTreeNode(true)
        {
            data = listData
        };

        foreach (var seidId in seidList)
        {
            var seidData = new SeidNodeInfo()
            {
                IsSeid = true,
                InSeidList = inSeidList,
                NodeIcon = "ui://NextCore/icon_dao",
                SeidID = seidId,
            };

            if(SeidMetas.TryGetValue(seidId,out var seidMeta))
            {
                seidData.NodeName = $"{seidId} {seidMeta.Name}";
            }
            else
            {
                seidData.NodeName = $"{seidId}";
            }

            var seidNode = new GTreeNode(false)
            {
                data = seidData
            };
            
            listNode.AddChild(seidNode);
        }

        listNode.expanded = true;
        SeidEditor.m_list.rootNode.AddChild(listNode);
        return listNode;
    }
}