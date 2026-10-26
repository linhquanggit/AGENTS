using BreakInfinity;
using DP.Tools;
using DP.Utilities;
using Games;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static Games.Events;
using static MilitaryRanks;
using static PVPSO;

public static class PVP
{
    static PVP()
    {
        dataInit = PVPSO.GetData();
        data = Player.PVPProcess;
        InitData();
        ResolvePendingMatchAsLoss();
    }
    static void InitData()
    {
        if (data == null)
        {
            Debug.Log("Init new PVP data");
            data = new Process
            {
                Point = 0,
                Prizes = InitPrizes(),
                Booster = new BoosterProcess
                {
                    IsPremium = false,
                    //DayOfYear = 0,
                    PremiumStart = string.Empty,
                    PremiumFinish = string.Empty,
                    Boosters = new List<BoosterItemProcess>()
                    {
                        new BoosterItemProcess
                            { Index = 0, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                        new BoosterItemProcess
                            { Index = 1, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                        new BoosterItemProcess
                            { Index = 2, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                    }
                },
                WinCount = 0,
                Casual = new RankProcess
                {
                    HighScores = 0,
                    Rank = RANK.BRONZE_3,
                    Airport = new AirportProcess
                    {
                        Context = AIRPORT_CONTEXT.PVPCalsual,
                        IsUnlocked = true,
                        Type = AIRPORT_TYPE.AIRPORT_JAPAN
                    },
                    AVG_KillTime = 0,
                    AVG_WaveScore = 0,
                    WinStreak = 0,
                    LoseStreak = 0,
                },
                OneVsOne = new RankProcess
                {
                    HighScores = 0,
                    Rank = RANK.NONE,
                    Airport = new AirportProcess
                    {
                        Context = AIRPORT_CONTEXT.PvpRank,
                        IsUnlocked = true,
                        Type = AIRPORT_TYPE.AIRPORT_JAPAN
                    },
                    AVG_KillTime = 0,
                    AVG_WaveScore = 0,
                    WinStreak = 0,
                    LoseStreak = 0,
                }
            };
            DPDebug.Log($"===========>CREAT NEW PVP");
            SaveData();
        }
        else
        {
            if (data.Prizes == null || data.Prizes.Count <= 0)
            {
                data.Prizes = InitPrizes();
                SaveData();
            }
            CheckCasualData();
            Check1V1Data();
        }
    }
    //
    static void CheckCasualData()
    {
        // khởi tạo data casual nếu null
        if (data.Casual == null)
        {
            data.Casual = new RankProcess
            {
                HighScores = 0,
                Rank = RANK.BRONZE_3,
                Airport = new AirportProcess
                {
                    Context = AIRPORT_CONTEXT.PVPCalsual,
                    IsUnlocked = true,
                    Type = AIRPORT_TYPE.AIRPORT_JAPAN
                },
            };
            SaveData();
        }
        // nếu chế độ casual chưa mở Airport thì khởi tạo mới
        if (data.Casual.Airport == null)
        {
            data.Casual.Airport = new AirportProcess
            {
                Context = AIRPORT_CONTEXT.PVPCalsual,
                IsUnlocked = true,
                Type = AIRPORT_TYPE.AIRPORT_JAPAN
            };
            SaveData();
        }
        // mở khoá sẵn airport casual nếu chưa mở
        if (!data.Casual.Airport.IsUnlocked)
        {
            data.Casual.Airport.IsUnlocked = true;
            SaveData();
        }
        if (data.Casual.Rank == RANK.NONE)
        {
            data.Casual.Rank = RANK.BRONZE_3;
            SaveData();
        }
        // vá AVGSCORE + điểm mang ra thi đấu nếu save cũ/từ server chưa có
        CheckAvgScoreData(MODE.CASUAL, data.Casual);
    }
    static void Check1V1Data()
    {
        // khởi tạo data Ranked nếu null
        if (data.OneVsOne == null)
        {
            data.OneVsOne = new RankProcess
            {
                Airport = new AirportProcess
                {
                    Context = AIRPORT_CONTEXT.PvpRank,
                    IsUnlocked = true,
                    Type = AIRPORT_TYPE.AIRPORT_JAPAN
                },
            };
            SaveData();
        }
        // nếu chế độ ranked chưa mở Airport thì khởi tạo mới
        if (data.OneVsOne.Airport == null)
        {
            data.OneVsOne.Airport = new AirportProcess
            {
                Context = AIRPORT_CONTEXT.PvpRank,
                IsUnlocked = true,
                Type = AIRPORT_TYPE.AIRPORT_JAPAN
            };
            SaveData();
        }
        // mở khoá sẵn airport casual nếu chưa mở
        if (!data.OneVsOne.Airport.IsUnlocked)
        {
            data.OneVsOne.Airport.IsUnlocked = true;
            SaveData();
        }
        // kiểm tra nếu dữ liệu bị lỗi thì set lại về 0
        if (float.IsNaN(data.OneVsOne.AVG_KillTime) || float.IsInfinity(data.OneVsOne.AVG_KillTime))
        {
            data.OneVsOne.AVG_KillTime = 0;
            SaveData();
        }
        // vá AVGSCORE + điểm mang ra thi đấu nếu save cũ/từ server chưa có
        CheckAvgScoreData(MODE.ONE_VS_ONE, data.OneVsOne);

        // Migration cho user đã join season hiện tại trước khi có field snapshot:
        // chốt group theo progression hiện tại đúng một lần rồi lưu lại.
        if (data.OneVsOne.IsEventPlaying &&
            (data.OneVsOne.Season == null ||
             !ProgressionGroups.IsValid(data.OneVsOne.Season.RewardProgressionGroup)))
        {
            data.OneVsOne.Season ??= new PVPSeasonProcess();
            data.OneVsOne.Season.RewardProgressionGroup = RewardByProgressionSystem.CurrentGroup;
            DPDebug.Log($"[PVP][Migration] Snapshot RewardByProgressionGroup=" +
                        $"{data.OneVsOne.Season.RewardProgressionGroup} for existing season " +
                        $"{data.OneVsOne.SeasonVersion} at level {GameProgressionLevel.Current}");
            SaveData();
        }
    }
    //
    static Process data;
    static PVPSO.PVPData dataInit;
    public static readonly int LevelUnlock = 55;
    public static float PVPPoint => data.Point;
    public static float FocusLostForfeitSeconds
    {
        get
        {
            // Fallback để data/asset cũ chưa có field mới vẫn giữ đúng ngưỡng mặc định.
            return Mathf.Max(0f, dataInit?.FocusLostForfeitSeconds ?? 5f);
        }
    }
    // lấy rank hiện tại theo mode chơi
    public static RANK GetRankCurrent(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.ONE_VS_ONE:// 1v1
                if (data.OneVsOne.Rank == RANK.NONE)
                {
                    data.OneVsOne.Rank = RANK.BRONZE_3;
                    SaveData();
                    return RANK.BRONZE_3;
                }
                return data.OneVsOne.Rank;
            default:// casual
                if (data.Casual.Rank == RANK.NONE)
                {
                    data.Casual.Rank = RANK.BRONZE_3;
                    SaveData();
                    return RANK.BRONZE_3;
                }
                return data.Casual.Rank;
        }
    }
    public static RankProcess GetRankProcess(MODE _mode = MODE.NONE)
    {
        if (_mode == MODE.NONE) _mode = ModeCurrent;
        switch (_mode)
        {
            case MODE.CASUAL: return data.Casual;
            case MODE.ONE_VS_ONE: return data.OneVsOne;
            //
            default: return null;
        }
    }
    //
    public static FIGHTER_TYPE FighterUsing()
    {
        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        var _fighterID = _process == null ? FIGHTER_TYPE.NONE : _process.Airport.IDFighter;
        return _fighterID;
    }
    public static BigDouble CPAirport()
    {
        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        var _cp = _process == null ? 0 : _process.Airport.CP;
        return _cp;
    }
    //
    static void SaveData()
    {
        DPDebug.Log($"[PVP][SaveData] 1v1 rank={data?.OneVsOne?.Rank} " +
                    $"challengerPoint={data?.OneVsOne?.ChallengerPoint} " +
                    $"seasonVersion='{data?.OneVsOne?.SeasonVersion}' " +
                    $"rewardProgressionGroup={data?.OneVsOne?.Season?.RewardProgressionGroup} " +
                    $"isEventPlaying={data?.OneVsOne?.IsEventPlaying}");
        Player.PVPProcess = data;
    }
    public static PVP.Process GetProcess()
    {
        return data;
    }

    #region [SEASON] — battle pass + nhiệm vụ hằng ngày

    /// <summary>Config mùa giải của chế độ XẾP HẠNG. Mode khác thì gọi GetSeasonData(mode).</summary>
    public static PVPSeasonData SeasonData => GetSeasonData(MODE.ONE_VS_ONE);

    /// <summary>
    /// Config mùa giải RIÊNG của một chế độ PVP — khai trong PVPSO.PVPModeData của mode đó, đối xứng
    /// với RankProcess.Season bên save. Không load Resources riêng.
    /// </summary>
    public static PVPSeasonData GetSeasonData(MODE _mode)
    {
        var _modeData = dataInit?.Modes?.FirstOrDefault(x => x.Mode == _mode);
        return _modeData?.Season;
    }

    /// <summary>
    /// Tiến trình mùa giải của chế độ XẾP HẠNG — hiện chỉ mode này có battle pass, nên UI dùng
    /// property này. Mode khác có season thì gọi GetSeasonProcess(mode).
    /// </summary>
    public static PVPSeasonProcess SeasonProcess => GetSeasonProcess(MODE.ONE_VS_ONE);

    /// <summary>
    /// Tiến trình mùa giải RIÊNG của một chế độ PVP — sống trong RankProcess của mode đó, cạnh
    /// SeasonVersion. Tự vá process thiếu (Pass/Missions null do save cũ) nên chỗ gọi khỏi tự lo.
    /// </summary>
    public static PVPSeasonProcess GetSeasonProcess(MODE _mode)
    {
        if (data == null) return null;

        var _rank = GetRankProcess(_mode);
        if (_rank == null) return null;

        _rank.Season = PVPSeason.EnsureProcess(_rank.Season);
        var _passData = GetSeasonData(_mode)?.Pass;
        bool _needsExpMigration = _passData != null && _passData.HasData &&
                                  _rank.Season.Pass != null && !_rank.Season.Pass.UsesCumulativeExp;
        _rank.Season.Pass = BattlePass.EnsureProcess(_passData, _rank.Season.Pass);
        if (_needsExpMigration && _rank.Season.Pass != null && _rank.Season.Pass.UsesCumulativeExp)
        {
            SaveData();
        }
        return _rank.Season;
    }

    /// <summary>
    /// Gán lại process mùa giải sau khi PVPSeason mutate. Mặc định ghi vào chế độ XẾP HẠNG —
    /// mode khác có pass riêng thì truyền _mode.
    /// </summary>
    static void SetSeasonProcess(PVPSeasonProcess _process, MODE _mode = MODE.ONE_VS_ONE)
    {
        if (data == null || _process == null) return;

        var _rank = GetRankProcess(_mode);
        if (_rank == null)
        {
            DPDebug.LogError($"[PVP] Thiếu RankProcess của mode [{_mode}] -> mất thay đổi season.");
            return;
        }

        _rank.Season = _process;
    }

    /// <summary>Version mùa của chế độ XẾP HẠNG. 0 = preseason, 1 = S1...</summary>
    public static int SeasonVersionRanked
    {
        get
        {
            if (data == null || data.OneVsOne == null) return 0;
            return data.OneVsOne.SeasonVersion;
        }
    }

    public const int INVALID_SEASON_VERSION = -1;

    /// <summary>
    /// SeasonVersion của event XẾP HẠNG đang ACTIVE trên server, do UIPVPMode ghi lại sau mỗi lần kéo
    /// all-event. RUNTIME-ONLY, KHÔNG lưu save — dựng lại ở mỗi lần load. -1 = chưa kéo được
    /// dữ liệu hoặc đang off-season.
    /// </summary>
    public static int SeasonVersionServerRanked { get; set; } = INVALID_SEASON_VERSION;

    /// <summary>
    /// ID của event ONE_VS_ONE đang ACTIVE lấy từ all-event/connect response.
    /// Runtime-only giống SeasonVersionServerRanked; dùng để vá RankProcess.EventID trước khi tìm đối thủ.
    /// </summary>
    public static string EventIDServerRanked { get; set; } = string.Empty;

    public static bool TryParseSeasonVersion(string _raw, out int _seasonVersion)
    {
        return int.TryParse(_raw, out _seasonVersion) && _seasonVersion >= 0;
    }

    /// <summary>
    /// Đồng bộ event id của mode vào save. Connect season và user data là hai nguồn tách biệt,
    /// nên save cũ có thể đã ở đúng SeasonVersion nhưng EventID vẫn rỗng.
    /// </summary>
    public static bool SyncEventID(MODE _mode, string _eventID)
    {
        if (string.IsNullOrWhiteSpace(_eventID)) return false;

        var _process = GetRankProcess(_mode);
        if (_process == null || _process.EventID == _eventID) return false;

        DPDebug.Log($"[PVP][SyncEventID] mode={_mode} eventID='{_process.EventID}'->'{_eventID}'");
        _process.EventID = _eventID;
        SaveData();
        return true;
    }

    /// <summary>
    /// Vá riêng SeasonVersion mà không chạy logic chuyển mùa. Chỉ dùng cho dữ liệu S1 cũ đã bị lệch;
    /// không thay đổi rank, điểm, Battle Pass, nhiệm vụ hay bất kỳ tiến trình nào khác.
    /// </summary>
    public static bool SyncSeasonVersionOnly(MODE _mode, int _seasonVersion)
    {
        if (_seasonVersion < 0) return false;

        var _process = GetRankProcess(_mode);
        if (_process == null || _process.SeasonVersion == _seasonVersion) return false;

        DPDebug.LogWarning($"[PVP][SyncSeasonVersionOnly] mode={_mode} " +
            $"seasonVersion='{_process.SeasonVersion}'->'{_seasonVersion}'");
        _process.SeasonVersion = _seasonVersion;
        SaveData();
        return true;
    }

    /// <summary>
    /// Server đã mở mùa mới nhưng user CHƯA join (chưa bấm Ranked để ConnectEvent) -> rank của mùa
    /// mới chưa xác định. Tự thành false ngay khi TryCreateNewSeason ghi SeasonVersion mới vào save.
    /// </summary>
    public static bool IsRankedNewSeasonNotJoined
    {
        get
        {
            if (SeasonVersionServerRanked < 0) return false;// off-season / chưa có data
            var _process = GetRankProcess(MODE.ONE_VS_ONE);
            return _process == null || !_process.IsEventPlaying ||
                   SeasonVersionServerRanked != SeasonVersionRanked;
        }
    }

    /// <summary>
    /// Đánh dấu ĐÃ bắn event "mở lần đầu" của mùa này. Trả true nếu đây là lần đầu (caller mới bắn).
    /// Mốc lưu bằng chính SeasonVersion của mode, nên sang mùa mới là tự cho bắn lại — không cần cờ bool
    /// và không cần reset ở đâu.
    /// </summary>
    public static bool TryMarkFirstOpen(bool _isBattlePass)
    {
        var _process = SeasonProcess;
        if (_process == null) return false;

        // Chưa có mùa (chưa connect server) -> chưa ghi mốc, để lần mở sau bắn với season_id đúng.
        var _rank = GetRankProcess(MODE.ONE_VS_ONE);
        if (_rank == null || !_rank.IsEventPlaying) return false;
        string _seasonID = SeasonVersionRanked.ToString();

        if (_isBattlePass)
        {
            if (_process.SeasonIDTrackedBP == _seasonID) return false;
            _process.SeasonIDTrackedBP = _seasonID;
        }
        else
        {
            if (_process.SeasonIDTrackedRanked == _seasonID) return false;
            _process.SeasonIDTrackedRanked = _seasonID;
        }
        SaveData();
        return true;
    }

    /// <summary>
    /// Mốc ngày để roll nhiệm vụ, tính theo ngày UTC nên mọi thiết bị đổi nhiệm vụ cùng lúc,
    /// không lệch theo múi giờ máy.
    /// </summary>
    public static string SeasonDayID => DPUtils.GetUtcNow.ToString("yyyyMMdd");

    /// <summary>Thời điểm nhiệm vụ làm mới = 0h UTC hôm sau, dạng round-trip cho DPUtils.GetRemainTime.</summary>
    public static string SeasonDayEndTimeUtc => DPUtils.GetUtcNow.Date.AddDays(1).ToString("o");

    /// <summary>
    /// Đảm bảo nhiệm vụ của ngày hiện tại đã được tạo trước khi gameplay ghi tiến trình.
    /// Không được phụ thuộc vào việc người chơi đã mở UI Battle Pass hay chưa.
    /// </summary>
    static bool EnsureSeasonDailyMissions()
    {
        SetSeasonProcess(PVPSeason.CheckDaily(SeasonData, SeasonProcess, SeasonDayID, out bool _isNewDay));
        return _isNewDay;
    }

    /// <summary>
    /// EndTimeEvent chỉ có sau khi mở màn PVP và kéo all-event, nên không thể dùng nó để tính
    /// nhiệm vụ OnlineTime từ lúc vừa vào game. IsEventPlaying được lưu theo user và cho biết
    /// user đã tham gia một mùa Ranked, đủ để khởi tạo/cộng mission trước khi mở UI Ranked.
    /// </summary>
    static bool HasJoinedRankedSeason()
    {
        var _rank = GetRankProcess(MODE.ONE_VS_ONE);
        return _rank != null && _rank.IsEventPlaying;
    }

    /// <summary>Gọi sau khi data user được load để reset mission ngày mới ngay lúc vào game.</summary>
    static void CheckSeasonDailyOnGameLoad()
    {
        OnSeasonAppResumed();
    }

    /// <summary>
    /// Kiểm tra lại ngày khi app trở lại foreground. Không gọi API mùa mới; chỉ dùng season local
    /// mà user đã tham gia, nên không phụ thuộc việc đã mở màn Ranked hay chưa.
    /// </summary>
    public static void OnSeasonAppResumed()
    {
        if (data == null || !HasJoinedRankedSeason()) return;
        if (EnsureSeasonDailyMissions()) SaveData();
    }

    /// <summary>
    /// Gọi khi TÌM THẤY đối thủ và bắt đầu vào trận — cộng "chơi X trận" ngay,
    /// không cần đánh xong. Chỉ tính ở chế độ xếp hạng.
    /// </summary>
    public static void OnSeasonMatchStarted()
    {
        if (data == null || !IsRankedEventActive) return;
        EnsureSeasonDailyMissions();
        SetSeasonProcess(PVPSeason.OnMatchStarted(SeasonData, SeasonProcess, modeCurrent));
        SaveData();
    }

    /// <summary>
    /// Gọi mỗi phút online — nối từ GameController.AddMinusOnlineTime.
    /// KHÔNG lọc theo mode: tính tổng thời gian chơi game, không riêng gì PVP xếp hạng.
    /// </summary>
    public static void OnSeasonOnlineMinute(int _minute = 1)
    {
        // OnlineTime phải được tính ngay từ màn game chung. Không phụ thuộc EndTimeEvent vì cờ này
        // chỉ được dựng sau khi user mở màn PVP/Ranked.
        if (data == null || !HasJoinedRankedSeason()) return;
        EnsureSeasonDailyMissions();
        SetSeasonProcess(PVPSeason.OnOnlineMinute(SeasonData, SeasonProcess, _minute));
        SaveData();
    }

    /// <summary>Gọi khi kết thúc trận — cộng "thắng X trận" và "đạt X tổng điểm". Chỉ tính ở xếp hạng.</summary>
    public static void OnSeasonMatchFinished()
    {
        // Kết quả trận/pending có thể được xử lý trước khi EndTimeEvent runtime được dựng lại
        // (ví dụ app bị kill rồi mở lại). IsEventPlaying là nguồn local đủ để ghi mission.
        if (data == null || !HasJoinedRankedSeason()) return;
        EnsureSeasonDailyMissions();

        // Trận bắt đầu trước 0h nhưng hoàn thành sau 0h: mission ngày cũ đã bị reset.
        // Ghi lại "chơi 1 trận" vào ngày hoàn thành để Play/Win/Score không bị chia sang 2 ngày.
        if (!string.IsNullOrEmpty(matchResultStartDayID) && matchResultStartDayID != SeasonDayID)
        {
            SetSeasonProcess(PVPSeason.OnMatchStarted(SeasonData, SeasonProcess, modeCurrent));
            DPDebug.Log($"[PVP][Season] Match crossed UTC day--start[{matchResultStartDayID}]" +
                        $"--finish[{SeasonDayID}]-->count full match on finish day.");
        }

        SetSeasonProcess(PVPSeason.OnMatchFinished(SeasonData, SeasonProcess, modeCurrent, IsWin,
                                                  Mathf.RoundToInt(scoreCurrent)));
        matchResultStartDayID = string.Empty;
        SaveData();
    }

    /// <summary>
    /// Kiểm tra mốc ngày để roll lại nhiệm vụ. _dayID do chỗ gọi truyền vào
    /// (ngày server / DayOfYear / ticks... tuỳ bạn chọn).
    /// </summary>
    public static bool CheckSeasonDaily(string _dayID)
    {
        if (data == null) return false;
        SetSeasonProcess(PVPSeason.CheckDaily(SeasonData, SeasonProcess, _dayID, out bool _isNewDay));
        SaveData();
        return _isNewDay;
    }

    /// <summary>Kiểm tra mốc mùa từ server. Khác mùa đã lưu -> reset pass. Trả true nếu sang mùa mới.</summary>
    public static bool CheckSeasonNew(string _seasonID)
    {
        if (data == null) return false;
        SetSeasonProcess(PVPSeason.CheckSeason(SeasonData, SeasonProcess, _seasonID, out bool _isNewSeason));
        SaveData();
        return _isNewSeason;
    }

    /// <summary>
    /// Cộng exp vào pass. Dùng khi nhiệm vụ ĐÃ được claim ở nơi khác (vd PopupBPMission tự claim
    /// rồi bắn exp ra) — nên ở đây chỉ cộng exp, KHÔNG claim lại.
    /// </summary>
    public static void AddSeasonPassExp(int _exp, out int _levelUp)
    {
        _levelUp = 0;
        if (data == null || _exp <= 0) return;

        // Thiếu nhánh mode trong save -> SeasonProcess null. Chặn ở đây thay vì để NRE giữa
        // luồng IAP (đã trừ tiền rồi).
        var _process = SeasonProcess;
        if (_process == null)
        {
            DPDebug.LogError("[PVP] SeasonProcess null -> không cộng được exp pass.");
            return;
        }

        int _levelBefore = _process.Pass != null ? _process.Pass.Level : 0;

        // Thiếu config thì AddExp trả về nguyên process (có thể null) -> đừng ghi null vào save,
        // UI sẽ NRE ở lần đọc sau.
        var _pass = BattlePass.AddExp(SeasonData?.Pass, _process.Pass, _exp, out _levelUp);
        if (_pass != null) _process.Pass = _pass;
        SaveData();

        // Mọi đường cộng exp (nhiệm vụ, mua gói) đều đi qua đây -> chỉ cần bắn mốc bậc ở một chỗ.
        if (_levelUp > 0) PVPAnalytics.BPLevelReached(_levelBefore, _levelBefore + _levelUp);
    }

    /// <summary>
    /// Cộng exp theo SKU vừa mua — gọi TỪ handler IAP (CC_Interface), KHÔNG qua UI.
    /// Nhờ vậy người chơi tắt popup giữa lúc chờ store thì exp vẫn được cộng, không mất tiền.
    /// Trả về số exp đã cộng (0 = không tìm thấy gói / thiếu config).
    /// </summary>
    public static int AddSeasonPassExpByProduct(IAP_Product _product)
    {
        if (data == null) return 0;

        var _pack = SeasonData?.ExpPacks?.GetByProduct(_product);
        if (_pack == null || !_pack.IsValid)
        {
            DPDebug.LogError($"[PVP] Không tìm thấy gói BP EXP theo SKU [{_product}] trong config " +
                             "-> đã trừ tiền mà không cộng được exp. Kiểm tra Season.ExpPacks của mode trong PVPSO.");
            return 0;
        }

        // Đã max thì AddExp bỏ hết exp -> phải báo LỖI, không im lặng trả về như đã cộng.
        if (BattlePass.IsCompleted(SeasonData?.Pass, SeasonProcess?.Pass))
        {
            DPDebug.LogError($"[PVP] Pass đã MAX mà vẫn mua được gói [{_product}] -> exp bị bỏ. " +
                             "Kiểm tra lại chỗ chặn nút mua.");
            return 0;
        }

        AddSeasonPassExp(_pack.Exp, out int _levelUp);
        DPDebug.Log($"[PVP] Mua gói BP EXP [{_product}] -> +{_pack.Exp} exp, lên {_levelUp} level.");
        return _pack.Exp;
    }

    /// <summary>
    /// Mở nhánh Premium của pass (gọi sau khi IAP thành công). Không phụ thuộc config nên
    /// thiếu asset vẫn ghi được cờ — tiền người chơi bỏ ra không bị mất.
    /// </summary>
    public static void UnlockSeasonPremium()
    {
        if (data == null) return;

        SetSeasonProcess(PVPSeason.UnlockPremium(SeasonData, SeasonProcess));
        SaveData();
    }

    /// <summary>Lưu lại tiến trình mùa giải — dùng khi UI đã tự mutate process (claim quà pass...).</summary>
    public static void SaveSeason()
    {
        if (data == null) return;
        SaveData();
    }

    #endregion
    public static void SetAirportProcess(AirportProcess _process, MODE _mode = MODE.NONE)
    {
        if (_mode == MODE.NONE) _mode = ModeCurrent;
        DPDebug.Log($"Change PVP Airport Process--Mode current[{ModeCurrent}]--Mode change[{_mode}]--FighterID[{_process.IDFighter}]");
        switch (_mode)
        {
            case MODE.CASUAL: data.Casual.Airport = _process; break;
            case MODE.ONE_VS_ONE: data.OneVsOne.Airport = _process; break;
        }
        SaveData();
    }
    public static void Refresh()
    {
        DPDebug.Log("PVP PROCESS refresh");
        if (Player.PVPProcess == null)
            DPDebug.Log("PVP PROCESS SERVER NULL");
        else
            DPDebug.Log($"[PVP][Refresh] incoming 1v1 rank={Player.PVPProcess.OneVsOne?.Rank} " +
                        $"challengerPoint={Player.PVPProcess.OneVsOne?.ChallengerPoint} " +
                        $"seasonVersion='{Player.PVPProcess.OneVsOne?.SeasonVersion}' " +
                        $"isEventPlaying={Player.PVPProcess.OneVsOne?.IsEventPlaying}");
        //
        // SeasonVersion local chỉ được đổi sau khi ConnectEvent xác nhận người chơi đã join.
        // Một số response server có thể chứa version của season ACTIVE dù user chưa join;
        // không được để Refresh() ghi đè version local trong trường hợp đó.
        var _localSeasonVersion = data?.OneVsOne?.SeasonVersion ?? 0;
        var _incomingRanked = Player.PVPProcess?.OneVsOne;
        if (_incomingRanked != null
            && !_incomingRanked.IsEventPlaying
            && data?.OneVsOne != null)
        {
            _incomingRanked.SeasonVersion = _localSeasonVersion;
        }

        data = Player.PVPProcess;
        if (data == null)
        {
            InitData();
            return;
        }
        // Data từ server có thể thiếu nhánh mode (JsonUtility để null) -> vá lại, không thì
        // GetRankProcess trả null và MỌI thay đổi season (nhiệm vụ, exp, premium) bị bỏ im lặng.
        CheckCasualData();
        Check1V1Data();
        // Mission hằng ngày và OnlineTime không được phụ thuộc vào việc user đã mở màn Ranked.
        CheckSeasonDailyOnGameLoad();
        DPDebug.Log($"[PVP][Refresh] applied 1v1 rank={data.OneVsOne?.Rank} " +
                    $"challengerPoint={data.OneVsOne?.ChallengerPoint} " +
                    $"seasonVersion='{data.OneVsOne?.SeasonVersion}' " +
                    $"isEventPlaying={data.OneVsOne?.IsEventPlaying}");
    }
    public static void OnNewDay()
    {
        OnNewDayBooster();
    }
    public static void CheckDownloadAsset()
    {
        if (Games.Level.LevelMax < 50) return;
        //
        AddressableManager.instance.CheckAndDownload("PVP");
    }
    public static bool IsUnlocked
    {
        get
        {
            if (Level.LevelMax >= LevelUnlock) return true;
            return false;
        }
    }

    #region [FRAME AVATAR]

    public static bool GetFrameAvatarUnlock(string _frameAvatarID)
    {
        DPDebug.Log($"PVP Frame avatar check unlock---ID[{_frameAvatarID}]");
        var _1v1 = string.IsNullOrEmpty(data.OneVsOne.FrameAvatarID) ? "Empty" : data.OneVsOne.FrameAvatarID;
        switch (_frameAvatarID)
        {
            // 1v1
            case "Frame#5":
            case "Frame#6":
            case "Frame#7":
            case "Frame#8":
            case "Frame#9":
            case "Frame#10":
                if (string.IsNullOrEmpty(data.OneVsOne.FrameAvatarID)) return false;// chưa có khung
                if (data.OneVsOne.FrameAvatarID == _frameAvatarID)// khung hiện tại đang được sử dụng
                    return true;
                else
                    return false;
            //
            default: return false;
        }
    }

    #endregion

    #region [RANK]

    // Lấy thay đổi Challenger Point. Với 1v1, điểm phụ thuộc vào chênh lệch
    // giữa điểm của đối thủ và điểm của mình.
    public static int GetChallengerPointBonus(MODE _mode, int _myChallengerPoint = -1, int _opChallengerPoint = -1)
    {
        return GetChallengerPointDelta(_mode, _myChallengerPoint, _opChallengerPoint, true);
    }

    public static int GetChallengerPointDelta(MODE _mode, int _myChallengerPoint = -1,
        int _opChallengerPoint = -1, bool _isWin = true)
    {
        if (_mode == MODE.NONE) _mode = modeCurrent;
        switch (_mode)
        {
            case MODE.CASUAL:
                var _modeData = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
                if (_modeData == null) return -1;
                return _isWin ? _modeData.Point.WinChallengerPoint : -_modeData.Point.LostChallengerPoint;
            case MODE.ONE_VS_ONE:// ở chế độ 1v1
                return GetChallengerPointDelta1V1(_myChallengerPoint, _opChallengerPoint, _isWin);
            default: return -1;
        }
    }

    static int GetChallengerPointDelta1V1(int _myChallengerPoint, int _opChallengerPoint, bool _isWin)
    {
        // D = B - A: dương khi đối thủ có điểm cao hơn mình.
        var _dRaw = _opChallengerPoint - _myChallengerPoint;
        var _d = Mathf.Clamp(_dRaw, -1000, 1000);
        int _result;

        // Mathf.RoundToInt làm tròn .5 về số chẵn. Công thức Challenger dùng
        // kiểu ROUND thông thường, nên dùng Floor(x + 0.5) vì các giá trị ở đây dương.
        if (_isWin)
        {
            _result = Mathf.Clamp(Mathf.FloorToInt(25f + _d / 100f + 0.5f), 15, 35);
        }
        else
        {
            var _losePoint = Mathf.Clamp(Mathf.FloorToInt(15f - _d / 200f + 0.5f), 10, 20);
            _result = -_losePoint;
        }

        DPDebug.Log($"Get Challenger Point Delta 1v1--A(user current point)[{_myChallengerPoint}]--" +
            $"B(opponent current point)[{_opChallengerPoint}]--D raw(B-A)[{_dRaw}]--D used(clamped)[{_d}]--" +
            $"isWin[{_isWin}]--delta[{_result}]");
        return _result;
    }
    // lấy điều kiện rank theo mode chơi
    public static RankRequire GetRanksRequire(MODE _mode, RANK _rank)
    {
        if (_mode == MODE.NONE) _mode = modeCurrent;
        // lấy điều kiện rank theo mode
        var _modeData = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
        if (_modeData == null) return null;
        //
        var _rankRequire = _modeData.RanksRequire.FirstOrDefault(x => x.Rank == _rank);
        return _rankRequire;
    }
    // lấy quà theo rank của mode, mặc định ai cũng sẽ có khi tham gia PVP
    public static List<RewardCount> GetRankRewards(MODE _mode)
    {
        var _rankProcess = GetRankProcess(_mode);
        if (_rankProcess == null) return null;
        return GetRankRewards(_mode, _rankProcess.Rank);
    }
    // lấy quà theo một rank chỉ định, dùng cho preview/hack mà không cần sửa rank thật của user
    public static List<RewardCount> GetRankRewards(MODE _mode, RANK _rank)
    {
        List<RewardCount> _rw = new List<RewardCount>();
        //
        PVPModeData _modeData = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
        if (_modeData == null) return null;
        if (_rank == RANK.NONE) return null;
        // thêm quà khung avatar
        var _rwFrame = GetFrameAvatar(_rank);
        if (_rwFrame != null) _rw.Add(_rwFrame);
        // quà theo rank
        var _rankReward = _modeData.NormalRankRewards == null
            ? null
            : _modeData.NormalRankRewards.FirstOrDefault(x => x.Rank == _rank);
        if (_rankReward != null) _rw.AddRange(_rankReward.Rewards);
        else DPDebug.LogWarning($"[PVP] Mode [{_mode}] thiếu config NormalRankRewards cho rank [{_rank}].");
        return _rw;
    }
    static RewardCount GetFrameAvatar(RANK _rank)
    {
        switch (_rank)
        {
            // đồng
            case RANK.BRONZE_1:
            case RANK.BRONZE_2:
            case RANK.BRONZE_3:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_5 };
            // bạc
            case RANK.SILVER_1:
            case RANK.SILVER_2:
            case RANK.SILVER_3:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_6 };
            // vàng
            case RANK.GOLD_1:
            case RANK.GOLD_2:
            case RANK.GOLD_3:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_7 };
            // bạch kim
            case RANK.PLATIUM_1:
            case RANK.PLATIUM_2:
            case RANK.PLATIUM_3:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_8 };
            // kim cương
            case RANK.DIAMOND_1:
            case RANK.DIAMOND_2:
            case RANK.DIAMOND_3:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_9 };
            // thách đấu
            case RANK.CHALLENGER:
                return new RewardCount { Count = 1, Type = REWARD_TYPE.FRAME_AVATAR_10 };
            //
            default: return null;
        }
    }
    // Cập nhật point progress ranked
    public static void UpdateRankedPointProgress()
    {
        // đã max cấp mùa hiện tại
        if (data.RankedPointLevel >= dataInit.RankedPointProgresses.Last().Level)
        {
            DPDebug.Log("Cập nhật điểm ranked--Đã max cấp mùa hiện tại--1");
            if (data.RankedPointProgressOld != data.RankedPointProgress)
            {
                DPDebug.Log("Cập nhật điểm ranked--Đã max cấp mùa hiện tại--2");
                data.RankedPointProgressOld = data.RankedPointProgress;
                SaveData();
            }
            //// TH số điểm mới vượt quá mốc cuối--> claim nó lại
            //if (data.RankedPointProgress >= dataInit.RankedPointProgresses.Last().PointRequier)
            //{
            //    data.RankedPointProgress = dataInit.RankedPointProgresses.Last().PointRequier;
            //    data.RankedPointProgressOld = data.RankedPointProgress;
            //    SaveData();
            //}
            return;
        }
        // TH chưa max cấp hiện tại
        var _require = dataInit.RankedPointProgresses.FirstOrDefault(x => x.Level == (data.RankedPointLevel + 1));// mốc hiện tại cần đạt được
        if (_require.Level == 0 && _require.PointRequier == 0) return;
        // TH số điểm mới không đạt mốc tiếp theo
        if (data.RankedPointProgress < _require.PointRequier)
        {
            // cập nhật mốc điểm cũ bằng mới
            data.RankedPointProgressOld = data.RankedPointProgress;
            SaveData();
            return;
        }
        // TH đã đủ điểm đạt mốc mới --> cập nhật
        var _remain = data.RankedPointProgress - _require.PointRequier;// số điểm dư
        // nâng level lên mốc mới và cập nhật point mới
        data.RankedPointLevel = _require.Level;// set level bằng mốc mới
        data.RankedPointProgress = _remain;// set số điểm dư mới
        data.RankedPointProgressOld = _remain;// set số điểm cũ lên bằng số điểm hiện tại, điểm này chỉ được cập nhật thông qua đây
        SaveData();
    }
    // khởi tạo tiến trình cho rank theo mode chơi
    static List<MatchProgress> CreateMatchProgress(MODE _mode, RANK _rank)
    {
        var _progress = new List<MatchProgress>();

        var _dataMode = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
        if (_dataMode != null)
        {
            var _requier = _dataMode.RanksRequire.FirstOrDefault(x => x.Rank == _rank);
            if (_requier != null)
            {
                for (int i = 1; i <= _requier.TotalMatches; i++)
                {
                    var _match = new MatchProgress
                    {
                        Index = i,
                        Result = MATCH_RESULT.NONE
                    };
                    _progress.Add(_match);
                }
                DPDebug.Log($"Khởi tạo rank progress--Mode[{_mode}]--Rank[{_rank}]--total match[{_progress.Count}]");
            }
        }

        return _progress;
    }
    // Khởi tạo / kết thúc mùa giải — dùng chung cho MỌI mode pvp, không hardcode mode nào.
    // Thêm mode pvp mới: chỉ cần khai trong IsPVPEventType + GetModeFromEventType + GetRankProcess.
    /// <summary>
    /// Khởi tạo mùa mới cho RIÊNG 1 chế độ, chỉ khi server đã sang SeasonVersion khác.
    /// Trả true nếu thực sự khởi tạo. Đây là cửa duy nhất nên gọi từ UI, không gọi trực tiếp CreateNewSeason.
    /// </summary>
    public static bool TryCreateNewSeason(MODE _mode, int _seasonVersion)
    {
        if (_seasonVersion < 0) return false;
        var _process = GetRankProcess(_mode);
        if (_process == null) return false;
        if (_process.IsEventPlaying && _process.SeasonVersion == _seasonVersion)
        {
            DPDebug.Log($"PVP không khởi tạo season--local và server cùng SeasonVersion[{_seasonVersion}]");
            return false;
        }
        DPDebug.Log($"PVP đồng bộ season mới--mode[{_mode}]--" +
            $"version local[{_process.SeasonVersion}]-->server[{_seasonVersion}]");
        // Chốt mùa cũ TRƯỚC. Cần cho TH user vào muộn, đã trôi qua kỳ trả quà nên luồng claim không chạy:
        // không gọi ở đây thì FrameAvatarID không bao giờ được set, mà gọi sau CreateNewSeason thì rank đã tụt.
        EndSeason(_mode);
        CreateNewSeason(_mode, _seasonVersion);
        return true;
    }
    public static void CreateNewSeason(MODE _mode, int _newSeasonVersion)
    {
        var _process = GetRankProcess(_mode);
        if (_process == null)
        {
            DPDebug.LogError($"[PVP] Thiếu RankProcess của mode [{_mode}] -> không khởi tạo được mùa mới.");
            return;
        }
        DPDebug.Log($"[PVP][CreateNewSeason] BEFORE mode={_mode} rank={_process.Rank} " +
                    $"challengerPoint={_process.ChallengerPoint} oldSeasonVersion='{_process.SeasonVersion}' " +
                    $"newSeasonVersion='{_newSeasonVersion}'");
        // chốt AVGSCORE của mùa mới. PHẢI đứng trước DropRank + reset HighScores vì cần rank & high score của preseason
        SetupAvgScoreNewSeason(_mode, _process);
        // RankedPoint* nằm ở Process (dùng chung mọi mode), chỉ mode xếp hạng cộng vào nên chỉ mode đó được reset
        if (IsRankedMode(_mode))
        {
            // CreateNewSeason chỉ được gọi sau khi ConnectEvent xác nhận season ACTIVE.
            // firstJoinSeason là tín hiệu ưu tiên; SeasonVersion khác nhau là nhánh phục hồi khi server
            // đã ghi nhận join nhưng local chưa kịp lưu. Cả hai nhánh đều đã qua guard SeasonVersion.
            DPDebug.Log($"[PVP][CreateNewSeason] reset Victory Bonus--" +
                $"oldSeason='{data.RankedPointSeasonID}' newSeason='{_newSeasonVersion}' " +
                $"oldProgress={data.RankedPointProgress} oldLevel={data.RankedPointLevel}");
            data.RankedPointProgress = 0;
            data.RankedPointProgressOld = 0;
            data.RankedPointLevel = 0;
            data.RankedPointSeasonID = _newSeasonVersion.ToString();

            _process.Season ??= new PVPSeasonProcess();
            _process.Season.RewardProgressionGroup = RewardByProgressionSystem.CurrentGroup;
            DPDebug.Log($"[PVP][CreateNewSeason] RewardByProgressionGroup=" +
                        $"{_process.Season.RewardProgressionGroup} at level {GameProgressionLevel.Current}");
        }
        var _newRank = RankHelperPVP.DropRank(_process.Rank, 1);// bắt đầu mùa giải mới thì người chơi sẽ bị tụt 1 bậc rank của mùa trước
        _process.Rank = _newRank;
        _process.MatchProgresses = CreateMatchProgress(_mode, _newRank);
        _process.ChallengerPoint = 0;
        _process.WinStreak = 0;
        _process.LoseStreak = 0;
        _process.IsEventPlaying = true;
        _process.SeasonVersion = _newSeasonVersion;
        _process.HighScores = 0;
        DPDebug.Log($"Khởi tạo mùa giải PVP mới--mode[{_mode}]--SeasonVersion[{_process.SeasonVersion}]--new rank[{_process.Rank}]");
        DPDebug.Log($"[PVP][CreateNewSeason] AFTER mode={_mode} rank={_process.Rank} " +
                    $"challengerPoint={_process.ChallengerPoint} seasonVersion='{_process.SeasonVersion}'");
        //
        SaveData();
    }

    // kết thúc mùa giải. Gọi được nhiều lần, lần 2 trở đi là no-op (gác bằng IsEventPlaying)
    public static void EndSeason(MODE _mode)
    {
        var _process = GetRankProcess(_mode);
        if (_process == null) return;
        if (!_process.IsEventPlaying) return;// chưa vào mùa nào, hoặc mùa này đã chốt rồi
        DPDebug.Log($"PVP kết thúc season--mode[{_mode}]--seasonVersion[{_process.SeasonVersion}]--rank[{_process.Rank}]");
        _process.FrameAvatarID = GetFrameAvatarID(_process.Rank);// set id khung avt cho mùa này
        _process.IsEventPlaying = false;
        SaveData();
    }
    static string GetFrameAvatarID(RANK _rank)
    {
        switch (_rank)
        {
            // đồng
            case RANK.BRONZE_1:
            case RANK.BRONZE_2:
            case RANK.BRONZE_3:
                return "Frame#5";
            // bạc
            case RANK.SILVER_1:
            case RANK.SILVER_2:
            case RANK.SILVER_3:
                return "Frame#6";
            // vàng
            case RANK.GOLD_1:
            case RANK.GOLD_2:
            case RANK.GOLD_3:
                return "Frame#7";
            // bạch kim
            case RANK.PLATIUM_1:
            case RANK.PLATIUM_2:
            case RANK.PLATIUM_3:
                return "Frame#8";
            // kim cương
            case RANK.DIAMOND_1:
            case RANK.DIAMOND_2:
            case RANK.DIAMOND_3:
                return "Frame#9";
            // thách đấu
            case RANK.CHALLENGER:
                return "Frame#10";
            //
            default: return string.Empty;
        }
    }
    // hàm kiểm tra xem RANK a có cao hơn RANK b không
    public static bool IsHigher(this RANK _origin, RANK _target)
    {
        int _groupOrigin = (int)_origin / 1000;
        int _groupTarget = (int)_target / 1000;

        if (_groupOrigin != _groupTarget)
        {
            // Nhóm rank nào lớn hơn thì rank đó cao hơn
            return _groupOrigin > _groupTarget;
        }

        // Cùng nhóm thì so tier ngược lại
        int _tierOrigin = (int)_origin % 1000;
        int _tierTarget = (int)_target % 1000;

        return _tierOrigin < _tierTarget; // vì 3 < 2 < 1
    }
    // hàm kiểm tra xem RANK A có cao hơn hoặc bằng RANK B
    public static bool IsHigherOrEqual(this RANK _origin, RANK _target)
    {
        return _origin == _target || _origin.IsHigher(_target);
    }
    // hàm lấy icon theo rank
    public static Sprite GetIconRank(RANK _rank)
    {
        if (dataInit.RanksData == null || dataInit.RanksData.Length <= 0) return dataInit.RanksData.FirstOrDefault(x => x.Rank == RANK.BRONZE_3).Icon;
        var _rs = dataInit.RanksData.FirstOrDefault(x => x.Rank == _rank).Icon;
        if (_rs == null) return dataInit.RanksData.FirstOrDefault(x => x.Rank == RANK.BRONZE_3).Icon;
        return _rs;
    }
    // Icon rank "?" cho TH chưa xác định được rank. null nếu chưa gán sprRankUnknown trong PVP.asset.
    public static Sprite GetIconRankUnknown()
    {
        return PVPSO.RankUnknownIcon;
    }
    // hàm lấy tên rank đã localize
    public static string GetRankName(RANK _rank)
    {
        if (dataInit.RanksData == null || dataInit.RanksData.Length <= 0)
            return dataInit.RanksData.FirstOrDefault(x => x.Rank == RANK.BRONZE_3).NameLocalize.ToString();
        var _rs = dataInit.RanksData.FirstOrDefault(x => x.Rank == _rank).NameLocalize.ToString();
        if (string.IsNullOrEmpty(_rs)) return dataInit.RanksData.FirstOrDefault(x => x.Rank == RANK.BRONZE_3).NameLocalize.ToString();
        return _rs;
    }
    // cập nhật rank
    static void UpdateRank(bool _isWin)
    {
        switch (modeCurrent)
        {
            case MODE.CASUAL: UpdateRankCasual(_isWin); break;
            case MODE.ONE_VS_ONE: UpdateRank1V1(_isWin); break;
        }
    }
    // hàm cập nhật rank casual khi kết thúc ván chơi
    static void UpdateRankCasual(bool _isWin)
    {
        if (_isWin)
        {
            // thắng thì lấy rank của bot
            DPDebug.Log($"Thắng---> rank casual hiện tại--[{data.Casual.Rank}]--rank mới---> {OpponentCurrent.RankProcess.Rank}");
            if (OpponentCurrent.RankProcess.Rank.IsHigher(data.Casual.Rank))
            {
                data.Casual.Rank = OpponentCurrent.RankProcess.Rank;
                SaveData();
            }
        }
        else
        {
            // thua thì tụt từ 2-4 bậc
            var _newRank = RankHelperPVP.DropRankRange(data.Casual.Rank, 2, 4);
            DPDebug.Log($"Thua----> rank casual hiện tại--[{data.Casual.Rank}]--rank mới---> {_newRank}");
            data.Casual.Rank = _newRank;
            SaveData();
        }
        DPDebug.Log($"Cập nhật rank casual mới khi xong trận---> {data.Casual.Rank}");
    }
    // cập nhật rank 1v1 khi kết thúc ván chơi
    static void UpdateRank1V1(bool _isWin)
    {
        // tăng tổng số trận đã chơi ở rank này
        if (_isWin)
        {
            data.OneVsOne.WinStreak += 1;// tăng chuỗi thắng
            data.OneVsOne.LoseStreak = 0;// reset chuỗi thua
            DPDebug.Log($"Cập nhật rank 1v1--win streak[{data.OneVsOne.WinStreak}]--lose streak[{0}]");
        }
        else
        {
            data.OneVsOne.WinStreak = 0;// reset chuỗi thắng
            data.OneVsOne.LoseStreak += 1;// tăng chuỗi thua
            DPDebug.Log($"Cập nhật rank 1v1--win streak[{0}]--lose streak[{data.OneVsOne.LoseStreak}]");
        }
        SaveData();
        // kiểm tra nếu người chơi đang ở rank thách đấu thì sẽ tăng theo công thức riêng
        if (data.OneVsOne.Rank == RANK.CHALLENGER)
        {
            UpdateRank1V1Challenger(_isWin);
            return;
        }
        // cập nhật rank dưới thách đấu
        UpdateRank1V1Normal(_isWin);
    }
    // cập nhật rank 1v1 khi dưới thách đấu
    static void UpdateRank1V1Normal(bool _isWin)
    {
        // cập nhật tiến trình trận đấu của rank hiện tại
        var _modeData = dataInit.Modes.FirstOrDefault(x => x.Mode == MODE.ONE_VS_ONE);
        var _progress = data.OneVsOne.MatchProgresses;
        var _lastKey = _progress.Last();
        bool _isLastKey = false;
        // Thay đổi trạng thái của trận đấu gần nhất vừa xong
        var _item = _progress.FirstOrDefault(x => x.Result == MATCH_RESULT.NONE);
        if (_item != null)
        {
            _item.Result = _isWin ? MATCH_RESULT.WIN : MATCH_RESULT.LOSE;
            DPDebug.Log($"Thay đổi tiến trình trận thứ[{_item.Index}]--result[{_item.Result}]");
            data.OneVsOne.MatchProgresses = _progress;
            _isLastKey = _item.Index == _lastKey.Index;
            SaveData();
        }
        // điều kiện của rank hiện tại
        var _require = _modeData.RanksRequire.FirstOrDefault(x => x.Rank == data.OneVsOne.Rank);
        // số trận chưa đấu
        var _remain = _progress.Where(x => x.Result == MATCH_RESULT.NONE).ToList().Count;
        // số trận đã thắng
        var _winTotal = _progress.Where(x => x.Result == MATCH_RESULT.WIN).ToList().Count;
        // kiểm tra nếu tổng số trận còn lại k đủ để lên rank thì --- failed luôn
        var _isFailed = _require.Win - (_winTotal + _remain) > 0 ? true : false;
        DPDebug.Log($"Update rank 1v1:(Tổng trận[{_progress.Count}]--điều kiện[{_require.Win}]--thắng[{_winTotal}]--còn lại[{_remain}])--->Không thỏa mãn điều kiện[{_isFailed}]");
        if (_isFailed)
        {
            // TH thua luôn do k đủ số trận thắng để lên hạng
            // reset thông số cho người chơi đánh lại từ đầu của rank hiện tại
            data.OneVsOne.MatchProgresses = new List<MatchProgress>();
            data.OneVsOne.MatchProgresses = CreateMatchProgress(MODE.ONE_VS_ONE, data.OneVsOne.Rank);
            DPDebug.Log($"Reset lại số trận chơi và số trận thắng ở rank[{data.OneVsOne.Rank}] do không thắng đủ số trận");
            SaveData();
            return;
        }
        // TH vẫn còn đủ điều kiện để đánh tiếp
        // kiểm tra xem rank tăng hoặc không
        // lấy điều kiện của mốc rank hiện tại
        DPDebug.Log($"Rank 1v1 require--rank[{_require.Rank}]--winRequire[{_require.Win}]--LimitMatches[{_require.TotalMatches}]");
        // người chơi thắng vượt qua số trận cần của rank này mà không quá tổng số trận cho phép thì lên rank
        // người chơi chưa đạt tối đa số trận đấu cho phép của rank này
        if (_winTotal >= _require.Win)
        {
            // người chơi vượt qua mốc của rank này ---> nâng rank
            var _newRank = RankHelperPVP.PromoteRank(data.OneVsOne.Rank);
            data.OneVsOne.Rank = _newRank;
            // reset thông số cho người chơi đánh lại từ đầu của rank mới
            data.OneVsOne.MatchProgresses.Clear();
            data.OneVsOne.MatchProgresses = CreateMatchProgress(MODE.ONE_VS_ONE, _newRank);
            DPDebug.Log($"Tăng rank 1v1 mới lên--->[{data.OneVsOne.Rank}]");
        }
        else
        {
            // nếu không tăng rank thì kiểm tra xem nếu đã đạt max số lượt được phép chơi ở rank này
            if (_isLastKey)
            {
                // reset thông số cho người chơi đánh lại từ đầu của rank hiện tại
                data.OneVsOne.MatchProgresses = new List<MatchProgress>();
                data.OneVsOne.MatchProgresses = CreateMatchProgress(MODE.ONE_VS_ONE, data.OneVsOne.Rank);
                DPDebug.Log($"Reset lại số trận chơi và số trận thắng ở rank[{data.OneVsOne.Rank}] do không thắng đủ số trận");
            }
        }
        // lưu lại
        SaveData();
    }
    // cập nhật rank 1v1 khi đang ở rank thách đấu
    static void UpdateRank1V1Challenger(bool _isWin)
    {
        var _opponentChallengerPoint = OpponentCurrent.RankProcess.ChallengerPoint;
        var _myChallengerPoint = MyProcessInGame.RankProcess.ChallengerPoint;
        var _pointDelta = GetChallengerPointDelta1V1(_myChallengerPoint, _opponentChallengerPoint, _isWin);
        var _pointOld = data.OneVsOne.ChallengerPoint;
        // Không cho Challenger Point xuống âm.
        data.OneVsOne.ChallengerPoint = Mathf.Max(0, _pointOld + _pointDelta);
        DPDebug.Log($"Cập nhật rank 1v1 thách đấu--->A(user current point)[{_myChallengerPoint}]--" +
            $"B(opponent current point)[{_opponentChallengerPoint}]--D raw(B-A)[{_opponentChallengerPoint - _myChallengerPoint}]--" +
            $"Win[{_isWin}]--point delta[{_pointDelta}]--point cũ[{_pointOld}]-->point mới[{data.OneVsOne.ChallengerPoint}]");
        SaveData();
    }


    #endregion

    #region CHIPS

    public static void ClearCacheChip(int _idChip)
    {
        // clear on both casual and ranked
        FighterDataCache.InvalidateByContext(AIRPORT_CONTEXT.PVPCalsual);
        FighterDataCache.InvalidateByContext(AIRPORT_CONTEXT.PvpRank);

        var _chip = Chip.GetProcess(_idChip);

        // check casual mode
        if (data.Casual != null && data.Casual.Airport != null)
        {
            var _slotsCasual = data.Casual.Airport.Slots.FirstOrDefault(x => x.IsEquippedChip(AIRPORT_CONTEXT.PVPCalsual) &&
                x.EquippedChip.Id == _idChip);
            if (_slotsCasual != null)
                _slotsCasual.EquippedChip = _chip;
        }

        // check ranked mode
        if (data.OneVsOne != null && data.OneVsOne.Airport != null)
        {
            var _slotsRanked = data.OneVsOne.Airport.Slots.FirstOrDefault(x => x.IsEquippedChip(AIRPORT_CONTEXT.PvpRank) &&
                x.EquippedChip.Id == _idChip);
            if (_slotsRanked != null)
                _slotsRanked.EquippedChip = _chip;
        }
        SaveData();
    }

    #endregion

    #region [BOOSTERS]

    // day of year của booster premium
    //static int BoosterDayOfYear
    //{
    //    get => data.Booster.DayOfYear;
    //    set { data.Booster.DayOfYear = value; SaveData(); }
    //}
    static string BoosterLastOpen
    {
        get
        {
            if (string.IsNullOrEmpty(data.Booster.LastOpen))
            {
                data.Booster.LastOpen = DPUtils.GetUtcNow.AddDays(-1).Ticks.ToString();
                SaveData();
            }
            else
            {
                if (!DPUtils.ParseTicks(data.Booster.LastOpen, out var _))
                {
                    data.Booster.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
                    SaveData();
                }
            }
            return data.Booster.LastOpen;
        }
    }

    static void OnNewDayBooster()
    {
        //if (BoosterDayOfYear == DPUtils.GetUtcNow.DayOfYear) return;
        if (!DPUtils.IsNewDay(BoosterLastOpen)) return;
        // sang ngày mới
        //BoosterDayOfYear = DPUtils.GetUtcNow.DayOfYear;
        data.Booster.LastOpen = DPUtils.GetUtcNow.Ticks.ToString();
        // nếu đã mua gói premium thì kiểm tra thời gian còn lại của gói
        if (data.Booster.IsPremium)
        {
            // Hết thời gian gói premium
            var _remain = (data.Booster.PremiumFinish.ConvertToDateTimeUtc() - DPUtils.GetUtcNow).TotalSeconds;
            DPDebug.Log($"Ngày mới kiểm tra gói premium booster PVP---remain[{_remain}]");
            if (_remain <= 0)
            {

                CancelBoosterPremium();
            }
            return;
        }
        // nếu chưa mua gói premium thì reset tất cả booster về trạng thái ban đầu
        foreach (var _booster in data.Booster.Boosters)
        {
            _booster.IsActive = false;
            _booster.IsFree = true;
            _booster.Duration = 0;
            _booster.CountWatchAds = 0;
        }
        SaveData();
    }

    // nâng cấp booster thành premium
    public static void UpgradeBoosterToPremium()
    {
        data.Booster.IsPremium = true;
        data.Booster.PremiumStart = DPUtils.GetUtcNow.Ticks.ToString();
        var _totalSeconds = DPUtils.Convert(7, TimeType.Day, TimeType.Seconds);
        data.Booster.PremiumFinish = DPUtils.GetUtcNow.AddSeconds(_totalSeconds).Ticks.ToString();
        // bật hết tất cả các booster
        foreach (var _item in data.Booster.Boosters)
        {
            _item.IsActive = true;
            _item.Duration = 0;
            _item.CountWatchAds = 0;
        }
        SaveData();
    }

    public static void GrantFromMail()
    {
        var _now = DPUtils.GetUtcNow;
        var _currentFinish = string.IsNullOrEmpty(data.Booster.PremiumFinish) ? _now : data.Booster.PremiumFinish.ConvertToDateTimeUtc();
        var _base = _currentFinish > _now ? _currentFinish : _now;

        data.Booster.IsPremium = true;
        data.Booster.PremiumStart = _now.Ticks.ToString();
        var _totalSeconds = DPUtils.Convert(7, TimeType.Day, TimeType.Seconds);
        data.Booster.PremiumFinish = _base.AddSeconds(_totalSeconds).Ticks.ToString();
        // bật hết tất cả các booster
        foreach (var _item in data.Booster.Boosters)
        {
            _item.IsActive = true;
            _item.Duration = 0;
            _item.CountWatchAds = 0;
        }
        SaveData();
    }

    // huỷ gói premium khi hết thời gian
    public static void CancelBoosterPremium()
    {
        if (!data.Booster.IsPremium) return;
        //
        DPDebug.Log("Hết thời gian gói premium booster PVP");
        data.Booster.IsPremium = false;
        data.Booster.PremiumStart = string.Empty;
        data.Booster.PremiumFinish = string.Empty;
        // reset tất cả booster về trạng thái ban đầu
        foreach (var _booster in data.Booster.Boosters)
        {
            _booster.IsActive = false;
            _booster.IsFree = true;
            _booster.Duration = 0;
            _booster.CountWatchAds = 0;
        }
        SaveData();
    }

    public static BoosterProcess BoosterCurrent
    {
        get
        {
            // nếu chưa có data booster thì khởi tạo mới
            if (data.Booster == null)
            {
                var _booster = new BoosterProcess
                {
                    IsPremium = false,
                    //DayOfYear = 0,
                    PremiumStart = string.Empty,
                    PremiumFinish = string.Empty,
                    Boosters = new List<BoosterItemProcess>()
                    {
                        new BoosterItemProcess
                            { Index = 0, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                        new BoosterItemProcess
                            { Index = 1, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                        new BoosterItemProcess
                            { Index = 2, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                    }
                };
                data.Booster = _booster;
                SaveData();
            }
            if (data.Booster.Boosters == null || data.Booster.Boosters.Count <= 0)
            {
                data.Booster.Boosters = new List<BoosterItemProcess>()
                {
                    new BoosterItemProcess
                    { Index = 0, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                    new BoosterItemProcess
                    { Index = 1, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                    new BoosterItemProcess
                    { Index = 2, IsFree = true, CountWatchAds = 0, IsActive = false, Duration = 0, IsOn = true },
                };
                SaveData();
            }
            return data.Booster;
        }
    }

    // cập nhật tất cả booster khi thoát game
    public static void AllBoosterUpdate()
    {
        if (data.Booster.IsPremium) return; // nếu đã mua gói premium thì k đếm time này
        //
        foreach (var _booster in data.Booster.Boosters)
        {
            if (!_booster.IsActive) continue; // chưa mở
            //
            _booster.Duration -= OnlineManager.Instance.GetOnlineTimeBooster(_booster.Index);
            Debug.Log($"Cập nhật booster PVP index[{_booster.Index}]--duration[{_booster.Duration}]");
            if (_booster.Duration < 0)
            {
                // hết thời gian sử dụng booster
                if (_booster.IsFree)// nếu là lượt free thì tắt free đi
                    _booster.IsFree = false;
                _booster.IsActive = false;
                _booster.Duration = 0;
            }
            SaveData();
            OnlineManager.Instance.SetOnlineTimeBooster(_booster.Index, 0f);
        }
    }
    public static BoosterData GetBoosterItemData(int _id)
    {
        return dataInit.Boosters.FirstOrDefault(x => x.Index == _id);
    }
    // cập nhật trạng thái của 1 booster theo index
    public static void BoosterUpdate(int _index, BoosterItemProcess _newProcess)
    {
        if (_index < 0 || _index >= data.Booster.Boosters.Count) return;
        //
        Debug.Log($"Cập nhật booster PVP index[{_index}]--isActive[{_newProcess.IsActive}]--isFree[{_newProcess.IsFree}]" +
            $"--duration[{_newProcess.Duration}]--countWatchAds[{_newProcess.CountWatchAds}]");
        data.Booster.Boosters[_index] = _newProcess;
        SaveData();
    }
    // hàm lấy % của booster theo index trong data
    public static float GetDataBoosterValue(int _index, bool _isPremium)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        if (_isPremium) return dataInit.Boosters[_index].Premium;
        return dataInit.Boosters[_index].Normal;
    }
    // hàm lấy % của booster theo index trong process
    public static float GetBoosterValue(int _index)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        // TH booster này đang chưa mở
        if (!data.Booster.Boosters[_index].IsActive) return 0f;
        // TH booster này đang tắt không sử dụng
        if (!data.Booster.Boosters[_index].IsOn) return 0f;
        // TH đã mua gói premium
        if (data.Booster.IsPremium) return dataInit.Boosters[_index].Premium;
        // TH default
        return dataInit.Boosters[_index].Normal;
    }
    public static void BoosterDeactive(int _index)
    {
        if (!data.Booster.Boosters[_index].IsActive) return;
        // nếu là lượt free thì tắt free đi
        if (data.Booster.Boosters[_index].IsFree)
            data.Booster.Boosters[_index].IsFree = false;
        data.Booster.Boosters[_index].IsActive = false;
        data.Booster.Boosters[_index].Duration = 0f;
        SaveData();
    }

    // hàm lấy thời gian đếm ngược còn lại của booster khi đang được sử dụng
    public static double GetBoosterTimeLeft(int _index)
    {
        if (!data.Booster.Boosters[_index].IsActive) return -1f;
        // nếu đã mua gói premium thì trả về thời gian còn lại của gói
        if (data.Booster.IsPremium)
        {
            double _remain = (data.Booster.PremiumFinish.ConvertToDateTimeUtc() - DPUtils.GetUtcNow).TotalSeconds;
            DPDebug.Log($"Thời gian còn lại của gói premium booster PVP: {data.Booster.PremiumFinish}--remain[{_remain}]");
            return _remain;
        }
        // nếu chưa mua gói premium thì trả về thời gian còn lại của gói khi xem ads
        return data.Booster.Boosters[_index].Duration;
    }

    #endregion

    #region [RANKED]

    public static string EndTimeEvent = string.Empty;
    // Chỉ event ONE_VS_ONE ACTIVE mới được chơi và cộng tiến độ PVP Season.
    public static bool IsRankedEventActive => !string.IsNullOrEmpty(EndTimeEvent);
    // các loại event trên server thuộc về PVP, thêm mode pvp mới thì bổ sung ở đây
    public static bool IsPVPEventType(EVENT_TYPE _type)
    {
        switch (_type)
        {
            case EVENT_TYPE.ONE_VS_ONE:// chế độ RANKED 1v1
                return true;
            default: return false;
        }
    }
    // đổi loại event trên server thành mode pvp tương ứng, không phải event pvp thì trả MODE.NONE
    public static MODE GetModeFromEventType(EVENT_TYPE _type)
    {
        switch (_type)
        {
            case EVENT_TYPE.ONE_VS_ONE: return MODE.ONE_VS_ONE;
            default: return MODE.NONE;
        }
    }
    // đổi mode pvp thành loại event trên server, dùng khi báo server đã nhận quà. Ngược của GetModeFromEventType
    public static EVENT_TYPE GetEventTypeFromMode(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.ONE_VS_ONE: return EVENT_TYPE.ONE_VS_ONE;
            default: return EVENT_TYPE.NONE;
        }
    }
    // mode có tính điểm XẾP HẠNG. Các field RankedPoint* nằm ở Process (dùng chung mọi mode) nên chỉ
    // mode xếp hạng mới được phép reset chúng khi vào mùa mới
    public static bool IsRankedMode(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.ONE_VS_ONE: return true;
            default: return false;
        }
    }
    // tag ghi log nhận quà theo mode. Giữ "1V1" cho ONE_VS_ONE để không vỡ analytics đang chạy
    static string GetClaimLogTag(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.ONE_VS_ONE: return "1V1";
            default: return _mode.ToString();
        }
    }
    // hàm lấy quà thách đấu dựa theo rank
    public static ChallengerReward GetChallengerReward(MODE _mode, int _rank)
    {
        var _rewards = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode).ChallengerRewards;
        return _rewards.FirstOrDefault(_c => _rank >= _c.MinRank && _rank <= _c.MaxRank);
    }
    // nhận quà khi kết thúc mùa
    public static void ClaimEventReward(MODE _mode, int _rankChallenger, System.Action _onCompleted = null)
    {
        var _rankProcess = GetRankProcess(_mode);
        if (_rankProcess == null)
        {
            DPDebug.LogError($"[PVP] Thiếu RankProcess của mode [{_mode}] -> không trả được quà cuối mùa.");
            if (_onCompleted != null) _onCompleted.Invoke();
            return;
        }
        EndSeason(_mode);// chốt mùa cũ (khung avatar theo rank hiện tại) TRƯỚC khi cộng quà
        // nhận quà rank 
        DPDebug.Log($"PVP nhận quà rank--mode[{_mode}]--rank[{_rankProcess.Rank}]");
        var _modeDataClaim = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
        var _rankReward = _modeDataClaim == null || _modeDataClaim.NormalRankRewards == null
            ? null
            : _modeDataClaim.NormalRankRewards.FirstOrDefault(x => x.Rank == _rankProcess.Rank);
        if (_rankReward != null)
            Rewards.AddReward(_rankReward.Rewards, _whereLogReport: $"CLAIM_PVP_{GetClaimLogTag(_mode)}_RANK");
        else
            DPDebug.LogWarning($"[PVP] Mode [{_mode}] thiếu config NormalRankRewards cho rank [{_rankProcess.Rank}] -> không có quà rank.");
        // nhận quà thách đấu nếu đạt
        if (_rankProcess.Rank != RANK.CHALLENGER)
        {
            if (_onCompleted != null) _onCompleted.Invoke();
            return;// chưa lên rank thách đấu
        }
        var _rewardsChallenger = GetChallengerReward(_mode, _rankChallenger);
        if (_rewardsChallenger == null)
        {
            if (_onCompleted != null) _onCompleted.Invoke();
            return;
        }
        // nhận quà thách đấu theo top
        DPDebug.Log($"PVP nhận quà thách đấu--mode[{_mode}]--top[{_rankChallenger}]");
        Rewards.AddReward(_rewardsChallenger.Rewards, _whereLogReport: $"CLAIM_PVP_{GetClaimLogTag(_mode)}_CHALLENGER");
        if (_onCompleted != null) _onCompleted.Invoke();
    }
    public static PlayerProcess FindOpponent(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.CASUAL:// casual thì tìm bot
                OpponentCurrent = FindBot(_mode);
                break;
            default: OpponentCurrent = null; break;
        }
        return OpponentCurrent;
    }
    public static void SetOpponentProcess(PlayerProcess _opponentProcess)
    {
        _opponentProcess.ScoreCurrent = GenerateScoreOpponent(_opponentProcess);
        OpponentCurrent = _opponentProcess;
    }
    // Điểm thi đấu của đối thủ: chưa đủ window thì random(0.8~1.1), đủ window thì random tiếp trên record thật đã chọn.
    // Logic cũ (chuỗi thắng/thua +-5%, chênh lệch X x hệ số C) đã bỏ theo công thức mới của GD.
    static float GenerateScoreOpponent(PlayerProcess _opponentProcess)
    {
        if (_opponentProcess == null || _opponentProcess.RankProcess == null)
        {
            Debug.LogError("GenerateScoreOpponent: process của đối thủ null");
            return 0f;
        }
        var _scoreCurrentOpponent = GetScoreForMatchRandom(modeCurrent, _opponentProcess.RankProcess);
        DPDebug.Log($"Tính điểm cho đối thủ PVP--mode[{modeCurrent}]--score for match của mình[{GetScoreForMatch(modeCurrent)}]" +
            $"--AVGSCORE đối thủ[{_opponentProcess.RankProcess.AvgScore}]" +
            $"--score for match đối thủ[{CalculateScoreForMatch(modeCurrent, _opponentProcess.RankProcess)}]" +
            $"-->score current opponent[{_scoreCurrentOpponent}]");
        return _scoreCurrentOpponent;
    }

    #region [AVG SCORE]
    // Công thức GD:
    // AVGSCORE          = HighScores của preseason x hệ số rank cuối preseason (đồng .2 -> thách đấu .8)
    // Chưa đủ 10 trận: điểm mang ra thi đấu = (AVGSCORE x số trận còn thiếu + tổng điểm các trận đã chơi) / 10.
    // Đủ 10 trận: bỏ 1 score cao nhất + 1 score thấp nhất, tính trung bình 8 score còn lại,
    // rồi chọn score thật gần giá trị trung bình nhất để dùng cho đối thủ asynchronous.
    // Nhánh chưa đủ 10 trận nhân random(0.8~1.1); nhánh đủ 10 nhân random riêng theo config (mặc định 0.9~1.0).
    // CHỈ lưu AvgScore + RecentMatchScores. Điểm mang ra thi đấu luôn tính lại từ 2 giá trị đó.

    // lấy config AVGSCORE theo mode
    static PVPSO.AvgScoreData GetAvgScoreData(MODE _mode)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        var _dataMode = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode);
        if (_dataMode == null || _dataMode.AvgScore == null) return new PVPSO.AvgScoreData();// dùng giá trị mặc định
        return _dataMode.AvgScore;
    }
    // chốt AVGSCORE khi bắt đầu mùa giải mới. PHẢI gọi TRƯỚC khi tụt rank và reset HighScores
    static void SetupAvgScoreNewSeason(MODE _mode, RankProcess _process)
    {
        if (_process == null) return;
        var _config = GetAvgScoreData(_mode);
        var _coeff = _config.GetCoeff(_process.Rank);// hệ số của rank CUỐI preseason
        _process.AvgScore = Mathf.Round(_process.HighScores * _coeff);
        if (_process.RecentMatchScores == null) _process.RecentMatchScores = new List<float>();
        _process.RecentMatchScores.Clear();// điểm các trận mùa cũ không mang sang mùa mới
        DPDebug.Log($"PVP chốt AVGSCORE mùa mới--mode[{_mode}]--rank preseason[{_process.Rank}]--high score preseason[{_process.HighScores}]" +
            $"--hệ số[{_coeff}]--AVGSCORE[{_process.AvgScore}]--score for match[{CalculateScoreForMatch(_mode, _process)}]");
    }
    // ghi nhận điểm 1 trận hợp lệ vừa đánh xong, chỉ giữ lại N trận gần nhất
    static void AddMatchScore(MODE _mode, RankProcess _process, float _score)
    {
        if (_process == null) return;
        var _config = GetAvgScoreData(_mode);
        if (_process.RecentMatchScores == null) _process.RecentMatchScores = new List<float>();
        _process.RecentMatchScores.Add(Mathf.Round(_score));
        while (_process.RecentMatchScores.Count > _config.WindowSize)
            _process.RecentMatchScores.RemoveAt(0);// bỏ trận cũ nhất ra khỏi cửa sổ 10 trận
        DPDebug.Log($"PVP ghi nhận điểm trận đấu--mode[{_mode}]--điểm trận này[{_score}]--số trận trong cửa sổ[{_process.RecentMatchScores.Count}/{_config.WindowSize}]" +
            $"--AVGSCORE[{_process.AvgScore}]--score for match mới[{CalculateScoreForMatch(_mode, _process)}]");
    }
    // tính điểm mang ra thi đấu (deterministic, chưa nhân random)
    public static float CalculateScoreForMatch(MODE _mode, RankProcess _process, bool _showDebug = false)
    {
        if (_process == null) return 0f;
        var _config = GetAvgScoreData(_mode);
        var _window = _config.WindowSize;
        var _total = 0f;
        var _count = 0;
        if (_process.RecentMatchScores != null)
        {
            _count = Mathf.Min(_process.RecentMatchScores.Count, _window);
            for (int i = _process.RecentMatchScores.Count - _count; i < _process.RecentMatchScores.Count; i++)
                _total += _process.RecentMatchScores[i];
        }
        float _rs;
        if (_count >= _window && _window >= 3)
        {
            // Lấy đúng window score gần nhất. Sort để bỏ đúng 1 score thấp nhất và 1 score cao nhất.
            var _scoresRecent = new List<float>(_window);
            for (int i = _process.RecentMatchScores.Count - _window; i < _process.RecentMatchScores.Count; i++)
                _scoresRecent.Add(_process.RecentMatchScores[i]);
            var _scoresTrimmed = new List<float>(_scoresRecent);
            _scoresTrimmed.Sort();
            var _scoreMinRemoved = _scoresTrimmed[0];
            var _scoreMaxRemoved = _scoresTrimmed[_scoresTrimmed.Count - 1];
            _scoresTrimmed.RemoveAt(_scoresTrimmed.Count - 1);// bỏ 1 score cao nhất
            _scoresTrimmed.RemoveAt(0);// bỏ 1 score thấp nhất

            var _trimmedTotal = 0f;
            for (int i = 0; i < _scoresTrimmed.Count; i++)
                _trimmedTotal += _scoresTrimmed[i];
            var _trimmedAverage = _trimmedTotal / _scoresTrimmed.Count;

            // Chọn score thật gần trung bình nhất. Nếu khoảng cách bằng nhau, ưu tiên score thấp hơn
            // do danh sách đã được sort tăng dần, giúp kết quả luôn deterministic.
            _rs = _scoresTrimmed[0];
            var _nearestDistance = Mathf.Abs(_rs - _trimmedAverage);
            for (int i = 1; i < _scoresTrimmed.Count; i++)
            {
                var _distance = Mathf.Abs(_scoresTrimmed[i] - _trimmedAverage);
                if (_distance >= _nearestDistance) continue;
                _nearestDistance = _distance;
                _rs = _scoresTrimmed[i];
            }

            if (_showDebug)
            {
                DPDebug.Log($"PVP SCORE [ĐỦ 10 TRẬN]--mode[{_mode}]" +
                    $"--recent matches[{_count}/{_window}]" +
                    $"--{_window} score gần nhất[{string.Join(", ", _scoresRecent.Select(x => x.ToString("0.##")).ToArray())}]" +
                    $"--loại thấp nhất[{_scoreMinRemoved}]--loại cao nhất[{_scoreMaxRemoved}]" +
                    $"--{_scoresTrimmed.Count} score còn lại[{string.Join(", ", _scoresTrimmed.Select(x => x.ToString("0.##")).ToArray())}]" +
                    $"--trung bình[{_trimmedAverage}]--record gần nhất[{_rs}]" +
                    $"--khoảng cách[{_nearestDistance}]");
            }
        }
        else
        {
            _rs = (_process.AvgScore * (_window - _count) + _total) / _window;
            if (_showDebug)
            {
                DPDebug.Log($"PVP SCORE [CHƯA ĐỦ 10 TRẬN]--mode[{_mode}]" +
                    $"--recent matches[{_count}/{_window}]--số trận còn thiếu[{_window - _count}]" +
                    $"--recent scores[{(_count > 0 ? string.Join(", ", _process.RecentMatchScores.Skip(_process.RecentMatchScores.Count - _count).Select(x => x.ToString("0.##")).ToArray()) : "trống")}]" +
                    $"--AVGSCORE[{_process.AvgScore}]--phần điểm AVGSCORE[{_process.AvgScore * (_window - _count)}]" +
                    $"--tổng điểm recent[{_total}]--score trước random[{_rs}]");
            }
        }
        if (float.IsNaN(_rs) || float.IsInfinity(_rs))
        {
            Debug.LogError($"ScoreForMatch invalid: {_rs}");
            _rs = 0f;
        }
        return Mathf.Round(_rs);
    }
    // điểm dùng trong trận: chưa đủ window thì random(0.8~1.1), đủ window thì random trên record thật.
    public static float GetScoreForMatchRandom(MODE _mode, RankProcess _process)
    {
        if (_process == null) return 0f;
        var _config = GetAvgScoreData(_mode);
        var _scoreForMatch = CalculateScoreForMatch(_mode, _process, true);
        var _count = _process.RecentMatchScores == null
            ? 0
            : Mathf.Min(_process.RecentMatchScores.Count, _config.WindowSize);
        // Đủ 10 trận: CalculateScoreForMatch đã chọn một score thật gần trimmed average nhất.
        // Nhân thêm hệ số riêng để điều chỉnh độ khó của đối thủ asynchronous.
        if (_count >= _config.WindowSize && _config.WindowSize >= 3)
        {
            var _randomMin = Mathf.Min(_config.FullWindowRandomMin, _config.FullWindowRandomMax);
            var _randomMax = Mathf.Max(_config.FullWindowRandomMin, _config.FullWindowRandomMax);
            var _factorFullWindow = UnityEngine.Random.Range(_randomMin, _randomMax);
            var _rsFullWindow = Mathf.Round(_scoreForMatch * _factorFullWindow);
            DPDebug.Log($"PVP SCORE [ĐỦ 10 TRẬN - KẾT QUẢ]--mode[{_mode}]" +
                $"--số trận[{_count}/{_config.WindowSize}]" +
                $"--điểm thật[{_scoreForMatch}]" +
                $"--khoảng random[{_randomMin:0.###}~{_randomMax:0.###}]" +
                $"--hệ số random[{_factorFullWindow:0.###}]" +
                $"--điểm đối thủ sau random[{_rsFullWindow}]");
            return _rsFullWindow;
        }
        var _factor = UnityEngine.Random.Range(_config.RandomMin, _config.RandomMax);
        var _rs = Mathf.Round(_scoreForMatch * _factor);
        DPDebug.Log($"PVP SCORE [CHƯA ĐỦ 10 TRẬN - KẾT QUẢ]--mode[{_mode}]" +
            $"--score trước random[{_scoreForMatch}]--hệ số random[{_factor}]--score sử dụng[{_rs}]");
        return _rs;
    }
    // AVGSCORE đang lưu của người chơi theo mode (dùng cho UI/log)
    public static float GetAvgScore(MODE _mode)
    {
        var _process = GetRankProcess(_mode);
        return _process == null ? 0f : _process.AvgScore;
    }
    // điểm mang ra thi đấu của người chơi theo mode, tính lại từ AVGSCORE + điểm các trận gần nhất (dùng cho UI/log)
    public static float GetScoreForMatch(MODE _mode)
    {
        return CalculateScoreForMatch(_mode, GetRankProcess(_mode));
    }
    // lấy mốc điểm chuẩn của 1 rank trong config bot, dùng làm SÀN cho user chưa có điểm nào
    static float GetBotHighScores(RANK _rank)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        var _dataMode = dataInit.Modes.FirstOrDefault(x => x.Mode == MODE.CASUAL);
        if (_dataMode == null || _dataMode.Bots == null) return 0f;
        var _bot = _dataMode.Bots.FirstOrDefault(x => x.Rank == _rank);
        if (_bot == null)
        {
            // rank NONE hoặc chưa config -> lấy mốc thấp nhất là đồng 3
            _bot = _dataMode.Bots.FirstOrDefault(x => x.Rank == RANK.BRONZE_3);
        }
        return _bot == null ? 0f : _bot.HighScores;
    }
    // Chuẩn hoá dữ liệu điểm của ĐỐI THỦ lấy từ server trước khi mang ra so sánh.
    // CheckAvgScoreData chỉ chạy trên máy chủ sở hữu save, KHÔNG chạy cho snapshot đối thủ, nên phải vá tại đây:
    // - đối thủ chưa mở game mùa mới -> save còn là mùa cũ (AVGSCORE + điểm 10 trận của mùa trước)
    // - đối thủ chưa update bản có AVGSCORE -> JSON thiếu field, deserialize ra 0
    // - đối thủ là user mới, chưa có HighScores -> lấy sàn theo config bot của rank đó
    // KHÔNG gọi SaveData: đây là data của người khác, chỉ sống trong ván này.
    public static void NormalizeOpponentScore(MODE _mode, RankProcess _process)
    {
        if (_process == null) return;
        var _config = GetAvgScoreData(_mode);
        if (_process.RecentMatchScores == null) _process.RecentMatchScores = new List<float>();
        var _seasonVersionCurrent = _mode == MODE.ONE_VS_ONE ? SeasonVersionRanked : INVALID_SEASON_VERSION;
        var _isSameSeason = _mode == MODE.ONE_VS_ONE && _process.SeasonVersion == _seasonVersionCurrent;
        if (!_isSameSeason)
        {
            // quy đổi lại y như lúc vào mùa mới: bỏ hết điểm trận cũ, AVGSCORE = HighScores x hệ số rank
            _process.RecentMatchScores.Clear();
            var _base = _process.HighScores > 0 ? _process.HighScores : GetBotHighScores(_process.Rank);
            _process.AvgScore = Mathf.Round(_base * _config.GetCoeff(_process.Rank));
            DPDebug.Log($"PVP chuẩn hoá điểm đối thủ--mode[{_mode}]--season đối thủ[{_process.SeasonVersion}]/hiện tại[{_seasonVersionCurrent}]" +
                $"--rank[{_process.Rank}]--high score[{_process.HighScores}]--base[{_base}]--AVGSCORE[{_process.AvgScore}]" +
                $"--score for match[{CalculateScoreForMatch(_mode, _process)}]");
        }
        else if (_process.AvgScore <= 0 && _process.RecentMatchScores.Count < _config.WindowSize)
        {
            // Cùng season nhưng snapshot thiếu AVGSCORE: chỉ tính bù phần AvgScore cho các ô còn thiếu.
            // Giữ nguyên điểm các trận gần nhất; đủ window thì CalculateScoreForMatch không cần AvgScore.
            var _base = _process.HighScores > 0 ? _process.HighScores : GetBotHighScores(_process.Rank);
            _process.AvgScore = Mathf.Round(_base * _config.GetCoeff(_process.Rank));
            DPDebug.Log($"PVP vá AVGSCORE đối thủ cùng season--mode[{_mode}]--season[{_process.SeasonVersion}]" +
                $"--rank[{_process.Rank}]--high score[{_process.HighScores}]--base[{_base}]--AVGSCORE[{_process.AvgScore}]" +
                $"--recent matches[{_process.RecentMatchScores.Count}/{_config.WindowSize}]" +
                $"--score for match[{CalculateScoreForMatch(_mode, _process)}]");
        }
    }
    // vá dữ liệu: user cũ (hoặc data từ server) chưa có AVGSCORE thì tính bù từ HighScores + rank hiện tại
    static void CheckAvgScoreData(MODE _mode, RankProcess _process)
    {
        if (_process == null) return;
        var _isDirty = false;
        if (_process.RecentMatchScores == null)
        {
            _process.RecentMatchScores = new List<float>();
            _isDirty = true;
        }
        if (_process.AvgScore <= 0)
        {
            var _config = GetAvgScoreData(_mode);
            // user chưa có HighScores (mới hoàn toàn) thì lấy sàn theo config bot của rank, tránh AVGSCORE = 0
            var _base = _process.HighScores > 0 ? _process.HighScores : GetBotHighScores(_process.Rank);
            _process.AvgScore = Mathf.Round(_base * _config.GetCoeff(_process.Rank));
            _isDirty = true;
            DPDebug.Log($"PVP vá AVGSCORE cho user cũ--mode[{_mode}]--rank[{_process.Rank}]--high score[{_process.HighScores}]--base[{_base}]--AVGSCORE[{_process.AvgScore}]");
        }
        if (_isDirty) SaveData();
    }
    #endregion

    public static PlayerProcess Find1V1BotWithRank()
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        var _rank = data.OneVsOne.Rank;
        var _data = dataInit.Modes.FirstOrDefault(x => x.Mode == MODE.CASUAL).Bots.FirstOrDefault(x => x.Rank == _rank);
        if (_data == null) return null;
        // random avatar
        var _avatarID = Avatar.GetAllAvatar().RandomElement().AvatarID;
        // random quân hàm = quân hàm của người chơi -1
        var _myMilitaryRank = MilitaryRank.GetProcess().RankHighest;
        var _militaryRank = MILITARY_RANK.LEVEL_1;
        // random country
        var _country = Countries.Random();
        // nếu quân hàm lớn hơn LEVEL_1 thì mới trừ 1
        if (_myMilitaryRank != MILITARY_RANK.NONE && _myMilitaryRank != MILITARY_RANK.LEVEL_1)
            _militaryRank = _myMilitaryRank.Decrement();
        // random tournament rank
        var _tournamentRank = DPUtils.RandomEnumExclude(TOURNAMENT_RANK_TIER.NONE);
        var _tournamentDivision = DPUtils.RandomEnumExclude(TournamentDivision.None);
        // random CP = CP của người chơi +-10%
        var _opponentCP = data.OneVsOne.Airport.CP * (1 + UnityEngine.Random.Range(-0.1f, 0.1f));
        range = (_data.HighScores * (_data.Range / 100f));
        minScores = _data.HighScores - range;
        maxScores = _data.HighScores + range;
        var _scoreBot = Mathf.Round(Random.Range(minScores, maxScores));
        // random fighter ID
        RankProcess _rankProcess = new RankProcess
        {
            Rank = _rank,
            Airport = new AirportProcess
            {
                Context = AIRPORT_CONTEXT.PvpRank,
                IsUnlocked = true,
                CP = _opponentCP,
                IDFighter = GetBotFighterID(data.OneVsOne.Airport.IDFighter),
            },
            HighScores = _scoreBot,
            AvgScore = Mathf.Round(_scoreBot * GetAvgScoreData(MODE.ONE_VS_ONE).GetCoeff(_rank)),
        };
        // cho bot đi theo ĐÚNG công thức của người chơi: số trận trong cửa sổ = số trận mình đã chơi,
        // nếu không thì đầu mùa mình bị nhân hệ số 0.2~0.8 mà bot vẫn full điểm -> bot mạnh hơn hẳn
        var _countPlayed = data.OneVsOne.RecentMatchScores == null
            ? 0
            : Mathf.Min(data.OneVsOne.RecentMatchScores.Count, GetAvgScoreData(MODE.ONE_VS_ONE).WindowSize);
        _rankProcess.RecentMatchScores = new List<float>();
        for (int i = 0; i < _countPlayed; i++)
            _rankProcess.RecentMatchScores.Add(Mathf.Round(Random.Range(minScores, maxScores)));
        //
        PlayerProcess _bot = new PlayerProcess
        {
            Name = DPUtils.RandomName(),
            AvatarID = _avatarID,
            FrameAvatarID = "Frame#1",
            Country = _country,
            MilitaryRankHighest = _militaryRank,
            TournamentRankHighest = _tournamentRank,
            TournamentDivisionHighest = _tournamentDivision,
            RankProcess = _rankProcess,
        };
        return _bot;
    }

    #endregion

    #region [IN GAME]

    // Trạng thái runtime của trận hiện tại. PendingMatch được lưu trong Process
    // để lần mở game sau vẫn xử thua nếu hệ điều hành kill app mà không gọi callback.
    static bool isMatchInProgress;
    static bool isMatchInvalid;
    static bool wasMatchForfeited;

    public static void BeginMatch()
    {
        if (isMatchInProgress) return;

        isMatchInProgress = true;
        wasMatchForfeited = false;
        isForfeitResultShown = false;
        // Chặn trường hợp callback tìm đối thủ hoàn tất khi app đã mất focus,
        // trước lúc trận kịp được đánh dấu bắt đầu.
        isMatchInvalid = !Application.isFocused;
        data.HasPendingMatch = true;
        data.PendingMatchMode = modeCurrent;
        matchResultStartDayID = SeasonDayID;
        data.PendingMatchSeasonDayID = matchResultStartDayID;
        SaveData();
        DPDebug.Log($"PVP begin match--invalid[{isMatchInvalid}]");

        if (isMatchInvalid)
            InvalidateCurrentMatch("application_not_focused_when_match_started");
    }
    static bool isForfeitResultShown;
    // Giữ ngày bắt đầu tới lúc ApplyMatchResult chạy. Cần tách khỏi HasPendingMatch vì pending
    // được clear sớm trước khi áp kết quả để chống xử lý trùng nếu app bị kill giữa callback.
    static string matchResultStartDayID = string.Empty;

    public static bool IsMatchInProgress => isMatchInProgress;
    public static bool WasMatchForfeited => wasMatchForfeited;

    public static bool InvalidateCurrentMatch(string _reason)
    {
        if (!isMatchInProgress) return false;
        isMatchInvalid = true;
        isMatchInProgress = false;
        wasMatchForfeited = true;
        isAddScoreIngame = false;
        ClearPendingMatch();

        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        if (_process == null)
        {
            SaveData();
            return true;
        }

        // Thoát/force quit luôn là thua. Vẫn xử lý stats, point, rank và mission
        // như một trận thua bình thường, chỉ không ghi RecentMatchScores.
        UpdateMatchScoreStats(_process);
        ApplyMatchResult(_process.Rank, false);
        DPDebug.LogWarning($"PVP forfeit current match--reason[{_reason}]--mode[{modeCurrent}]--score[{scoreCurrent}]");
        return true;
    }

    public static bool TryMarkForfeitResultShown()
    {
        if (!wasMatchForfeited || isForfeitResultShown) return false;

        isForfeitResultShown = true;
        return true;
    }

    static void ClearPendingMatch()
    {
        // Chụp lại trước khi xoá pending; OnSeasonMatchFinished dùng giá trị này để nhận biết
        // trận có chạy xuyên qua mốc đổi ngày hay không.
        matchResultStartDayID = data.PendingMatchSeasonDayID;
        data.HasPendingMatch = false;
        data.PendingMatchMode = MODE.NONE;
        data.PendingMatchSeasonDayID = string.Empty;
    }

    // Fallback cho force kill: callback pause/quit có thể không kịp chạy. Khi game mở lại,
    // pending match được tính là thua với score 0 và không thêm RecentMatchScores.
    static void ResolvePendingMatchAsLoss()
    {
        if (data == null || !data.HasPendingMatch) return;

        modeCurrent = data.PendingMatchMode;
        scoreCurrent = 0;
        isAddScoreIngame = false;
        isMatchInProgress = false;
        isMatchInvalid = true;
        ClearPendingMatch();

        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        if (_process == null)
        {
            SaveData();
            return;
        }

        ApplyMatchResult(_process.Rank, false);
        DPDebug.LogWarning($"PVP resolved pending match as loss--mode[{modeCurrent}]");
    }

    public static void InitLevel()
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        scoreCurrent = 0;
        isAddScoreIngame = true;
        SetRankCurrent();
        MyProcessInGame = GetMyProcess();
        DPDebug.Log($"mode current---{modeCurrent}--rank current-{rankCurrent}");
        waveDataCurrent = dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.WavesData.FirstOrDefault(x => x.Rank == rankCurrent);
        DPDebug.Log($"wave data current---{waveDataCurrent.Rank}--{waveDataCurrent.HP_Base}--{waveDataCurrent.DMG_Base}--{waveDataCurrent.Body_DMG_Base}");
        DPDebug.Log("PVP my process equiped award index--->" + MyProcessInGame.EquippedAwardIndex);
        waveProcessCurrent = new WaveProcess
        {
            HP = waveDataCurrent.HP_Base,
            DMG = waveDataCurrent.DMG_Base,
            BodyDMG = waveDataCurrent.Body_DMG_Base,
        };
        PointAddFinished = 0;
        cacheLifeTime = new();
        totalWavePlayed = 0;

        // lấy wave current
        waveIndexCurrent = 1;
        RandomWaveCurrent();
    }
    // bước nhảy để hiển thị booster trong game, chỉ tăng khi thua ván đấu
    static int stepShowBoosterInGame = 0;
    public static int StepShowBoosterInGame { set => stepShowBoosterInGame = value; get => stepShowBoosterInGame; }
    //
    public static bool IsShowTimePlayInGame
    {
        get
        {
            switch (modeCurrent)
            {
                case MODE.CASUAL: return true;  // luôn hiển thị ở chế độ casual
                case MODE.ONE_VS_ONE:// 1v1 chỉ hiển thị khi rank dưới thách đấu
                    if (data.OneVsOne.Rank == RANK.CHALLENGER)
                        return false;
                    return true;
                default: return true;
            }
        }
    }
    // phần trăm tăng vào base của Fighter khi vào chơi PVP
    public static float GetPercentIncreaseBaseInGame(int _pointLevel)
    {
        // công thức: tổng = win * mul (%)
        float _totalPercent = Mathf.RoundToInt(data.WinCount * GetMultipleIncrease(_pointLevel));
        return _totalPercent;
    }
    // hàm lấy multiple theo level point
    public static float GetMultipleIncrease(int _pointLevel)
    {
        var _mul = dataInit.RankedPointProgresses.FirstOrDefault(x => x.Level == _pointLevel).Multiple;
        return _mul;
    }
    public static double TotalSecondsInLevel => 120;

    // hàm lấy ra mốc point gần nhất thỏa mãn điều kiện
    static PVPSO.PointProgress? GetNearestPoint(float _point)
    {
        PVPSO.PointProgress? _best = null;

        foreach (var _item in dataInit.RankedPointProgresses)
        {
            if (_item.PointRequier <= _point)
            {
                if (_best == null || _item.PointRequier > _best.Value.PointRequier)
                    _best = _item;
            }
        }

        return _best;
    }
    // hàm tính phần trăm bonus khi chơi chế độ đấu rank tăng vào base của F
    public static float GetBonusIncreaseBase()
    {
        // k có bonus khi chơi casual
        if (modeCurrent == MODE.CASUAL) return 0;
        //
        var _bonus = GetPercentIncreaseBaseInGame(data.RankedPointLevel);
        return _bonus;
    }
    // lấy rank hiện tại của bản thân
    static void SetRankCurrent()
    {
        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        if (_process == null) return;
        //
        if (_process.Rank == RANK.NONE)
        {
            // nếu rank null thì gán rank mặc định là đồng 3
            _process.Rank = RANK.BRONZE_3;
            SaveData();
        }
        rankCurrent = _process.Rank;
    }

    // get process của bản thân trong ván chơi này
    public static PlayerProcess MyProcessInGame;

    static PlayerProcess GetMyProcess()
    {
        var _process = GetRankProcess().Clone();
        if (_process == null) return null;
        //
        var _militaryRank = MilitaryRank.GetProcess().RankHighest;
        var _rankTournamentDivision = Tournament.Division == TournamentDivision.None ? TournamentDivision.DIVISION_4 : Tournament.Division;
        var _awardEquippedIndex = Award.GetEquippedAwardIndex();
        var _awardLevelEquipped = 0;
        if (_awardEquippedIndex != -1)
            _awardLevelEquipped = Award.GetAwardItem(_awardEquippedIndex).Level;
        var _playerProcess = new PlayerProcess
        {
            AvatarID = Avatar.AvatarCurrent,
            FrameAvatarID = Frame.FrameCurrent,
            Name = Player.Name,
            Country = Player.Country,
            MilitaryRankHighest = _militaryRank == MILITARY_RANK.NONE ? MILITARY_RANK.LEVEL_1 : _militaryRank,
            TournamentRankHighest = Tournament.TournamentRankTier,
            TournamentDivisionHighest = _rankTournamentDivision,
            EquippedAwardIndex = _awardEquippedIndex,
            LevelAwardEquipped = _awardLevelEquipped,
            RankProcess = _process.Clone(),
        };
        return _playerProcess;
    }
    //
    public static bool IsAddScoreIngame
    {
        get => isAddScoreIngame;
    }
    static bool isAddScoreIngame = true;
    public static float PointAddFinished; // số pvp point được cộng thêm khi kết thúc ván chơi
    public static bool IsWin; // trạng thái thắng hay thua của ván chơi

    public static void OnLevelCompleted(System.Action _onCompleted = null)
    {
        isAddScoreIngame = false;// dừng cộng điểm trong game
        var _canAddRecentScore = isMatchInProgress && !isMatchInvalid;
        isMatchInProgress = false;// chống xử lý/lưu kết quả trận nhiều lần
        ClearPendingMatch();

        if (wasMatchForfeited)
        {
            // Kết quả thua đã được lưu ngay lúc app bị thoát; không xử lý lần hai.
            IsWin = false;
            if (_onCompleted != null) _onCompleted.Invoke();
            return;
        }

        var _process = modeCurrent == MODE.CASUAL ? data.Casual : data.OneVsOne;
        if (_process == null)
        {
            if (_onCompleted != null) _onCompleted.Invoke();
            return;
        }
        UpdateMatchScoreStats(_process);
        // Chỉ ghi nhận trận hợp lệ: không thoát giữa chừng và điểm cuối trận
        // phải lớn hơn 10% PvP High Score.
        var _minValidMatchScore = _process.HighScores * 0.1f;
        if (_canAddRecentScore && scoreCurrent > _minValidMatchScore)
        {
            AddMatchScore(modeCurrent, _process, scoreCurrent);
            SaveData();
            DPDebug.Log($"PVP add valid match successful--mode[{modeCurrent}]--score[{scoreCurrent}]" +
                $"--high score[{_process.HighScores}]--10% threshold[{_minValidMatchScore}]" +
                $"--recent matches[{_process.RecentMatchScores.Count}]");
        }
        Compare(_process.Rank);
        //
        if (_onCompleted != null) _onCompleted.Invoke();
    }

    static void UpdateMatchScoreStats(RankProcess _process)
    {
        if (_process == null) return;
        // cập nhật điểm cao nhất nếu vượt qua điểm cũ
        if (scoreCurrent > _process.HighScores)
        {
            _process.HighScores = scoreCurrent;
            SaveData();
        }
        // cập nhật AVG_KillTime và AVG_WaveScore nếu tốt hơn cái cũ
        if (cacheLifeTime != null && (_process.AVG_KillTime == 0 || _process.AVG_KillTime > AVG_KillTime))
        {
            // nếu giá trị hiện tại = 0 hoặc nhỏ hơn thì thay thế
            _process.AVG_KillTime = AVG_KillTime;
            SaveData();
        }
        if (totalWavePlayed > 0 && _process.AVG_WaveScore < AVG_WaveScore)
        {
            // nếu cao hơn thì tốt hơn
            _process.AVG_WaveScore = AVG_WaveScore;
            SaveData();
        }
    }
    static void Compare(RANK _rank)
    {
        // so sánh điểm với đối thủ nếu lớn > or = thì thắng
        var _isWin = OpponentCurrent != null && scoreCurrent >= OpponentCurrent.ScoreCurrent;
        ApplyMatchResult(_rank, _isWin);
    }

    static void ApplyMatchResult(RANK _rank, bool _isWin)
    {
        IsWin = _isWin;
        MilestoneProgression.AddPointAdditional(MilestoneProgressionSO.MILESTONE_POINT.PLAY_PVP, 1, IsWin);
        DPDebug.Log($"PVP completed--điểm của mình[{scoreCurrent}]--đối thủ[{OpponentCurrent?.ScoreCurrent ?? 0}]--Win[{IsWin}]");
        // tăng số trận thắng lên
        if (IsWin)
        {
            // TH thắng reset bước nhảy show booster trong game
            StepShowBoosterInGame = 0;
            data.WinCount += 1;
            SaveData();
            DPDebug.Log($"Tăng số trận thắng trong PVP lên-->[{data.WinCount}]");
        }
        else
        {
            // TH thua tăng bước nhảy show booster trong game
            StepShowBoosterInGame += 1;
            DPDebug.Log($"Tăng bước nhảy show booster trong PVP lên-->[{StepShowBoosterInGame}]");
        }
        // tặng pvp point
        GetPVPPointBonus(modeCurrent, _rank, IsWin);
        data.Point += PointAddFinished;
        // tăng point progress trong xếp hạng
        AddRankedPoint();
        // cập nhật rank mới
        UpdateRank(IsWin);
        SaveData();
        // nhiệm vụ mùa giải: thắng X trận + đạt X tổng điểm. Đặt SAU khi IsWin đã có giá trị.
        OnSeasonMatchFinished();
    }
    static void AddRankedPoint()
    {
        if (modeCurrent == MODE.NONE || modeCurrent == MODE.CASUAL) return;// k tăng khi chơi ở chế độ này
        // các chế độ xếp hạng đều được tăng
        data.RankedPointProgress += PointAddFinished;
        SaveData();
    }
    // hàm tính toán AVG
    public static float AVG_KillTime
    {
        get
        {
            float _totalTime = 0;
            for (int i = 0; i < cacheLifeTime.Count; i++)
            {
                _totalTime += cacheLifeTime[i];
            }
            if (cacheLifeTime.Count <= 0) return 0;
            DPDebug.Log($"Get AVG Kill Time---Total time[{_totalTime}]--Cache Life Time[{cacheLifeTime.Count}]");
            var _rs = _totalTime / cacheLifeTime.Count;
            if (float.IsNaN(_rs) || float.IsInfinity(_rs))
            {
                Debug.LogError("AVG_KillTime invalid: " + _rs);
                _rs = 0f; // hoặc giá trị default
            }
            return _rs;
        }
    }

    public static float AVG_WaveScore => scoreCurrent / totalWavePlayed;

    private static List<float> cacheLifeTime;
    private static int totalWavePlayed;

    public static void AddEnemyLifeTime(float newLifeTime)
    {
        cacheLifeTime.Add(newLifeTime);
    }

    // hàm lấy số pvp point được tăng khi xong màn chơi theo mode & rank
    static PointData pointMode;

    static void GetPVPPointBonus(MODE _mode, RANK _rank, bool _isWin)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        pointMode = dataInit.Modes.FirstOrDefault(x => x.Mode == _mode).Point;
        if (_mode == MODE.CASUAL)
        {
            // chế độ casual thì chỉ chung 1 chỉ số cho tất cả các rank, lấy rank. Lấy thông số của rank đầu tiên làm chung
            if (_isWin) PointAddFinished = pointMode.PVPPoints.First().Win_PVPPoint;
            else PointAddFinished = pointMode.PVPPoints.First().Lose_PVPPoint;
        }
        else
        {
            // các chế độ pvp khác thì lấy pvp point theo đúng mốc rank hiện tại của người chơi
            if (_isWin)
                PointAddFinished = pointMode.PVPPoints.FirstOrDefault(x => x.Rank == _rank).Win_PVPPoint;
            else
                PointAddFinished = pointMode.PVPPoints.FirstOrDefault(x => x.Rank == _rank).Lose_PVPPoint;
        }
    }

    // các thông số level
    static MODE modeCurrent;

    public static MODE ModeCurrent
    {
        get => modeCurrent;
        set => modeCurrent = value;
    }

    // tiến trình của đối thủ trong ván thi đấu này
    static PlayerProcess opponentCurrent;

    public static PlayerProcess OpponentCurrent
    {
        get => opponentCurrent;
        set => opponentCurrent = value;
    }

    static float scoreCurrent;

    public static float ScoreCurrent
    {
        get => scoreCurrent;
    }

    static RANK rankCurrent;

    // Wave data
    static WaveData waveDataCurrent;
    static WaveProcess waveProcessCurrent;

    public static WaveProcess WaveProcessCurrent
    {
        get => waveProcessCurrent;
    }

    // tổng HP booster tăng thêm(%)
    public static float HP_Bonus
    {
        get
        {
            var _percent = GetBoosterValue(0);
            return _percent;
        }
    }

    // tổng DMG booster tăng thêm(%)
    public static float DMG_Bonus
    {
        get
        {
            var _percent = GetBoosterValue(1);
            return _percent;
        }
    }

    // thời gian giãn cách giữa 2 lần sinh wave
    public static float DelayWave
    {
        get
        {
            var _base = dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.DelayWave;
            var _percent = GetBoosterValue(2);
            if (_percent == 0) return _base;
            _base = _base - (_base * (_percent / 100f));
            if (_base < 0) _base = 0;
            return _base;
        }
    }

    public static float MatchDuration => 120f; // thời gian chơi của 1 màn chơi là 2 phut = 120s


    // chuyển sang wave quái tiếp theo
    public static void NextWave()
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        // next wave data
        WaveProcess _newWave = new WaveProcess
        {
            HP = waveProcessCurrent.HP * waveDataCurrent.Factor,
            DMG = waveProcessCurrent.DMG * waveDataCurrent.Factor,
            BodyDMG = waveProcessCurrent.BodyDMG * waveDataCurrent.Factor,
        };
        waveProcessCurrent = _newWave;
        DPDebug.Log($"Next wave data---Wave index cur[{waveIndexCurrent}]--Total wave played[{totalWavePlayed}]---1");
        // next wave quái
        if (waveIndexCurrent == dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.PoolWaves.Count)
            waveIndexCurrent = 1;
        else
            waveIndexCurrent++;
        //
        RandomWaveCurrent();
        totalWavePlayed++;
        DPDebug.Log($"Next wave data---Wave index cur[{waveIndexCurrent}]--Total wave played[{totalWavePlayed}]---2");
    }

    // wave quái
    static int waveIndexCurrent;
    static EnemyWavePVP waveCurrent;

    public static EnemyWavePVP WaveCurrent
    {
        get => waveCurrent;
    }

    static void RandomWaveCurrent()
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        DPDebug.Log($"Random wave current---Wave index cur[{waveIndexCurrent}]");
        var _waves = dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.PoolWaves;
        waveCurrent = _waves.FirstOrDefault(x => x.Index == waveIndexCurrent).Waves.RandomElement();
    }

    // hàm tính scores của từng con quái dự trên thời gian sống của nó
    static float factor, eFactor, scoreAdd;

    public static float GetScore(float _time, ENEMY_RANK _eRank)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        factor = 1f;
        foreach (var _item in dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.ScoresData)
        {
            if (_time <= _item.Time)
            {
                factor = _item.Factor;
                break;
            }
        }

        eFactor = dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.EnemiesRank
            .FirstOrDefault(x => x.Rank == _eRank).Factor;
        scoreAdd = waveDataCurrent.ScoreBase * factor * eFactor;

        return scoreAdd;
    }

    public static void AddScoreInGame(float _scoreBonus)
    {
        if (!IsAddScoreIngame) return; // k tăng score in game
        // không tăng score nếu k ở trạng thái playing
        if (GamePlayController.State != GAMEPLAY_STATE.PLAYING) return;
        //
        scoreCurrent += scoreAdd;
    }
    public static void HackAddScoreInGame(float _score)
    {
        // không tăng score nếu k ở trạng thái playing
        if (GamePlayController.State != GAMEPLAY_STATE.PLAYING) return;
        scoreCurrent += _score;
    }

    // airport mode using
    public static AIRPORT_CONTEXT AirportModeUsing
    {
        get
        {
            switch (modeCurrent)
            {
                case MODE.CASUAL: return AIRPORT_CONTEXT.PVPCalsual;
                case MODE.ONE_VS_ONE: return AIRPORT_CONTEXT.PvpRank;
                default: return AIRPORT_CONTEXT.PVPCalsual;
            }
        }
    }
    // Hàm tính toán rơi item khi giết chết quái trong endless
    public static EndlessSO.ItemLootTier DropItemInGame(ENEMY_RANK _eRank)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        // 
        float _tierFactor = dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData.PieceDropRate
            .EnemiesBase.FirstOrDefault(x => x.Rank == _eRank).TierFactor;
        var _tier = RandomLootTier(Endless.TotalRarity, _tierFactor);
        var _item = _tier.RandomItemLoot();
        DPDebug.Log($"PVP rơi item rank[{_eRank.ToString()}]--Tier[{_tier.Tier}]--item[{_item.RewardType}]");
        return _item;
    }

    static List<EndlessSO.ItemLootTier> items = new List<EndlessSO.ItemLootTier>();
    static float[] itemProbs;

    static EndlessSO.ItemLootTier RandomItemLoot(this EndlessSO.LootTierRate _tier)
    {
        items = _tier.Items;
        if (items.Count <= 0 || items == null) return null;
        //
        if (items.Count == 1) return items[0];
        itemProbs = new float[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            itemProbs[i] = items[i].Rate;
        }

        var _id = itemProbs.Choose();
        return items[_id];
    }

    static List<EndlessSO.LootTierRate> lootRate = new List<EndlessSO.LootTierRate>();

    static EndlessSO.LootTierRate RandomLootTier(float _rateBonus, float _factor)
    {
        if (dataInit == null) PVPSO.GetData();
        //
        float[] _tierProbs = new float[2];
        lootRate = new List<EndlessSO.LootTierRate>(dataInit.Modes.FirstOrDefault(x => x.Mode == modeCurrent).LevelData
            .LootRate);
        for (int i = 0; i < lootRate.Count; i++)
        {
            _tierProbs[0] = lootRate[i].Rate;
            if (i == 0) // add bonus rate
                _tierProbs[0] = Mathf.Clamp((lootRate[i].Rate + (lootRate[i].Rate * _rateBonus / 100f)) * _factor, 0,
                    100);
            _tierProbs[1] = 100 - _tierProbs[0];
            DPDebug.Log($"Rate tier[{lootRate[i].Tier}]--[{_tierProbs[0]}/{_tierProbs[1]}]");
            if (DPUtils.Choose(_tierProbs) != 0) continue;
            return lootRate[i];
        }

        return null;
    }

    #endregion

    #region [PRIZES]

    // hàm kiểm tra xem đã nhận hết tất cả các mốc quà chưa
    public static bool IsClaimAllPrizes
    {
        get
        {
            foreach (var _item in data.Prizes)
            {
                if (!_item.IsClaimed) return false;
            }
            return true;
        }
    }
    // hàm reset lại các mốc phần thưởng
    public static void ResetAllPrizes()
    {
        DPDebug.Log("Reset all prizes PVP");
        data.Point = 0;// reset pvp point
        data.Prizes.Clear();// reset mốc quà
        data.Prizes = InitPrizes();
        SaveData();
    }
    // hàm khởi tạo các mốc phần thưởng
    static List<PrizesProcess> InitPrizes()
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        List<PrizesProcess> _rs = new List<PrizesProcess>();
        foreach (var _item in dataInit.PrizesRequireDatas)
        {
            PrizesProcess _prizes = new PrizesProcess
            {
                Index = _item.Index,
                PointRequire = _item.PointRequire,
                IsClaimed = false
            };
            _rs.Add(_prizes);
        }

        return _rs;
    }

    // Prizes
    static List<PrizesProcess> prizes;

    public static List<PrizesProcess> GetAllPrizes()
    {
        if (data.Prizes == null || data.Prizes.Count <= 0)
        {
            prizes = new List<PrizesProcess>();
            prizes = InitPrizes();
            data.Prizes = prizes;
            SaveData();
            return prizes;
        }
        else
        {
            return data.Prizes;
        }
    }

    // hàm kiểm tra xem có mốc quà nào có thể được nhận
    public static bool IsPrizesClaim
    {
        get
        {
            foreach (var _item in data.Prizes)
            {
                if (_item.IsClaimed) continue;
                // điểm mốc này đã đạt 
                if (PVPPoint >= _item.PointRequire) return true;
            }

            return false;
        }
    }

    // lấy quà theo mốc
    public static RewardCount GetPrizesReward(int _index)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        var _rw = dataInit.PrizesRequireDatas.FirstOrDefault(x => x.Index == _index).Reward;
        return _rw;
    }

    // nhận mốc quà đã đạt
    public static void ClaimPrizes(float _pointRequire)
    {
        var _process = data.Prizes.FirstOrDefault(x => x.PointRequire == _pointRequire);
        if (_process == null) return;
        //
        _process.IsClaimed = true;
        SaveData();
    }

    #endregion

    #region [BOT]

    // hàm tìm ra 1 con bot theo mode
    public static PlayerProcess FindBot(MODE _mode)
    {
        switch (_mode)
        {
            case MODE.CASUAL:
                if (data.Casual.Rank == RANK.NONE) // khởi đầu sẽ từ rank đồng 3
                    return FindCasualBotWithRank(RANK.BRONZE_3);
                else
                {
                    // sau khi có rank thì sẽ tìm con bot rank của mình +1
                    //var _rank = NextRank(data.Casual.Rank);
                    var _rank = RankHelperPVP.PromoteRank(data.Casual.Rank, 1);
                    DPDebug.Log($"Find bot rank current[{data.Casual.Rank}]--->next rank[{_rank}]");
                    return FindCasualBotWithRank(_rank);
                }
            default: return null;
        }
    }

    static float range, minScores, maxScores;
    static FIGHTER_TYPE GetBotFighterID(FIGHTER_TYPE _fighterIDInput)
    {
        var _allFighters = Fighters.GetReleasedFighters();
        var _fighterRank = Fighters.GetAllFighters().FirstOrDefault(x => x.IDFighter == _fighterIDInput).Rank;
        if (_fighterRank == PIECES_RANK.NONE) return FIGHTER_TYPE.FIGHTER_1;// phòng lỗi nếu không tìm được fighter thì lấy fighter 1
                                                                            // tìm tất cả fighter có rank = rank của fighter input
        var _fighters = _allFighters.Where(x => x.Rank == _fighterRank).ToList();
        var _fighterID = _fighters.RandomElement().IDFighter;
        if (_fighterID == FIGHTER_TYPE.NONE) return FIGHTER_TYPE.FIGHTER_1;// phòng lỗi nếu không tìm được fighter thì lấy fighter 1
        return _fighterID;
    }
    static PlayerProcess FindCasualBotWithRank(RANK _rank)
    {
        if (dataInit == null) dataInit = PVPSO.GetData();
        //
        var _data = dataInit.Modes.FirstOrDefault(x => x.Mode == MODE.CASUAL).Bots.FirstOrDefault(x => x.Rank == _rank);
        if (_data == null) return null;
        // random avatar
        var _avatarID = Avatar.GetAllAvatar().RandomElement().AvatarID;
        // random quân hàm = quân hàm của người chơi -1
        var _myMilitaryRank = MilitaryRank.GetProcess().RankHighest;
        var _militaryRank = MILITARY_RANK.LEVEL_1;
        // random country
        var _country = Countries.Random();
        // nếu quân hàm lớn hơn LEVEL_1 thì mới trừ 1
        if (_myMilitaryRank != MILITARY_RANK.NONE && _myMilitaryRank != MILITARY_RANK.LEVEL_1)
            _militaryRank = _myMilitaryRank.Decrement();
        // random tournament rank
        var _tournamentRank = DPUtils.RandomEnumExclude(TOURNAMENT_RANK_TIER.NONE);
        var _tournamentDivision = DPUtils.RandomEnumExclude(TournamentDivision.None);
        // random CP = CP của người chơi +-10%
        var _opponentCP = data.Casual.Airport.CP * (1 + UnityEngine.Random.Range(-0.1f, 0.1f));
        range = (_data.HighScores * (_data.Range / 100f));
        minScores = _data.HighScores - range;
        maxScores = _data.HighScores + range;
        var _scoreCurrent = Mathf.Round(Random.Range(minScores, maxScores));// tìm điểm trong ván đấu của bot
        // random fighter ID
        RankProcess _rankProcess = new RankProcess
        {
            Rank = _rank,
            Airport = new AirportProcess
            {
                Context = AIRPORT_CONTEXT.PVPCalsual,
                IsUnlocked = true,
                CP = _opponentCP,
                IDFighter = GetBotFighterID(data.Casual.Airport.IDFighter),
            },
            HighScores = _scoreCurrent,
            // bot casual không có lịch sử trận nên AVGSCORE = chính điểm random theo rank
            AvgScore = _scoreCurrent,
        };
        //
        PlayerProcess _bot = new PlayerProcess
        {
            Name = DPUtils.RandomName(),
            AvatarID = _avatarID,
            Country = _country,
            MilitaryRankHighest = _militaryRank,
            TournamentRankHighest = _tournamentRank,
            TournamentDivisionHighest = _tournamentDivision,
            RankProcess = _rankProcess,
            ScoreCurrent = _scoreCurrent,
        };
        return _bot;
    }

    #endregion

    #region [HACK]

    public static void HackAddPoint(float _point)
    {
        data.Point += _point;
        SaveData();
    }
    public static void HackAddPointRanked(float _point)
    {
        data.RankedPointProgress += _point;
        SaveData();
    }
    public static void HackChangeProcessRank(MODE _mode, RankProcess _newProcess)
    {
        switch (_mode)
        {
            case MODE.CASUAL: data.Casual = _newProcess; break;
            case MODE.ONE_VS_ONE: data.OneVsOne = _newProcess; break;
        }
        SaveData();
    }

    /// <summary>
    /// Cộng trực tiếp quà cuối mùa theo rank chỉ định cho tester. Không gọi EndSeason,
    /// không reset rank và không thay đổi trạng thái claim/event thật.
    /// </summary>
    public static bool HackClaimSeasonRankReward(MODE _mode, RANK _rank, out List<RewardCount> _rewards)
    {
        _rewards = GetRankRewards(_mode, _rank);
        if (_rewards == null || _rewards.Count <= 0)
        {
            DPDebug.LogError($"[PVP][HackSeasonReward] No reward config--mode[{_mode}]--rank[{_rank}]");
            return false;
        }

        Rewards.AddReward(_rewards, _whereLogReport: $"HACK_PVP_{GetClaimLogTag(_mode)}_SEASON_RANK_{_rank}");
        DPDebug.Log($"[PVP][HackSeasonReward] Claimed--mode[{_mode}]--rank[{_rank}]--rewardCount[{_rewards.Count}]");
        return true;
    }

    /// <summary>
    /// Cộng trực tiếp quà Top Challenger theo vị trí leaderboard cho tester.
    /// Chỉ cộng nhánh ChallengerRewards, không cộng lại quà rank Challenger/frame.
    /// </summary>
    public static bool HackClaimChallengerTopReward(MODE _mode, int _top, out List<RewardCount> _rewards)
    {
        _rewards = null;
        if (_top <= 0)
        {
            DPDebug.LogError($"[PVP][HackChallengerReward] Invalid top--top[{_top}]");
            return false;
        }

        var _config = GetChallengerReward(_mode, _top);
        if (_config == null || _config.Rewards == null || _config.Rewards.Count <= 0)
        {
            DPDebug.LogError($"[PVP][HackChallengerReward] No reward config--mode[{_mode}]--top[{_top}]");
            return false;
        }

        _rewards = new List<RewardCount>(_config.Rewards);
        Rewards.AddReward(_rewards, _whereLogReport: $"HACK_PVP_{GetClaimLogTag(_mode)}_CHALLENGER_TOP_{_top}");
        DPDebug.Log($"[PVP][HackChallengerReward] Claimed--mode[{_mode}]--top[{_top}]" +
            $"--range[{_config.MinRank}-{_config.MaxRank}]--rewardCount[{_rewards.Count}]");
        return true;
    }

    /// <summary>Cộng EXP trực tiếp cho PVP Battle Pass, không bắn analytics của luồng thật.</summary>
    public static bool HackAddSeasonPassExp(int _amount, out int _levelUp)
    {
        _levelUp = 0;
        if (data == null || _amount <= 0)
        {
            DPDebug.LogError($"[PVP][HackBPExp] Invalid process or amount--amount[{_amount}]");
            return false;
        }

        var _seasonData = SeasonData;
        var _seasonProcess = SeasonProcess;
        if (_seasonData?.Pass == null || !_seasonData.Pass.HasData || _seasonProcess?.Pass == null)
        {
            DPDebug.LogError("[PVP][HackBPExp] Battle Pass config/process is null or empty.");
            return false;
        }

        int _levelBefore = _seasonProcess.Pass.Level;
        int _expBefore = _seasonProcess.Pass.Exp;
        int _safeAmount = (int)System.Math.Min(_amount, (long)int.MaxValue - _expBefore);
        if (_safeAmount <= 0) return false;

        var _pass = BattlePass.AddExp(_seasonData.Pass, _seasonProcess.Pass, _safeAmount, out _levelUp);
        if (_pass == null) return false;

        _seasonProcess.Pass = _pass;
        bool _isChanged = _pass.Level != _levelBefore || _pass.Exp != _expBefore;
        if (_isChanged) SaveData();

        DPDebug.Log($"[PVP][HackBPExp] Add EXP--amount[{_safeAmount}]--level[{_levelBefore}->{_pass.Level}]--exp[{_expBefore}->{_pass.Exp}]--levelUp[{_levelUp}]--changed[{_isChanged}]");
        return _isChanged;
    }

    /// <summary>
    /// Lấy danh sách nhiệm vụ PVP Battle Pass đang active hôm nay cho màn hack.
    /// </summary>
    public static List<(PVPSeasonMissionConfig config, BattlePassMissionProgress progress)> HackGetSeasonMissions()
    {
        var _empty = new List<(PVPSeasonMissionConfig config, BattlePassMissionProgress progress)>();
        if (data == null)
        {
            DPDebug.LogError("[PVP][HackMission] PVP process is null.");
            return _empty;
        }

        var _seasonData = GetSeasonData(MODE.ONE_VS_ONE);
        if (_seasonData == null || _seasonData.Missions == null || !_seasonData.Missions.HasData)
        {
            DPDebug.LogError("[PVP][HackMission] PVP Battle Pass mission config is null or empty.");
            return _empty;
        }

        SetSeasonProcess(PVPSeason.CheckDaily(_seasonData, SeasonProcess, SeasonDayID, out bool _isNewDay), MODE.ONE_VS_ONE);
        if (_isNewDay) SaveData();
        return PVPSeason.GetTodayMissions(_seasonData, SeasonProcess);
    }

    /// <summary>
    /// Tăng tiến trình cho đúng một nhiệm vụ PVP Battle Pass đang active hôm nay.
    /// Không vượt Require và không tự claim để tester vẫn kiểm tra được luồng thật.
    /// </summary>
    public static bool HackAddSeasonMissionProgress(int _missionID, int _amount = 1)
    {
        if (_amount <= 0)
        {
            DPDebug.LogError($"[PVP][HackMission] Progress amount must be greater than zero--amount[{_amount}]");
            return false;
        }

        var _missions = HackGetSeasonMissions();
        var _seasonProcess = SeasonProcess;
        for (int i = 0; i < _missions.Count; i++)
        {
            var _config = _missions[i].config;
            var _progress = _missions[i].progress;
            if (_config == null || _progress == null || _config.ID != _missionID) continue;

            if (_progress.IsClaimed || _progress.Value >= _config.Require)
            {
                DPDebug.Log($"[PVP][HackMission] Mission cannot advance--id[{_missionID}]--value[{_progress.Value}/{_config.Require}]--claimed[{_progress.IsClaimed}]");
                return false;
            }

            if (_config.Type == TYPE_MISSION.PVP_Ranked_TotalScoreDay)
            {
                // Mission tổng điểm dùng TotalScoreToday làm giá trị thực; progress chỉ là bản
                // clamp theo Require để phục vụ trạng thái hoàn thành trên UI.
                long _total = (long)_seasonProcess.TotalScoreToday + _amount;
                _seasonProcess.TotalScoreToday = (int)System.Math.Min(_total, int.MaxValue);
                _progress.Value = Mathf.Min(_seasonProcess.TotalScoreToday, _config.Require);
            }
            else
            {
                _progress.Value = Mathf.Min(_progress.Value + _amount, _config.Require);
            }

            SaveData();
            DPDebug.Log($"[PVP][HackMission] Add progress successful--id[{_missionID}]--type[{_config.Type}]--amount[{_amount}]--value[{_progress.Value}/{_config.Require}]--day[{SeasonDayID}]");
            return true;
        }

        DPDebug.LogError($"[PVP][HackMission] Mission is not active today--id[{_missionID}]");
        return false;
    }

    #endregion

    public static RANK ConvertToRank(string _rankValue)
    {
        if (int.TryParse(_rankValue, out int _value) && System.Enum.IsDefined(typeof(RANK), _value))
        {
            return (RANK)_value;
        }
        return RANK.NONE;
    }

    //==============================================================================================================================================
    [System.Serializable]
    public class Process
    {
        public float Point; // điểm pvp hiện tại đang có
        public List<PrizesProcess> Prizes = new List<PrizesProcess>(); // tiến trình của tất cả phần thưởng mùa này
        public BoosterProcess Booster; // tiến trình của booster hiện tại
        public int WinCount;// tổng số trận win của tất cả các chế độ chơi trong PVP, và sẽ không bị reset theo mùa
        public float RankedPointProgress;// điểm để tính mốc tăng chỉ số cho người chơi, chỉ tăng trong chế độ XẾP HẠNG
        public float RankedPointProgressOld;// điểm cũ để tính mốc tăng chỉ số cho người chơi, chỉ tăng trong chế độ XẾP HẠNG
        public int RankedPointLevel;// cấp độ đã đạt được của mùa giải lần này cho chế độ xếp hạng
        public string RankedPointSeasonID;// season đã reset tiến trình Victory Bonus gần nhất
        public bool HasPendingMatch;// true từ lúc bắt đầu trận đến khi có kết quả/forfeit
        public MODE PendingMatchMode = MODE.NONE;// mode của pending match, dùng để xử thua sau force kill
        public string PendingMatchSeasonDayID = string.Empty;// ngày UTC bắt đầu pending match, dùng xử trận xuyên ngày
        //
        public RankProcess Casual; // tiến trình của chế độ casual
        public RankProcess OneVsOne; // tiến trình của chế độ 1v1
        // Season nằm TRONG RankProcess của từng mode (cạnh SeasonVersion) -> thêm chế độ PVP mới cũng
        // không phải đổi shape của save.
    }
    [System.Serializable]
    public class BoosterProcess
    {
        public bool IsPremium; // gói vip sẽ k phải xem QC và không giới hạn số lượt trong ngày(10 lượt). và gói này có thời gian sử dụng 7 ngày
        //public int DayOfYear; // ngày trong năm của booster
        public string LastOpen;
        public string PremiumStart; // thời gian gói premium bắt đầu
        public string PremiumFinish; // thời gian gói premium kết thúc
        public List<BoosterItemProcess> Boosters = new List<BoosterItemProcess>(); // tiến trình tất cả booster
    }
    [System.Serializable]
    public class BoosterItemProcess
    {
        public int Index;
        public bool IsFree;// chỉ được sử dụng 1 lần duy nhất trong ngày, qua ngày mới được reset
        public bool IsActive; // đang được kích hoạt
        public double Duration; // thời gian đang đếm ngược
        public int CountWatchAds; // số lượt xem quảng cáo trong ngày
        public bool IsOn; // chỉ sử dụng cho id:2 là booster giãn cách giữa các wave quái
    }
    [System.Serializable]
    public class WaveProcess
    {
        public BigDouble HP;
        public BigDouble DMG;
        public BigDouble BodyDMG;
    }
    [System.Serializable]
    public class PlayerProcess // tiến trình của 1 người chơi trong 1 ván đấu
    {
        public string Name;
        public string AvatarID;
        public string FrameAvatarID;
        public string Country;
        public MILITARY_RANK MilitaryRankHighest; // cấp hàm cao nhất user đạt được
        public TOURNAMENT_RANK_TIER TournamentRankHighest; // cấp Tournament cao nhất user đạt được
        public TournamentDivision TournamentDivisionHighest; // cấp Division cao nhất user đạt được
        public RankProcess RankProcess;
        public float ScoreCurrent; // điểm hiện tại trong ván đấu
        public int EquippedAwardIndex = -1;
        public int LevelAwardEquipped = 0;
        //
        public PlayerProcess Clone()
        {
            return new PlayerProcess
            {
                Name = this.Name,
                AvatarID = this.AvatarID,
                FrameAvatarID = this.FrameAvatarID,
                Country = this.Country,
                MilitaryRankHighest = this.MilitaryRankHighest,
                TournamentRankHighest = this.TournamentRankHighest,
                TournamentDivisionHighest = this.TournamentDivisionHighest,
                RankProcess = this.RankProcess != null ? this.RankProcess.Clone() : null,
                ScoreCurrent = this.ScoreCurrent,
                EquippedAwardIndex = this.EquippedAwardIndex,
                LevelAwardEquipped = this.LevelAwardEquipped,
            };
        }
    }
    [System.Serializable]
    public class PrizesProcess
    {
        public int Index;
        public float PointRequire; // số điểm của mốc quà này
        public bool IsClaimed;
    }
    [System.Serializable]
    public class RankProcess
    {
        public AirportProcess Airport; // sân bay riêng chế độ PVP này
        public RANK Rank = RANK.NONE; // cấp bậc, mỗi cấp bậc dưới thách đấu thì đều có 3 đoàn
        public float HighScores; // điểm cao nhất hiện tại user đạt được trong 1 ván thi đấu
        public float AvgScore; // AVGSCORE = HighScores của preseason x hệ số rank cuối preseason. Chốt 1 lần khi vào mùa mới
        public List<float> RecentMatchScores = new List<float>(); // điểm của 10 trận hợp lệ gần nhất, trận mới nhất nằm cuối list
        // Điểm mang ra thi đấu KHÔNG lưu: là giá trị dẫn xuất từ AvgScore + RecentMatchScores,
        // lấy qua PVP.CalculateScoreForMatch(mode, process) để không bao giờ bị lệch với 2 field trên.
        public int ChallengerPoint; // điểm của rank thách đấu, điểm mặc định là 0
        public float AVG_KillTime; // thời gian trung bình để tiêu diệt 1 con quái
        public float AVG_WaveScore; // điểm trung bình đạt được sau mỗi wave
        public int WinStreak; // chuỗi thắng hiện tại
        public int LoseStreak; // chuỗi thua hiện tại
        public List<MatchProgress> MatchProgresses = new List<MatchProgress>();// tiến trình số trận thắng thua hiện tại của rank này
        public string EventID;// id của event hiện tại trên server
        // LEGACY (<15.5), cố ý ngừng deserialize/sử dụng để giá trị cũ (mọi user = 2) không gây reset preseason.
        // public string SeasonID;
        public int SeasonVersion = 0;// 0 = preseason, 1 = S1...; field mới nên save cũ tự nhận giá trị mặc định 0
        // battle pass + nhiệm vụ hằng ngày của mùa giải, RIÊNG cho từng chế độ PVP
        public PVPSeasonProcess Season = new PVPSeasonProcess();
        public bool IsEventPlaying;// event đang hoạt động
        public string FrameAvatarID = string.Empty;// khung avatar hiện tại được sử dụng

        public RankProcess() { }
        public override string ToString()
        {
            return $"[RankProcess: Airport = {Airport}, RANK = {Rank}]";
        }
        public RankProcess Clone()
        {
            var _clone = new RankProcess
            {
                Airport = this.Airport != null ? this.Airport.Clone() : null,
                Rank = this.Rank,
                HighScores = this.HighScores,
                AvgScore = this.AvgScore,
                RecentMatchScores = this.RecentMatchScores != null
                    ? new List<float>(this.RecentMatchScores)
                    : new List<float>(),
                ChallengerPoint = this.ChallengerPoint,
                AVG_KillTime = this.AVG_KillTime,
                AVG_WaveScore = this.AVG_WaveScore,
                WinStreak = this.WinStreak,
                LoseStreak = this.LoseStreak,
                MatchProgresses = new List<MatchProgress>(this.MatchProgresses.Count),
                EventID = this.EventID,
                SeasonVersion = this.SeasonVersion,
                IsEventPlaying = this.IsEventPlaying,
                FrameAvatarID = this.FrameAvatarID,
                // GIỮ NGUYÊN REFERENCE, không deep-clone: clone dùng cho snapshot đối thủ (không ai
                // sửa season trong đó), nhưng hack panel clone rồi gán ngược lại qua
                // HackChangeProcessRank -> mất reference là mất cả battle pass + gói premium đã mua.
                Season = this.Season,
            };
            // Clone danh sách MatchProgress
            foreach (var _match in this.MatchProgresses)
                _clone.MatchProgresses.Add(_match != null ? _match.Clone() : null);

            return _clone;
        }
    }
    [System.Serializable]
    public class MatchProgress
    {
        public int Index; // trận đấu thứ mấy
        public MATCH_RESULT Result; // kết quả trận đấu
        //
        public MatchProgress Clone()
        {
            return new MatchProgress
            {
                Index = this.Index,
                Result = this.Result
            };
        }
    }

    public enum MODE
    {
        CASUAL, // chỉ đánh với bot
        ONE_VS_ONE, // 1v1
                    //
        NONE = 99,
    }

    public enum RANK
    {
        NONE = 0,

        // Rank đồng
        BRONZE_1 = 1001,
        BRONZE_2 = 1002,
        BRONZE_3 = 1003,

        // Rank bạc
        SILVER_1 = 2001,
        SILVER_2 = 2002,
        SILVER_3 = 2003,

        // Rank vàng
        GOLD_1 = 3001,
        GOLD_2 = 3002,
        GOLD_3 = 3003,

        // Rank bạch kim
        PLATIUM_1 = 4001,
        PLATIUM_2 = 4002,
        PLATIUM_3 = 4003,

        // Rank kim cương
        DIAMOND_1 = 5001,
        DIAMOND_2 = 5002,
        DIAMOND_3 = 5003,

        // Rank thách đấu
        CHALLENGER = 6000,
    }
}
public enum MATCH_RESULT
{
    NONE,
    WIN,
    LOSE,
}
public enum AIRPORT_CONTEXT
{
    NORMAL,
    PVPCalsual,
    PvpRank
}
