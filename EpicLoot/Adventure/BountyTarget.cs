﻿using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Character))]
    public class BountyTarget : MonoBehaviour
    {
        public const string BountyTargetKey = "BountyTarget";
        public const string MonsterIDKey = "MonsterID";
        public const string IsAddKey = "IsAdd";

        private Character _character;
        private BountyInfo _bountyInfo;
        private string _monsterID;

        public void Awake()
        {
            _character = GetComponent<Character>();
            _character.m_onDeath += OnDeath;
        }

        public void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDeath -= OnDeath;
            }
        }

        private void OnDeath()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                var saveData = player.GetAdventureSaveData();
                if (saveData.GetBountyInfoByID(_bountyInfo.ID) != null && _bountyInfo.State == BountyState.InProgress)
                {
                    AdventureDataManager.Bounties.SlayBountyTarget(_bountyInfo, _monsterID);
                }
            }
        }

        public void Setup(BountyInfo bounty, string monsterID, bool isAdd)
        {
            _bountyInfo = bounty;
            _monsterID = monsterID;

            var zdo = _character.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                zdo.Set(BountyTargetKey, _bountyInfo.ID);
                zdo.Set(MonsterIDKey, monsterID);
                zdo.Set(IsAddKey, isAdd);
            }

            _character.m_name = isAdd ? $"{_character.m_name} Minion" : (string.IsNullOrEmpty(bounty.TargetName) ? _character.m_name : bounty.TargetName);
            _character.SetLevel(GetMonsterLevel(bounty, monsterID, isAdd));
            _character.m_baseAI.SetPatrolPoint();
            _character.m_boss = !isAdd;
        }

        private int GetMonsterLevel(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (isAdd)
            {
                foreach (var targetInfo in bounty.Adds)
                {
                    if (targetInfo.MonsterID == monsterID)
                    {
                        return targetInfo.Level;
                    }
                }

                return 1;
            }
            else
            {
                return bounty.Target.Level;
            }
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    public static class Character_Start_Patch
    {
        public static void Postfix(Character __instance)
        {
            var zdo = __instance.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                var bountyID = zdo.GetString(BountyTarget.BountyTargetKey);
                if (!string.IsNullOrEmpty(bountyID))
                {
                    var bountyInfo = Player.m_localPlayer?.GetAdventureSaveData().GetBountyInfoByID(bountyID);
                    if (bountyInfo != null)
                    {
                        var bountyTarget = __instance.gameObject.AddComponent<BountyTarget>();
                        var monsterID = zdo.GetString(BountyTarget.MonsterIDKey);
                        var isAdd = zdo.GetBool(BountyTarget.IsAddKey);
                        bountyTarget.Setup(bountyInfo, monsterID, isAdd);
                    }
                }
            }
        }
    }
}