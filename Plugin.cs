using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.UnityEngine;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Rendering.Universal.LibTessDotNet;
using static Il2CppSystem.Globalization.TimeSpanFormat;
using static UnityEngine.UIElements.DefaultEventSystem;
using static CreateBullet;
using static InitZombieList;
using static Board;
using System.Text.Json;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;


namespace modMenu;

[BepInPlugin("modMenu", "modMenu", "2.1.5")]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    public static string keep;
    public static string myPath;

    


    public unsafe static void CopyToIl2CppArray(bool[] standardArray, Il2CppStructArray<bool> il2cppArray)
    {
        // 确保两个数组长度一致
        if (standardArray.Length != il2cppArray.Length)
        {
            return;
        }

        // 遍历标准数组，将值复制到 Il2CppStructArray 中
        for (int i = 0; i < standardArray.Length; i++)
        {
            il2cppArray[i] = standardArray[i];
        }
    }

    public static void SaveBoardTag()
    {
        string result = "";
        result += "# 胆小菇之梦\n是否启用：false\n";
        result += "# 塔防\n是否启用：false\n";
        result += "# 种子雨\n是否启用：false\n";
        result += "# 坚不可摧\n是否启用：false\n";
        result += "# 排山倒海\n是否启用：false\n";
        result += "# 超级随机\n是否启用：false\n";
        result += "# 夜晚\n是否启用：false\n";
        result += "# 大地图\n是否启用：false\n";
        result += "# 无尽\n是否启用：false\n";
        result += "# 允许旅行植物\n是否启用：false\n";
        result += "# 允许旅行buff\n是否启用：false\n";
        result += "# 屋顶\n是否启用：false\n";
        string path = Path.Combine(myPath, "Board.txt");
        File.WriteAllText(path, result);
    }

    public static void LoadBoardTag()
    {
        // 文件路径
        string path = Path.Combine(myPath, "Board.txt");
        string content = File.ReadAllText(path);

        // 正则表达式匹配 "是否启用：true" 或 "是否启用：false"
        Regex regex = new Regex(@"是否启用：(true|false)");

        // 查找所有匹配的布尔值
        MatchCollection matches = regex.Matches(content);

        // 创建一个布尔数组来存储结果
        bool[] boolArray = new bool[matches.Count];

        // 遍历匹配结果并解析为 bool 值
        for (int i = 0; i < matches.Count; i++)
        {
            boolArray[i] = bool.Parse(matches[i].Groups[1].Value);
        }
        BoardTag tag = new BoardTag();
        tag.isScaredyDream = boolArray[0];
        tag.isTowerDefence = boolArray[1];
        tag.isSeedRain = boolArray[2];
        tag.isIndestructible = boolArray[3];
        tag.isColumn = boolArray[4];
        tag.isSuperRandom = boolArray[5];
        tag.isNight = boolArray[6];
        tag.isBigMap = boolArray[7];
        tag.isEndless = boolArray[8];
        tag.enableTravelPlant = boolArray[9];
        tag.enableTravelBuff = boolArray[10];
        tag.isRoof = boolArray[11];
        Board.Instance.boardTag = tag;
    }
    public static bool[] GetEnabledStatusArray(string filePath)
    {
        // 读取文件的所有行
        string[] lines = File.ReadAllLines(filePath);
        int count = 0;

        // 先计算启用状态的数量
        foreach (string line in lines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue; // 跳过注释行和空行

            if (line.Contains("是否启用："))
            {
                count++; // 计数启用状态的数量
            }
        }

        // 创建一个布尔数组以存储启用状态
        bool[] enabledStatus = new bool[count];
        int index = 0;

        // 重新遍历文件行以填充布尔数组
        foreach (string line in lines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue; // 跳过注释行和空行

            if (line.Contains("是否启用："))
            {
                // 提取启用状态并转换为布尔值
                string status = line.Split(new[] { "是否启用：" }, System.StringSplitOptions.None)[1].Trim();
                enabledStatus[index++] = status.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }
        }

        return enabledStatus;
    }

    public static void ExportDictionaryToTxt(Dictionary<int, string> dictionary, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var kvp in dictionary)
            {
                // 将键值对写入文本文件，格式为 "key,value"
                writer.WriteLine($"{kvp.Key},{kvp.Value}");
            }
        }
    }

    public static void WriteBuffsToFile()
    {
        TravelMgr travelMgr = ComponentHolderProtocol.GetOrAddComponent<TravelMgr>(GameObject.Find("GameAPP"));
        int i = 0;
        string result = "";

        // 遍历 advancedUpgrades
        for (; i < travelMgr.advancedUpgrades.Length; i++)
        {
            if (TravelMgr.advancedBuffs.TryGetValue(i, out string value))
            {
                result += "# " + value + "\n" + "是否启用：" + "false" + "\n";
            }
        }

        // 遍历 ultimateUpgrades
        for (int j = 0; j < travelMgr.ultimateUpgrades.Length; j++, i++)
        {
            if (TravelMgr.ultimateBuffs.TryGetValue(j, out string value))
            {
                result += "# " + value + "\n" + "是否启用：" + "false" + "\n";
            }
        }
        File.WriteAllText(Path.Combine(myPath, "buffs.txt"), result);
    }

    public static void ReadBuffsToGame()
    {
        TravelMgr travelMgr = ComponentHolderProtocol.GetOrAddComponent<TravelMgr>(GameObject.Find("GameAPP"));
        bool[] a = GetEnabledStatusArray(Path.Combine(myPath, "buffs.txt"));
        UnityEngine.Debug.Log(a.Length);
        bool[] b = new bool[travelMgr.advancedUpgrades.Length];
        bool[] c = new bool[travelMgr.ultimateUpgrades.Length];
        int i = 0;
        for (; i < travelMgr.advancedUpgrades.Length; i++)
        {
            b[i] = a[i];
        }
        for (int j = 0; j < travelMgr.ultimateUpgrades.Length; j++, i++)
        {
            c[j] = a[i];
        }
        CopyToIl2CppArray(b, travelMgr.advancedUpgrades);
        CopyToIl2CppArray(c, travelMgr.ultimateUpgrades);
    }
    public static Dictionary<int, string> ConvertIl2CppDictionary(Il2CppSystem.Collections.Generic.Dictionary<int, string> il2CppDict)
    {
        var standardDict = new Dictionary<int, string>();

        foreach (var kvp in il2CppDict)
        {
            // 将每个键值对添加到标准的 C# Dictionary 中
            standardDict.Add(kvp.Key, kvp.Value);
        }

        return standardDict;
    }


    public override void Load()
    {
        Log = BepInEx.Logging.Logger.CreateLogSource("MyCustomLogger");
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        string dllPath = Assembly.GetExecutingAssembly().Location;
        string dllDirectory = Path.GetDirectoryName(dllPath);
        myPath = dllDirectory;
        keep = Path.Combine(dllDirectory, "keep.txt");
        new Harmony("modMenu").PatchAll();
    }

    [HarmonyPatch(typeof(Board), "Update")]
    public class BoardPatch
    {
        public static void Postfix()
        {
            if (MyCustomBehaviour.isGloveNoCD)
            {
                GloveMgr.Instance.CD = GloveMgr.Instance.fullCD;
            }
            if (MyCustomBehaviour.isCustomWord)
            {
                TravelMgr travelMgr = ComponentHolderProtocol.GetOrAddComponent<TravelMgr>(GameObject.Find("GameAPP"));
                ReadBuffsToGame();
            }

            if (MyCustomBehaviour.isAllWord)
            {
                TravelMgr travelMgr = ComponentHolderProtocol.GetOrAddComponent<TravelMgr>(GameObject.Find("GameAPP"));
                for (int i = 0; i < travelMgr.advancedUpgrades.Count; i++)
                {
                    travelMgr.advancedUpgrades[i] = true;
                }
                for (int j = 0; j < travelMgr.ultimateUpgrades.Count; j++)
                {
                    travelMgr.ultimateUpgrades[j] = true;
                }
            }
            if(MyCustomBehaviour.isCustomWord|| MyCustomBehaviour.isAllWord)
            {
                // 获取 Board 类的 boardTag 结构体实例
                BoardTag boardTagInstance = GameAPP.board.GetComponent<Board>().boardTag;

                // 修改 boardTagInstance 的部分值
                boardTagInstance.isTravel = true;
                boardTagInstance.enableTravelBuff = true;

                // 将修改后的实例赋值回原来的位置
                GameAPP.board.GetComponent<Board>().boardTag = boardTagInstance;

            }


        }
    }

    [HarmonyPatch(typeof(CreatePlant), "SetPlant")]
    class GameAPPPatchsadfsadsds
    {
        [HarmonyPrefix]
        static bool Prefix(ref int newColumn, ref int newRow, ref int theSeedType,CreatePlant __instance)
        {
            if (MyCustomBehaviour.plantMode == 1|| MyCustomBehaviour.plantMode >= 4)
            {
                return true;
            }
            if (MyCustomBehaviour.plantMode == 2)
            {
                MyCustomBehaviour.plantMode = 1;
                for (int i = 0; i < GameAPP.board.GetComponent<Board>().rowNum; i++)
                {
                    GameAPP.board.GetComponent<Board>().GetComponent<CreatePlant>().SetPlant(newColumn, i, theSeedType);
                }
                MyCustomBehaviour.plantMode = 2;
            }
            else if (MyCustomBehaviour.plantMode == 3) {
                MyCustomBehaviour.plantMode = 1;
                for (int i = 0; i < GameAPP.board.GetComponent<Board>().rowNum; i++)
                {
                    for (int j = 0; j < GameAPP.board.GetComponent<Board>().columnNum; j++)
                    {
                        GameAPP.board.GetComponent<Board>().GetComponent<CreatePlant>().SetPlant(j, i, theSeedType);
                    }
                }
                MyCustomBehaviour.plantMode = 3;
            }
            return false;
        }

    }

    [HarmonyPatch(typeof(Zombie), "TakeDamage")]
    class GameAPPPatchsadf
    {
        [HarmonyPrefix]
        static void Prefix(ref int theDamage)
        {
            if (MyCustomBehaviour.isBulletSeckill)
            {
                theDamage = 1000000;
            }
        }

    }


    [HarmonyPatch(typeof(Plant), "PlantShootUpdate")]
    class PatchPlantShoot
    {
        [HarmonyPrefix]
        static void fastPlantShoot(Plant __instance)
        {
            if (MyCustomBehaviour.isFastShoot)
            {
                __instance.thePlantAttackCountDown = 0;
            }

        }
    }



    [HarmonyPatch(typeof(Plant), "Die")]
    class GameAPPPatchsadfasdsad
    {
        [HarmonyPrefix]
        static bool Prefix()
        {
            if (MyCustomBehaviour.isPlantInvulnerable)
            {
                return false;
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(Zombie), "Update")]
    class PatchZombieCold
    {
        [HarmonyPrefix]
        static void ZombieCold(Zombie __instance)
        {
            if (MyCustomBehaviour.isZombieCold)
            {
                __instance.SetCold(1f);
            }
            if (MyCustomBehaviour.isZombieFreeze)
            {
                __instance.SetCold(1f);
                __instance.SetFreeze(1f);
            }
            if (MyCustomBehaviour.isZombieMindControlled && !__instance.isMindControlled)
            {
                __instance.SetMindControl(100);
            }
            if (MyCustomBehaviour.isZombieGrap)
            {
                __instance.SetGrap(1f);
            }
            if (MyCustomBehaviour.isZombieJalaed)
            {
                __instance.SetJalaed();
            }
            
            
        }
    }

    [HarmonyPatch(typeof(CreateZombie), "SetZombie")]
    class GameAPPPatchsadfsadsd
    {
        [HarmonyPrefix]
        static void Prefix(ref int theZombieType)
        {
            if (MyCustomBehaviour.isRandomZombie)
            {
                int rand = UnityEngine.Random.Range(0, MyCustomBehaviour.ZombieTypesArray.Length);
                theZombieType = rand;
            }
            if (MyCustomBehaviour.isChangeZombie)
            {
                string str = File.ReadAllText(Path.Combine(myPath, "ChangeZombieType.txt"));
                string[] strArray = str.Split(',');
                int[] intArray = strArray.Select(int.Parse).ToArray();
                int rand = UnityEngine.Random.Range(0, intArray.Length);
                theZombieType = intArray[rand];
            }
        }

    }

    [HarmonyPatch(typeof(CreateZombie), "SetZombie")]
    class asdaff2
    {
        [HarmonyPrefix]
        static bool Prefix(ref int theRow, ref int theZombieType, ref float theX, ref bool isIdle)
        {
            if (!MyCustomBehaviour.isZombieRate)
            {
                return true;
            }
            if (MyCustomBehaviour.isZombieRate)
            {
                MyCustomBehaviour.isZombieRate = false;
                int m = System.Convert.ToInt32(File.ReadAllText(Path.Combine(myPath, "ZombieCreateRate.txt")));
                for (int i = 0; i < m; i++)
                {
                    CreateZombie.Instance.SetZombie(theRow, theZombieType, theX, isIdle);
                }
                MyCustomBehaviour.isZombieRate = true;
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(GridItem), "CreateGridItem")]
    class PatchNoPit
    {
        [HarmonyPrefix]
        static bool NoPit(ref int theType)
        {
            if (MyCustomBehaviour.isNoPit)
            {
                if (theType == 0 || theType == 1)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CreateBullet), "SetBullet")]
    class GameAPPPatchsadfsad
    {
        [HarmonyPrefix]
        static void Prefix(ref int theBulletType)
        {
            if (MyCustomBehaviour.isRandomBullet)
            {
                int rand = UnityEngine.Random.Range(0, MyCustomBehaviour.bulletTypesArray.Length);
                theBulletType = rand;
            }
        }

    }

    [HarmonyPatch(typeof(AlmanacPlantCtrl), "GetSeedType")]
    class GameAPPPatchsadfsadasd
    {
        [HarmonyPostfix]
        static void Postfix(AlmanacPlantCtrl __instance)
        {
           MyCustomBehaviour.clickPlantType = __instance.plantSelected;
        }

    }

    [HarmonyPatch(typeof(AlmanacCardZombie), "OnMouseDown")]
    class GameAPPPatchsadfsadasdasd
    {
        [HarmonyPostfix]
        static void Postfix(AlmanacCardZombie __instance)
        {
            MyCustomBehaviour.clickZombieType = (int)__instance.theZombieType;
        }

    }

    [HarmonyPatch(typeof(GameAPP), "Start")]
    class GameAPPPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            ClassInjector.RegisterTypeInIl2Cpp<MyCustomBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<ButtonType>();

            GameObject myGameObject = new GameObject("MyInjectedObject");
            myGameObject.AddComponent<MyCustomBehaviour>();
            myGameObject.AddComponent<ButtonType>();
        }

    }


    [HarmonyPatch(typeof(InGameBtn), "Start")]
    class GameAPPPatchsadaf
    {
        [HarmonyPostfix]
        static void Postfix(InGameBtn __instance)
        {
            __instance.gameObject.AddComponent<ButtonType>();
        }

    }





    [HarmonyPatch(typeof(InGameBtn), "OnMouseUpAsButton")]
    class boardPatch2332sss
    {
        [HarmonyPrefix]
        static bool Prefix(InGameBtn __instance)
        {
            int type = __instance.GetComponent<ButtonType>().type;
            if (type == 0)
            {
                return true;
            }

            if (type == 1)
            {
                GameAPP.board.GetComponent<Board>().theSun += 99999;
            }
            else if (type == 2)
            {
                MyCustomBehaviour.isGloveNoCD = !MyCustomBehaviour.isGloveNoCD;
                GameObject bu = GameObject.Find("bu2");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "手套无CD(" + (MyCustomBehaviour.isGloveNoCD ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 3)
            {
                MyCustomBehaviour.isHammerNoCD = !MyCustomBehaviour.isHammerNoCD;
                GameObject bu = GameObject.Find("bu3");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "木锤无CD(" + (MyCustomBehaviour.isHammerNoCD ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 4)
            {
                MyCustomBehaviour.isCardNoCD = !MyCustomBehaviour.isCardNoCD;
                GameObject bu = GameObject.Find("bu4");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "植物无CD(" + (MyCustomBehaviour.isCardNoCD ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 5)
            {
                MyCustomBehaviour.isPlantFreeSet = !MyCustomBehaviour.isPlantFreeSet;
                GameObject bu = GameObject.Find("bu5");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "重叠种植(" + (MyCustomBehaviour.isPlantFreeSet ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 6)
            {
                foreach (Zombie i in GameAPP.board.GetComponent<Board>().zombieArray)
                {
                    if (i != null)
                    {
                        i.Die();
                    }
                }
            }
            else if (type == 7)
            {
                foreach (Plant i in GameAPP.board.GetComponent<Board>().plantArray)
                {
                    if (i != null)
                    {
                        UnityEngine.Object.Destroy(i.gameObject);
                    }
                }
            }
            else if (type == 8)
            {
                global::UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Items/Fertilize/Ferilize"), new Vector2(0f, 0f), Quaternion.identity, GameAPP.board.transform);
            }
            else if (type == 9)
            {
                global::UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Items/Bucket"), new Vector2(0f, 0f), Quaternion.identity, GameAPP.board.transform);
            }
            else if (type == 10)
            {
                global::UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Items/Helmet"), new Vector2(0f, 0f), Quaternion.identity, GameAPP.board.transform);
            }
            else if (type == 11)
            {
                GameObject gameObject = Resources.Load<GameObject>("Board/Award/TrophyPrefab");
                GameObject gameObject2 = global::UnityEngine.Object.Instantiate<GameObject>(gameObject, GameAPP.board.gameObject.transform);
                gameObject2.transform.position = new Vector2(0f, 0f);
            }else if (type == 12)
            {
                global::UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Items/JackBox"), new Vector2(0f, 0f), Quaternion.identity, GameAPP.board.transform);
            }
            else if (type == 13)
            {
                MyCustomBehaviour.savePlants();
            }
            else if (type == 14)
            {
                MyCustomBehaviour.loadPlants();
            }
            else if (type == 15)
            {
                MyCustomBehaviour.isBulletSeckill = !MyCustomBehaviour.isBulletSeckill;
                GameObject bu = GameObject.Find("bu15");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "子弹秒杀(" + (MyCustomBehaviour.isBulletSeckill ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 16)
            {
                MyCustomBehaviour.isRandomBullet = !MyCustomBehaviour.isRandomBullet;
                GameObject bu = GameObject.Find("bu16");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "随机子弹(" + (MyCustomBehaviour.isRandomBullet ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 17)
            {
                MyCustomBehaviour.isPlantInvulnerable = !MyCustomBehaviour.isPlantInvulnerable;
                GameObject bu = GameObject.Find("bu17");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "植物无敌(" + (MyCustomBehaviour.isPlantInvulnerable ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 18)
            {
                MyCustomBehaviour.isRandomZombie = !MyCustomBehaviour.isRandomZombie;
                GameObject bu = GameObject.Find("bu18");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "随机僵尸(" + (MyCustomBehaviour.isRandomZombie ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 19)
            {
                MyCustomBehaviour.plantMode++;
                if (MyCustomBehaviour.plantMode >= 4)
                {
                    MyCustomBehaviour.plantMode = 1;
                }
                string a;
                if (MyCustomBehaviour.plantMode == 1)
                {
                    a = "种植模式(个)";
                }else if (MyCustomBehaviour.plantMode == 2)
                {
                    a = "种植模式(列)";
                }
                else
                {
                    a = "种植模式(全屏)";
                }
                GameObject bu = GameObject.Find("bu19");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = a;
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 20)
            {
                GameAPP.developerMode = !GameAPP.developerMode;
                GameObject bu = GameObject.Find("bu20");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "开发者模式(" + (GameAPP.developerMode ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 21)
            {
                MyCustomBehaviour.isFastShoot = !MyCustomBehaviour.isFastShoot;
                GameObject bu = GameObject.Find("bu21");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "极限攻速(" + (MyCustomBehaviour.isFastShoot ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 22)
            {
                MyCustomBehaviour.isAllWord = !MyCustomBehaviour.isAllWord;
                GameObject bu = GameObject.Find("bu22");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "全词条(" + (MyCustomBehaviour.isAllWord ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }

            }
            else if (type == 23)
            {
                MyCustomBehaviour.isCustomWord = !MyCustomBehaviour.isCustomWord;
                GameObject bu = GameObject.Find("bu23");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "自定义词条(" + (MyCustomBehaviour.isCustomWord ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 24)
            {
                Application.OpenURL("https://space.bilibili.com/405337949");
            }
            else if (type == 25)
            {
                Vector3 a = new Vector3(2.28f, -0.65f, -100f);
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "SeedLibrary")
                    {
                        if (!obj.transform.parent.gameObject.activeSelf)
                        {
                            obj.SetActive(true);
                            obj.transform.parent.gameObject.SetActive(true);
                            obj.transform.position = a;
                        }
                        else
                        {
                            obj.SetActive(false);
                            obj.transform.parent.gameObject.SetActive(false);
                        }
                        break;
                    }
                }
            }
            else if (type == 26)
            {
                MyCustomBehaviour.isZombieCold = !MyCustomBehaviour.isZombieCold;
                GameObject bu = GameObject.Find("bu26");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸减速(" + (MyCustomBehaviour.isZombieCold ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 27)
            {
                MyCustomBehaviour.isZombieFreeze = !MyCustomBehaviour.isZombieFreeze;
                GameObject bu = GameObject.Find("bu27");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸冻结(" + (MyCustomBehaviour.isZombieFreeze ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 28)
            {
                MyCustomBehaviour.isZombieMindControlled = !MyCustomBehaviour.isZombieMindControlled;
                GameObject bu = GameObject.Find("bu28");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸魅惑(" + (MyCustomBehaviour.isZombieMindControlled ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 29)
            {
                MyCustomBehaviour.isZombieGrap = !MyCustomBehaviour.isZombieGrap;
                GameObject bu = GameObject.Find("bu29");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸缠绕(" + (MyCustomBehaviour.isZombieGrap ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 30)
            {
                MyCustomBehaviour.isZombieJalaed = !MyCustomBehaviour.isZombieJalaed;
                GameObject bu = GameObject.Find("bu30");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸红温(" + (MyCustomBehaviour.isZombieJalaed ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 31)
            {
                MyCustomBehaviour.isNoPit = !MyCustomBehaviour.isNoPit;
                GameObject bu = GameObject.Find("bu31");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "没有坑洞(" + (MyCustomBehaviour.isNoPit ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 32)
            {
                foreach (Plant i in GameAPP.board.GetComponent<Board>().plantArray)
                {
                    if (i != null)
                    {
                        UnityEngine.Object.Destroy(i.GetComponents<BoxCollider2D>()[1]);
                    }
                }
            }
            else if (type == 33)
            {
                global::UnityEngine.Object.Instantiate<GameObject>(Resources.Load<GameObject>("Items/Pickaxe"), new Vector2(0f, 0f), Quaternion.identity, GameAPP.board.transform);
            }
            else if (type == 34)
            {
                MyCustomBehaviour.isClickPlant = !MyCustomBehaviour.isClickPlant;
                GameObject bu = GameObject.Find("bu34");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植植物(" + (MyCustomBehaviour.isClickPlant ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 35)
            {
                MyCustomBehaviour.isClickZombie = !MyCustomBehaviour.isClickZombie;
                GameObject bu = GameObject.Find("bu35");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植僵尸(" + (MyCustomBehaviour.isClickZombie ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 36)
            {
                MyCustomBehaviour.isIgnoreZombie = !MyCustomBehaviour.isIgnoreZombie;
                GameObject back = GameObject.Find("Background");
                GameObject checkLose = null;
                for (int i = 0; i < back.transform.childCount; i++)
                {
                    if (back.transform.GetChild(i).gameObject.name == "checklose")
                    {
                        checkLose = back.transform.GetChild(i).gameObject;
                    }
                }
                if (MyCustomBehaviour.isIgnoreZombie)
                {
                    checkLose.SetActive(false);
                }
                else
                {
                    checkLose.SetActive(true);
                }
                GameObject bu = GameObject.Find("bu36");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "无视僵尸进家(" + (MyCustomBehaviour.isIgnoreZombie ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 37)
            {
                LoadBoardTag();
            }
            else if (type == 38)
            {
                MyCustomBehaviour.isZombieRate = !MyCustomBehaviour.isZombieRate;
                GameObject bu = GameObject.Find("bu38");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸生成倍率(" + (MyCustomBehaviour.isZombieRate ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 39)
            {
                MyCustomBehaviour.isClickZombie2 = !MyCustomBehaviour.isClickZombie2;
                GameObject bu = GameObject.Find("bu39");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植僵尸(" + (MyCustomBehaviour.isClickZombie2 ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 40)
            {
                foreach(GameObject i in UnityEngine.Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    i.SetActive(true);
                }
            }
            else if (type == 41)
            {
                MyCustomBehaviour.isChangeZombie = !MyCustomBehaviour.isChangeZombie;
                GameObject bu = GameObject.Find("bu41");
                for (int i = 0; i < bu.transform.childCount; i++)
                {
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "更改出怪类型(" + (MyCustomBehaviour.isChangeZombie ? "开" : "关") + ")";
                    bu.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
                }
            }
            else if (type == 42)
            {
                GameAPP.board.GetComponent<Board>().GetComponent<CreateMower>().SetMower(GameAPP.board.GetComponent<Board>().roadType);
            }
            else if (type == 43)
            {
                foreach (GameObject i in GameAPP.board.GetComponent<Board>().mowerArray)
                {
                    if (i != null)
                    {
                        Traverse.Create(i.GetComponent<Mower>()).Method("StartMove").GetValue();
                    }
                }
            }
            else if (type == 44)
            {
                for (int k = 0; k < Board.Instance.iceRoadFadeTime.Count; k++)
                {
                    Board.Instance.iceRoadFadeTime[k] = 0f;
                }
            }
            else if (type == 45)
            {
                for (int l = Board.Instance.griditemArray.Count - 1; l >= 0; l--)
                {
                    UnityEngine.Object.Destroy(Board.Instance.griditemArray[l]);
                }
                //System.Array.Clear(Board.Instance.griditemArray);
            }
            else if (type == 46)
            {
                for(int i = 0; i < Board.Instance.columnNum; i++)
                {
                    for(int j = 0;j< Board.Instance.rowNum; j++)
                    {
                        GridItem.CreateGridItem(i, j, 3);
                    }
                }
            }
            else if (type == 47)
            {
                Board.Instance.theMoney = 999999999;
            }
            else if (type == 48)
            {
                foreach(Zombie i in Board.Instance.zombieArray)
                {
                    if(i != null)
                    {
                        i.DropGardenPlant();
                    }
                }
            }

            return false;
        }

    }

    [HarmonyPatch(typeof(Board), "Start")]
    class boardPatch
    {
        [HarmonyPostfix]
        static void Postfix(Board __instance)
        {
            if (MyCustomBehaviour.CanvasUp != null)
            {
                return;
            }
            GameObject button = GameObject.Find("CanvasUp/InGameUIFHD/Bottom/SeedLibrary/ReselectCard");
            GameObject CanvasUpLpp = GameObject.Find("CanvasUp");
            MyCustomBehaviour.CanvasUp = UnityEngine.GameObject.Instantiate(CanvasUpLpp);
            MyCustomBehaviour.CanvasUp.name = "myCanvas";
            MyCustomBehaviour.CanvasUp.GetComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.All;
            MyCustomBehaviour.CanvasUp.GetComponent<Canvas>().sortingOrder = 20001;

           RectTransform rectTransform;
            GameObject bu1 = UnityEngine.Object.Instantiate(button);
            bu1.name = "bu1";
            bu1.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu1.AddComponent<ButtonType>().type = 1;
            for(int i = 0; i < bu1.transform.childCount; i++)
            {
                bu1.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "阳光+99999";
            }
            rectTransform = bu1.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f);

            GameObject bu2 = UnityEngine.Object.Instantiate(button);
            bu2.name = "bu2";
            bu2.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu2.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu2.AddComponent<ButtonType>().type = 2;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu2.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text ="手套无CD("+ (MyCustomBehaviour.isGloveNoCD?"开":"关")+")";
            }
            rectTransform = bu2.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f-48f);

            GameObject bu3 = UnityEngine.Object.Instantiate(button);
            bu3.name = "bu3";
            bu3.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu3.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu3.AddComponent<ButtonType>().type = 3;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu3.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "木锤无CD(" + (MyCustomBehaviour.isHammerNoCD ? "开" : "关") + ")";
            }
            rectTransform = bu3.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48*2f);

            GameObject bu4 = UnityEngine.Object.Instantiate(button);
            bu4.name = "bu4";
            bu4.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu4.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu4.AddComponent<ButtonType>().type = 4;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu4.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "植物无CD(" + (MyCustomBehaviour.isCardNoCD ? "开" : "关") + ")";
            }
            rectTransform = bu4.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 3f);

            GameObject bu5 = UnityEngine.Object.Instantiate(button);
            bu5.name = "bu5";
            bu5.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu5.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu5.AddComponent<ButtonType>().type = 5;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu5.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "重叠种植("+(MyCustomBehaviour.isPlantFreeSet ? "开" : "关") + ")";
            }
            rectTransform = bu5.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 4f);

            GameObject bu6 = UnityEngine.Object.Instantiate(button);
            bu6.name = "bu6";
            bu6.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu6.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu6.AddComponent<ButtonType>().type = 6;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu6.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "清除僵尸";
            }
            rectTransform = bu6.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 5f);

            GameObject bu7 = UnityEngine.Object.Instantiate(button);
            bu7.name = "bu7";
            bu7.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu7.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu7.AddComponent<ButtonType>().type = 7;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu7.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "清除植物";
            }
            rectTransform = bu7.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 6f);

            GameObject bu8 = UnityEngine.Object.Instantiate(button);
            bu8.name = "bu8";
            bu8.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu8.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu8.AddComponent<ButtonType>().type = 8;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu8.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成肥料";
            }
            rectTransform = bu8.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 7f);

            GameObject bu9 = UnityEngine.Object.Instantiate(button);
            bu9.name = "bu9";
            bu9.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu9.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu9.AddComponent<ButtonType>().type = 9;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu9.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成铁桶";
            }
            rectTransform = bu9.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 8f);

            GameObject bu10 = UnityEngine.Object.Instantiate(button);
            bu10.name = "bu10";
            bu10.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu10.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu10.AddComponent<ButtonType>().type = 10;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu10.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成头盔";
            }
            rectTransform = bu10.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 9f);

            GameObject bu11 = UnityEngine.Object.Instantiate(button);
            bu11.name = "bu11";
            bu11.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu11.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu11.AddComponent<ButtonType>().type = 11;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu11.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成奖杯";
            }
            rectTransform = bu11.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 10f);

            GameObject bu12 = UnityEngine.Object.Instantiate(button);
            bu12.name = "bu12";
            bu12.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu12.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu12.AddComponent<ButtonType>().type = 12;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu12.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成小丑盒子";
            }
            rectTransform = bu12.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 11f);

            GameObject bu13 = UnityEngine.Object.Instantiate(button);
            bu13.name = "bu13";
            bu13.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu13.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu13.AddComponent<ButtonType>().type = 13;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu13.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "保存阵容";
            }
            rectTransform = bu13.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f);

            GameObject bu14 = UnityEngine.Object.Instantiate(button);
            bu14.name = "bu14";
            bu14.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu14.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu14.AddComponent<ButtonType>().type = 14;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu14.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "读取阵容";
            }
            rectTransform = bu14.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48f);

            GameObject bu15 = UnityEngine.Object.Instantiate(button);
            bu15.name = "bu15";
            bu15.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu15.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu15.AddComponent<ButtonType>().type = 15;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu15.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "子弹秒杀(" + (MyCustomBehaviour.isBulletSeckill ? "开" : "关") + ")";
            }
            rectTransform = bu15.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 2f);

            GameObject bu16 = UnityEngine.Object.Instantiate(button);
            bu16.name = "bu16";
            bu16.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu16.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu16.AddComponent<ButtonType>().type = 16;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu16.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "随机子弹("+(MyCustomBehaviour.isRandomBullet ? "开" : "关") + ")";
            }
            rectTransform = bu16.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 3f);

            GameObject bu17 = UnityEngine.Object.Instantiate(button);
            bu17.name = "bu17";
            bu17.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu17.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu17.AddComponent<ButtonType>().type = 17;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu17.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "植物无敌(" + (MyCustomBehaviour.isPlantInvulnerable ? "开" : "关") + ")";
            }
            rectTransform = bu17.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 4f);

            GameObject bu18 = UnityEngine.Object.Instantiate(button);
            bu18.name = "bu18";
            bu18.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu18.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu18.AddComponent<ButtonType>().type = 18;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu18.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "随机僵尸(" + (MyCustomBehaviour.isRandomZombie ? "开" : "关") + ")";
            }
            rectTransform = bu18.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 5f);

            GameObject bu19 = UnityEngine.Object.Instantiate(button);
            bu19.name = "bu19";
            bu19.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu19.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu19.AddComponent<ButtonType>().type = 19;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu19.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "切换种植模式(个)";
            }
            rectTransform = bu19.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 6f);

            GameObject bu20 = UnityEngine.Object.Instantiate(button);
            bu20.name = "bu20";
            bu20.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu20.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu20.AddComponent<ButtonType>().type = 20;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu20.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "开发者模式(" + (GameAPP.developerMode ? "开" : "关") + ")";
            }
            rectTransform = bu20.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 7f);

            GameObject bu21 = UnityEngine.Object.Instantiate(button);
            bu21.name = "bu21";
            bu21.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu21.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu21.AddComponent<ButtonType>().type = 21;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu21.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "极限攻速(" + (MyCustomBehaviour.isFastShoot ? "开" : "关") + ")";
            }
            rectTransform = bu21.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 8f);

            GameObject bu22 = UnityEngine.Object.Instantiate(button);
            bu22.name = "bu22";
            bu22.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu22.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu22.AddComponent<ButtonType>().type = 22;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu22.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "全词条(" + (MyCustomBehaviour.isAllWord ? "开" : "关") + ")";
            }
            rectTransform = bu22.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 9f);

            GameObject bu23 = UnityEngine.Object.Instantiate(button);
            bu23.name = "bu23";
            bu23.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu23.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu23.AddComponent<ButtonType>().type = 23;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu23.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "自定义词条(" + (MyCustomBehaviour.isCustomWord ? "开" : "关") + ")";
            }
            rectTransform = bu23.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 10f);

            GameObject bu24 = UnityEngine.Object.Instantiate(button);
            bu24.name = "bu24";
            bu24.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu24.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu24.AddComponent<ButtonType>().type = 24;
            for (int i = 0; i < bu1.transform.childCount; i++)
            {
                bu24.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "关注我";
            }
            rectTransform = bu24.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 11f);

            GameObject bu25 = UnityEngine.Object.Instantiate(button);
            bu25.name = "bu25";
            bu25.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu25.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu25.AddComponent<ButtonType>().type = 25;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu25.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "打开(隐藏)选卡界面";
            }
            rectTransform = bu25.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f);

            GameObject bu26 = UnityEngine.Object.Instantiate(button);
            bu26.name = "bu26";
            bu26.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu26.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu26.AddComponent<ButtonType>().type = 26;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu26.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸减速("+(MyCustomBehaviour.isZombieCold ? "开" : "关") + ")";
            }
            rectTransform = bu26.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48f);

            GameObject bu27 = UnityEngine.Object.Instantiate(button);
            bu27.name = "bu27";
            bu27.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu27.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu27.AddComponent<ButtonType>().type = 27;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu27.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸冻结(" + (MyCustomBehaviour.isZombieFreeze ? "开" : "关") + ")";
            }
            rectTransform = bu27.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 2f);

            GameObject bu28 = UnityEngine.Object.Instantiate(button);
            bu28.name = "bu28";
            bu28.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu28.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu28.AddComponent<ButtonType>().type = 28;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu28.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸魅惑(" + (MyCustomBehaviour.isZombieMindControlled ? "开" : "关") + ")";
            }
            rectTransform = bu28.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 3f);

            GameObject bu29 = UnityEngine.Object.Instantiate(button);
            bu29.name = "bu29";
            bu29.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu29.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu29.AddComponent<ButtonType>().type = 29;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu29.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸缠绕(" + (MyCustomBehaviour.isZombieGrap ? "开" : "关") + ")";
            }
            rectTransform = bu29.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 4f);

            GameObject bu30 = UnityEngine.Object.Instantiate(button);
            bu30.name = "bu30";
            bu30.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu30.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu30.AddComponent<ButtonType>().type = 30;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu30.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸红温(" + (MyCustomBehaviour.isZombieJalaed ? "开" : "关") + ")";
            }
            rectTransform = bu30.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 5f);

            GameObject bu31 = UnityEngine.Object.Instantiate(button);
            bu31.name = "bu31";
            bu31.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu31.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu31.AddComponent<ButtonType>().type = 31;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu31.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "没有坑洞(" + (MyCustomBehaviour.isNoPit ? "开" : "关") + ")";
            }
            rectTransform = bu31.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 6f);

            GameObject bu32 = UnityEngine.Object.Instantiate(button);
            bu32.name = "bu32";
            bu32.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu32.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu32.AddComponent<ButtonType>().type = 32;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu32.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "植物遁入虚空";
            }
            rectTransform = bu32.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 7f);

            GameObject bu33 = UnityEngine.Object.Instantiate(button);
            bu33.name = "bu33";
            bu33.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu33.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu33.AddComponent<ButtonType>().type = 33;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu33.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成矿工稿子";
            }
            rectTransform = bu33.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 8f);

            GameObject bu34 = UnityEngine.Object.Instantiate(button);
            bu34.name = "bu34";
            bu34.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu34.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu34.AddComponent<ButtonType>().type = 34;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu34.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植植物(" + (MyCustomBehaviour.isClickPlant ? "开" : "关") + ")";
            }
            rectTransform = bu34.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 9f);

            GameObject bu35 = UnityEngine.Object.Instantiate(button);
            bu35.name = "bu35";
            bu35.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu35.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu35.AddComponent<ButtonType>().type = 35;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu35.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植僵尸(" + (MyCustomBehaviour.isClickZombie ? "开" : "关") + ")";
            }
            rectTransform = bu35.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 10f);

            GameObject bu36 = UnityEngine.Object.Instantiate(button);
            bu36.name = "bu36";
            bu36.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu36.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu36.AddComponent<ButtonType>().type = 36;
            for (int i = 0; i < bu25.transform.childCount; i++)
            {
                bu36.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "无视僵尸进家(" + (MyCustomBehaviour.isIgnoreZombie ? "开" : "关") + ")";
            }
            rectTransform = bu36.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 11f);

            GameObject bu37 = UnityEngine.Object.Instantiate(button);
            bu37.name = "bu37";
            bu37.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu37.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu37.AddComponent<ButtonType>().type = 37;
            for (int i = 0; i < bu37.transform.childCount; i++)
            {
                bu37.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "应用关卡配置";
            }
            rectTransform = bu37.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f);

            GameObject bu38 = UnityEngine.Object.Instantiate(button);
            bu38.name = "bu38";
            bu38.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu38.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu38.AddComponent<ButtonType>().type = 38;
            for (int i = 0; i < bu38.transform.childCount; i++)
            {
                bu38.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "僵尸生成倍率(" + (MyCustomBehaviour.isZombieRate ? "开" : "关") + ")";
            }
            rectTransform = bu38.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48f);

            GameObject bu39 = UnityEngine.Object.Instantiate(button);
            bu39.name = "bu39";
            bu39.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu39.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu39.AddComponent<ButtonType>().type = 39;
            for (int i = 0; i < bu39.transform.childCount; i++)
            {
                bu39.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "右键种植僵尸(" + (MyCustomBehaviour.isClickZombie2 ? "开" : "关") + ")";
            }
            rectTransform = bu39.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 2f);

            GameObject bu40 = UnityEngine.Object.Instantiate(button);
            bu40.name = "bu40";
            bu40.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu40.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu40.AddComponent<ButtonType>().type = 40;
            for (int i = 0; i < bu40.transform.childCount; i++)
            {
                bu40.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "激活所有物体";
            }
            rectTransform = bu40.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 3f);

            GameObject bu41 = UnityEngine.Object.Instantiate(button);
            bu41.name = "bu41";
            bu41.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu41.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu41.AddComponent<ButtonType>().type = 41;
            for (int i = 0; i < bu41.transform.childCount; i++)
            {
                bu41.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "更改出怪类型(" + (MyCustomBehaviour.isChangeZombie ? "开" : "关") + ")";
            }
            rectTransform = bu41.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 4f);

            GameObject bu42 = UnityEngine.Object.Instantiate(button);
            bu42.name = "bu42";
            bu42.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu42.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu42.AddComponent<ButtonType>().type = 42;
            for (int i = 0; i < bu42.transform.childCount; i++)
            {
                bu42.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "生成推推车";
            }
            rectTransform = bu42.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 5f);

            GameObject bu43 = UnityEngine.Object.Instantiate(button);
            bu43.name = "bu43";
            bu43.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu43.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu43.AddComponent<ButtonType>().type = 43;
            for (int i = 0; i < bu43.transform.childCount; i++)
            {
                bu43.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "推推车启动";
            }
            rectTransform = bu43.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 6f);

            GameObject bu44 = UnityEngine.Object.Instantiate(button);
            bu44.name = "bu44";
            bu44.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu44.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu44.AddComponent<ButtonType>().type = 44;
            for (int i = 0; i < bu44.transform.childCount; i++)
            {
                bu44.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "清除冰道";
            }
            rectTransform = bu44.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 7f);

            GameObject bu45 = UnityEngine.Object.Instantiate(button);
            bu45.name = "bu45";
            bu45.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu45.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu45.AddComponent<ButtonType>().type = 45;
            for (int i = 0; i < bu45.transform.childCount; i++)
            {
                bu45.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "清除坑洞";
            }
            rectTransform = bu45.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 8f);

            GameObject bu46 = UnityEngine.Object.Instantiate(button);
            bu46.name = "bu46";
            bu46.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu46.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu46.AddComponent<ButtonType>().type = 46;
            for (int i = 0; i < bu46.transform.childCount; i++)
            {
                bu46.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "全屏梯子";
            }
            rectTransform = bu46.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 9f);

            GameObject bu47 = UnityEngine.Object.Instantiate(button);
            bu47.name = "bu47";
            bu47.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu47.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu47.AddComponent<ButtonType>().type = 47;
            for (int i = 0; i < bu37.transform.childCount; i++)
            {
                bu47.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "无限金币";
            }
            rectTransform = bu47.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 10f);

            GameObject bu48 = UnityEngine.Object.Instantiate(button);
            bu48.name = "bu48";
            bu48.transform.SetParent(MyCustomBehaviour.CanvasUp.transform);
            bu48.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            bu48.AddComponent<ButtonType>().type = 48;
            for (int i = 0; i < bu37.transform.childCount; i++)
            {
                bu48.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "全场僵尸掉花园植物";
            }
            rectTransform = bu48.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-860f, 500f - 48 * 11f);




            string pattern = "";
            pattern = @"^bu([1-9]|1[0-2])$";
            for (int i = 0; i < MyCustomBehaviour.CanvasUp.transform.childCount; i++)
            {
                Transform tr = MyCustomBehaviour.CanvasUp.transform.GetChild(i);
                if (!Regex.IsMatch(tr.name, pattern))
                {
                    tr.gameObject.SetActive(false);
                }
                if (Regex.IsMatch(tr.name, pattern))
                {
                    tr.gameObject.SetActive(true);
                }
            }
        }

    }

    public class MyCustomBehaviour : MonoBehaviour
    {
        public static bool isGloveNoCD = false;
        public static bool isHammerNoCD = false;
        public static bool isCardNoCD = false;
        public static bool isPlantFreeSet = false;
        public static bool isBulletSeckill = false;
        public static bool isRandomBullet = false;
        public static bool isPlantInvulnerable = false;
        public static bool isRandomZombie = false;
        public static bool isFastShoot = false;
        public static bool isHuiZhiPlant = false;
        public static bool isHuiZhiZombie = false;
        public static bool isZombieCold = false;
        public static bool isZombieFreeze = false;
        public static bool isZombieMindControlled = false;
        public static bool isNoPit = false;
        public static bool isClickPlant = false;
        public static bool isClickZombie = false;
        public static bool isClickZombie2 = false;
        public static bool isIgnoreZombie = false;
        public static bool isZombieGrap = false;
        public static bool isZombieJalaed = false;
        public static bool isAllWord = false;
        public static bool isCustomWord = false;
        public static bool isZombieRate = false;
        public static bool isChangeZombie = false;
        

        public static int plantMode = 1;
        public static int clickPlantType = 0;
        public static int clickZombieType = 0;
        public static GameObject CanvasUp = null;

        public static BulletType[] bulletTypesArray = (BulletType[])System.Enum.GetValues(typeof(BulletType));
        public static ZombietType[] ZombieTypesArray = (ZombietType[])System.Enum.GetValues(typeof(ZombietType));

        public static int currentIndex = 1;
        public static int theMaxIndex = 4;
        

        public static void savePlants()
        {
            string result = "";
            foreach (Plant i in GameAPP.board.GetComponent<Board>().plantArray)
            {
                if (i != null)
                {
                    result += "(" + i.thePlantColumn + "," + i.thePlantRow + "," + i.thePlantType + ")";
                }
            }
            File.WriteAllText(Plugin.keep, result);
        }
        public static void loadPlants()
        {
            string content = File.ReadAllText(Plugin.keep);

            // 使用正则表达式匹配三元组中的数字
            Regex regex = new Regex(@"\((\d+),(\d+),(\d+)\)");
            MatchCollection matches = regex.Matches(content);

            // 用List存储所有数字
            List<int> numbers = new List<int>();

            foreach (Match match in matches)
            {
                for (int i = 1; i <= 3; i++)  // 每个三元组有三个数字
                {
                    numbers.Add(int.Parse(match.Groups[i].Value));
                }
            }
            int[] numberArray = numbers.ToArray();
            for (int i = 2; i < numberArray.Length; i += 3)
            {
                GameAPP.board.GetComponent<Board>().GetComponent<CreatePlant>().SetPlant(numberArray[i - 2], numberArray[i - 1], numberArray[i]);
            }

        }

        public static string GetHierarchyPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform;

            while (current.parent != null)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            return path;
        }

        public unsafe static void SetArrayValue(int row, int col, int value)
        {
            // 获取指向数组的指针
            System.IntPtr arrayPtr;
            IL2CPP.il2cpp_field_static_get_value(GetNativeFieldInfoPtrData(), (void*)(&arrayPtr));

            if (arrayPtr == System.IntPtr.Zero)
            {
                throw new System.InvalidOperationException("Array pointer is null.");
            }

            // 计算元素的偏移量
            int[] lengths = new int[2]; // 这里假设我们知道数组的维度
            lengths[0] = 2048; // 行数
            lengths[1] = 2048; // 列数

            // 根据行和列计算元素在内存中的位置
            int index = row * lengths[1] + col; // 计算一维数组的索引
            System.IntPtr elementPtr = System.IntPtr.Add(arrayPtr, index * sizeof(int)); // 每个元素的大小是 4 字节

            // 设置数组值
            *((int*)elementPtr) = value;
        }


        public static System.IntPtr GetNativeFieldInfoPtrData()
        {
            // 获取 MixData 类型
            System.Type mixDataType = typeof(MixData);

            // 获取 private static readonly 字段信息
            FieldInfo fieldInfo = mixDataType.GetField("NativeFieldInfoPtr_data", BindingFlags.Static | BindingFlags.NonPublic);

            if (fieldInfo != null)
            {
                // 获取字段的值
                return (System.IntPtr)fieldInfo.GetValue(null); // null 表示静态字段
            }

            throw new System.InvalidOperationException("Field 'NativeFieldInfoPtr_data' not found.");
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Q))
            {
                SaveBoardTag();
            }
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.W))
            {
                WriteBuffsToFile();
            }
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.E))
            {
                SetArrayValue(0, 0, 1);
                Log.LogMessage("123");
            }
            
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                if (MyCustomBehaviour.isClickZombie)
                {

                }
                if (MyCustomBehaviour.isClickZombie2)
                {
                    GameAPP.board.GetComponent<Board>().GetComponent<CreateZombie>().SetZombie(Mouse.Instance.theMouseRow, MyCustomBehaviour.clickZombieType, Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition).x).GetComponent<Zombie>();
                }
                if (MyCustomBehaviour.isClickPlant)
                {
                    CreatePlant.Instance.SetPlant(Mouse.Instance.theMouseColumn, Mouse.Instance.theMouseRow, MyCustomBehaviour.clickPlantType);
                }
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Tab))
            {
                if (currentIndex < theMaxIndex)
                {
                    currentIndex++;
                    string pattern = "";
                    if (currentIndex == 1)
                    {
                        pattern = @"^bu([1-9]|1[0-2])$";
                    }else if (currentIndex==2)
                    {
                        pattern = @"^bu(1[3-9]|2[0-4])$";
                    }
                    else if(currentIndex==3)
                    {
                        pattern = @"^bu(2[5-9]|3[0-6])$";
                    }
                    else
                    {
                        pattern = @"^bu(3[7-9]|4[0-8])$";
                    }
                    for (int i = 0; i < CanvasUp.transform.childCount; i++)
                    {
                        Transform tr = CanvasUp.transform.GetChild(i);
                        if (!Regex.IsMatch(tr.name, pattern))
                        {
                            tr.gameObject.SetActive(false);
                        }
                        if (Regex.IsMatch(tr.name, pattern))
                        {
                            tr.gameObject.SetActive(true);
                        }
                    }
                }
                else
                {
                    currentIndex = 0;
                    string pattern = @"^bu([1-9]|[1-9][0-9])$";
                    for (int i = 0; i < CanvasUp.transform.childCount; i++)
                    {
                        Transform tr = CanvasUp.transform.GetChild(i);
                        if (Regex.IsMatch(tr.name, pattern))
                        {
                            tr.gameObject.SetActive(false);
                        }
                    }
                }

            }
        }
        
    }

    

    [HarmonyPatch(typeof(GloveMgr), "CDUpdate")]
    class PatchGloveCD
    {
        [HarmonyPrefix]
        static void GloveCD(GloveMgr __instance)
        {
            
        }

    }

    [HarmonyPatch(typeof(CardUI), "Update")]
    class Patch998
    {
        [HarmonyPrefix]
        static void Prefix(CardUI __instance)
        {
            if (MyCustomBehaviour.isCardNoCD)
            {
                __instance.CD = __instance.fullCD;
            }
        }
    }

    [HarmonyPatch(typeof(HammerMgr), "Update")]
    class Patch9988
    {
        [HarmonyPrefix]
        static void Prefix(HammerMgr __instance)
        {
            if (MyCustomBehaviour.isHammerNoCD)
            {
                __instance.CD = __instance.fullCD;
            }

        }
    }

    [HarmonyPatch(typeof(CreatePlant), "SetPlant")]
    class PatchPlantFreeSet
    {
        [HarmonyPrefix]
        static void freeSetPlant(ref bool isFreeSet)
        {
            if (MyCustomBehaviour.isPlantFreeSet)
            {
                isFreeSet = true;
            }
        }
    }

    public class ButtonType:MonoBehaviour {
        public int type = 0;
    }

}





