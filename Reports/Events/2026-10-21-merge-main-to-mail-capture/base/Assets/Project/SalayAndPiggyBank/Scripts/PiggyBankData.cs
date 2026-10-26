using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PiggyBank", menuName = "Data/PiggyBank")]
public class PiggyBankData : ScriptableObject
{
    const string ITEM_RESOURCE_FOLDER = "Data/PiggyBank";
    private static ResourceAsset<PiggyBankData> asset = new ResourceAsset<PiggyBankData>(ITEM_RESOURCE_FOLDER);

    public Data data;
    public static Data DataPiggyBanks => asset.Value.data;

    public static int Count() => PiggyBankData.DataPiggyBanks.PiggyLevels.Count;
    public static int GemPerEnemy(int _lv)
    {
        var _data = PiggyBankData.LevelData(_lv);
        return _data.GemsPerEnemy;
    }

    public static (int _notOpen, int _opened) FactorEnemySweepCollect(int _lv)
    {
        var _data = PiggyBankData.LevelData(_lv);
        return (_data.EnemyCollectSweepNotOpen, _data.EnemyCollectSweepOpened);
    }

    public static PiggyLevelData LevelData(int _lv)
    {
        var _lvData = DataPiggyBanks.PiggyLevels.FirstOrDefault(x => x.Level == _lv + 1);
        return _lvData ?? null;
    }

    [Serializable]
    public class Data
    {
        public List<PiggyLevelData> PiggyLevels = new();
    }

    [Serializable]
    public class PiggyLevelData
    {
        public int Level;
        public Sprite sprPiggy;
        public int MileStonesGem1;
        public int MileStonesGem2;
        public int EnemyCollectSweepNotOpen;
        public int EnemyCollectSweepOpened;
        public int GemsPerEnemy = 1;
        public IAP_Product Product;
    }
}