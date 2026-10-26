using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Games;
using DP.Utilities;
using System.Security.Cryptography;
using System.Text;
using static Games.Events;
using System.Runtime.InteropServices;

[Serializable]
public class PayloadData
{
    public string data;
    public string checksum;
}
[Serializable]
public class TokenData
{
    public string message;
}
[Serializable]
public class GeoData
{
    public string ip;
    public string city;
    public string region;
    public string country;
    //public string status;
    //public string countryCode;
    //public string query;
}
public static class DHS
{
    static string https
    {
        get
        {
            // return "https://1941-api.onegame.mobi/test";
            if (Hack.UseServerTest) return "https://192.168.1.246:8386";
            return "https://1941-api.onegame.mobi";
        }
    }
    static int TimeOut = 25;
    static string urlUpdateInfoUser = $"{https}/api/users";
    //
    static string urlFindOpponentPVP = "{0}/api/users/events/pvp/opponent/{1}?eventId={2}&seasonId={3}";
    //
    static string urlUser = $"{https}/api/users/";
    static string urlGetLeaderboardPVP1V1 = $"{https}/api/users/leaderboard/global";
    static string urlGetLeaderboard = $"{https}/api/users/leaderboard/global";
    static string urlGetLeaderboardEndless = $"{https}/api/users/leaderboard/endless";
    static string urlGetLeaderboardCountry = $"{https}/api/users/leaderboard/country";
    static string urlGetLeaderboardEvent = $"{https}/api/users/leaderboard/event";
    static string urlGetUserWithIdSocial = $"{https}/api/users/social/";
    static string urlToken = $"{https}/generateToken";
    static string urlChatHistory = $"{https}/api/users/chatHistory/global";

    static string urlGiftCode = "https://remote.onegamestudio.net/api/giftcode/redeem.jsp";
    static string urlGetListMailbox = $"{urlUser}mailbox/list"; // get list mailbox /:userId/:messageType
    static string urlMarkAsRead = $"{urlUser}mailbox/markAsRead"; // get list mailbox /:messageId/:userId
    static string urlClaimReward = $"{urlUser}mailbox/claim"; // claim reward mailbox /:messageId/:userId

    static string urlDeleteAllMail = $"{urlUser}mailbox/deleteAll/"; // claim reward mailbox /:messageId/:userId
    static string urlClaimAllMail = $"{urlUser}mailbox/claimAll/"; // claim reward mailbox /:messageId/:userId

    static string urlCheck = $"{urlUser}events/check/";
    static string urlAllEvent = $"{urlUser}events/all/";
    static string urlConnectEventEndless = $"{urlUser}events/connect/";
    static string urlClaimEventEndless = $"{urlUser}events/claim/";
    static string urlEventActiveTime = $"{urlUser}events/active-time?eventName=";
    static string urlEventBattleTeamActive = $"{https}/api/team-events/active-season";
    //
    public delegate void WebCallback(UnityWebRequest www);
    public static event Action<bool> OnConnected;
    public static string token;
    static bool IsTokenGenarated => !string.IsNullOrEmpty(token);
    //=============================================================================================================================
    public static void OnConnect(bool _isConnect)
    {
        OnConnected?.Invoke(_isConnect);
    }
    static bool IsCanPutData
    {
        get
        {
            if (Player.CampaignProcess == null)
            {
                DPDebug.Log("Can't push data level current null!");
                return false;
            }
            if (string.IsNullOrEmpty(Player.IdSocial))
            {
                var _normalMode = Player.CampaignProcess.Modes?.Find(x => x != null && x.Mode == CAMPAIGN_MODE.NORMAL);
                var _lvMax = _normalMode?.LevelProcess?.LevelMax ?? Player.LevelCurrent?.LevelMax ?? 0;
                if (_lvMax < 1)
                    return false;
            }
            return true;
        }
    }
    //=============================================================================================================================
    #region [PVP]

    public static void GetOpponentPVP1V1(MonoBehaviour _mono, Action<string> _callback = null)
    {
        _mono.StartCoroutine(IEGetOpponentPVP1V1(_callback));
    }
    static IEnumerator IEGetOpponentPVP1V1(Action<string> _callback = null)
    {
        var _rankProcess = PVP.GetRankProcess(PVP.MODE.ONE_VS_ONE);
        string _eventId = _rankProcess.EventID;
        int _seasonVersion = _rankProcess.SeasonVersion;
        var _strUrl = string.Format(urlFindOpponentPVP, https, Player.Id, _eventId, _seasonVersion);
        DPDebug.Log($"Find opponent PVP Url[{_strUrl}]");
        var _uri = new Uri(_strUrl);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get opponent PVP successful json: \n{_json}");
                    _callback.Invoke(_json);
                }
                else
                {
                    _callback.Invoke(string.Empty);
                }
                break;
            default:
                DPDebug.LogWarning($"Request delete user data with error! Request state: {_request.error}");
                _callback.Invoke(string.Empty);
                break;
        }
    }
    public static void GetAllPVP(MonoBehaviour _mono, Action<bool, string> _callback)
    {
        _mono.StartCoroutine(IEGetAllPVP(_callback));
    }
    private static IEnumerator IEGetAllPVP(Action<bool, string> _callback)
    {
        // var _url = $"{urlAllEvent}{Player.Id}?eventName=ONE_VS_ONE";
        // PVP cần leaderboard của chính event REDEMPTION/ENDED để chốt quà top Challenger.
        // Không thể gọi ConnectEvent sau khi claim vì endpoint đó trả season ACTIVE hiện tại.
        var _url = $"{urlAllEvent}{Player.Id}?isIncludeLB=true";
        DPDebug.Log($"Check url get event PVP: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    // dump JSON đầy đủ ra file + clipboard, kèm bảng tóm tắt các event (tránh Console cắt chuỗi)
                    // PVPEventDebug.Report(_json);
                    DPDebug.Log($"Get event PVP successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Get event PVP with an error: {_request.error}";
                    DPDebug.LogError(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Get event PVP with an error: {_request.error}";
                DPDebug.LogError(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    #endregion

    public static void GetChatHistoryGlobal(MonoBehaviour _mono, Action<string> _callback = null)
    {
        _mono.StartCoroutine(IEGetChatHistoryGlobal(_callback));
    }
    private static IEnumerator IEGetChatHistoryGlobal(Action<string> _callback = null)
    {
        var _uri = new Uri(urlChatHistory);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get chat history successful json: \n{_json}");
                    _callback.Invoke(_json);
                }
                else
                {
                    _callback.Invoke(string.Empty);
                }
                break;
            default:
                DPDebug.LogWarning($"Request delete user data with error! Request state: {_request.error}");
                _callback.Invoke(string.Empty);
                break;
        }
    }
    public static void GenerateToken(MonoBehaviour _mono, System.Action _callback)
    {
        DPDebug.Log($"====> USE SV TEST: {Hack.UseServerTest} | URL TOKEN: {urlToken}");
        _mono.StartCoroutine(IEGenerateToken(_callback));
    }
    private static IEnumerator IEGenerateToken(System.Action _callback)
    {
        var _uri = new Uri(urlToken);
        DPDebug.Log($"====> URL TOKEN: {_uri}");
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        // 3. Send
        yield return _request.SendWebRequest();

        // 4. Wait for completion
        yield return _request;

        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                DPDebug.Log($"Upload finished succesfully: json[{_json}]");
                TokenData _data = JsonUtility.FromJson<TokenData>(_json);
                token = _data.message;
                DPDebug.Log($"Upload finished succesfully: token[{token}]");
                break;
            default:
                DPDebug.LogWarning($"Request finished with error! Request state: {_request.error}");
                break;
        }
        _callback?.Invoke();
    }
    private static string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
    /// <summary>
    /// Post THƯỜNG — TÔN TRỌNG cờ chống hack. User bị đánh dấu hack (IsHackDontSendToServer=true)
    /// thì KHÔNG đẩy save lên server. Mọi luồng game bình thường nên dùng hàm này.
    /// </summary>
    public static void PostUser(MonoBehaviour _mono, System.Action<bool> _callback = null)
    {
        if (SystemVariables.IsHackDontSendToServer)
        {
            DPDebug.Log("Hack don't send data Player to server! (PostUser bị chặn bởi IsHackDontSendToServer)");
            // BẮT BUỘC gọi callback: nhiều nơi (nút Continue PVP, tournament...) chờ callback để tiếp tục UI,
            // không gọi thì user bị đánh dấu sẽ kẹt màn.
            if (_callback != null) _callback.Invoke(false);
            return;
        }
        ForcePostUser(_mono, _callback);
    }

    /// <summary>
    /// Post BẮT BUỘC — KHÔNG xét cờ SystemVariables.IsHackDontSendToServer.
    /// Dùng cho bản ghi anti-hack (AntiCheat.Flag) và hack menu (UIHack): phải lên server DÙ cờ đã bật.
    /// VẪN giữ guard IsCanPutData để không đẩy save rỗng đè lên dữ liệu server.
    /// </summary>
    public static void ForcePostUser(MonoBehaviour _mono, System.Action<bool> _callback = null)
    {
        DPDebug.Log($"====> can push data to server[{IsCanPutData}]");
        if (!IsCanPutData)
            return;
        Player.UpdateInforSendToServer();
        var _infoUser = Player.Process;
        string _jsonData = JsonUtility.ToJson(_infoUser);
        DPDebug.Log($"Post user json player:\n{_jsonData}");
        //
        DPUtils.WriteText("User_Post", _jsonData);
        string _checksum = ComputeSha256Hash(_jsonData);
        // Tạo payload
        var _payload = new PayloadData
        {
            data = _jsonData,
            checksum = _checksum,
        };
        _mono.StartCoroutine(IEPostUser(_payload, _callback));
    }
    private static IEnumerator IEPostUser(PayloadData _info, System.Action<bool> _callback = null)
    {
        string _json = JsonUtility.ToJson(_info);
        DPDebug.Log($"=====> data post:\n{_json}");
        byte[] _byteData = Encoding.ASCII.GetBytes(_json.ToCharArray());
        using UnityWebRequest _www = UnityWebRequest.Put(urlUpdateInfoUser, _byteData);
        DPDebug.Log($"=====> URL Post: {urlUpdateInfoUser}");
        _www.certificateHandler = new BypassCertificate();
        _www.SetRequestHeader("Content-Type", "application/json");
        _www.SetRequestHeader("Accept", "text/json");
        _www.SetRequestHeader("Authorization", "Bearer " + token);
        DPDebug.Log($"=====> Token Post: {token}");
        _www.timeout = 5;

        yield return _www.SendWebRequest();

        switch (_www.result)
        {
            case UnityWebRequest.Result.Success:
                Debug.Log("==========> " + _www.downloadHandler.text);
                Debug.Log("======> POST USER - SERVER: " + _www.downloadHandler.text);
                if (_callback != null) _callback.Invoke(true);
                break;
            default:
                Debug.Log($"======> POST USER - SERVER: ERROR\n{_www.error}\n{_www.downloadHandler.text}");
                if (_callback != null) _callback.Invoke(false);
                break;
        }
    }
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Bỏ qua tất cả các lỗi SSL
        }
    }
    //=============================================================================================================================
    public static void GetUserFromServer(MonoBehaviour _mono, string _id, Action<bool, string> _callback = null, bool _isFromLB = false, bool _isWriteText = false)
    {
        _mono.StartCoroutine(IEGetUser(_id, _callback, _isFromLB, _isWriteText));
    }
    private static IEnumerator IEGetUser(string _id, Action<bool, string> _callback = null, bool _isFromLB = false, bool _isWriteText = false)
    {
        string _url = $"{urlUser}{_id}";
        if (_isFromLB)
            _url += "?OnLB=true";
        DPDebug.Log("Get User URL: " + _url);
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                if (_request.responseCode < 203)
                {
                    var _json = _request.downloadHandler.text;
                    DPDebug.Log($"Get user data successful json: \n{_json}");
                    if (_isWriteText)// ghi file json user ra ngoài để xem
                        DPUtils.WriteText("User_Get", _json);
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Get user data with an error: {_request.error}";
                    DPDebug.Log(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request finished with an error: {_request.error}";
                DPDebug.Log(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetUserFromServerWithIdSocial(MonoBehaviour _mono, string _id, string _idSocial, Action<string, bool> _callback)
    {
        _mono.StartCoroutine(IEGetUserWithIdSocial(_id, _idSocial, _callback));
    }
    private static IEnumerator IEGetUserWithIdSocial(string _id, string _idSocial, Action<string, bool> _callback)
    {
        string _url = $"{urlGetUserWithIdSocial}{_idSocial}";
        DPDebug.Log($"Request user IdSocial with URL[{_url}]");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                DPDebug.Log($"Get user data IdSocial--code[{_request.responseCode}]");
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get user data IdSocial successful json: \n{_json}");
                    DPUtils.WriteText("User_GetIdSocial", _json);
                    _callback.Invoke(_json, true);
                }
                else
                {
                    DPDebug.Log($"Get user data IdSocial successful json null:\n{_json}");
                    _callback.Invoke(string.Empty, true);
                }
                break;
            default:
                DPDebug.LogWarning($"Request Get user data IdSocial with error! Request state: {_request.error}");
                if (_request.responseCode == 400)
                {
                    _callback.Invoke(string.Empty, true);
                }
                else
                    _callback.Invoke(string.Empty, false);
                break;
        }
    }
    //==============================================================================================================================
    public static void GetLeaderboardPVP1V1(MonoBehaviour _mono, Action<bool, string> _callback)
    {
        DPDebug.Log($"=======> IS TOKEN GENARATED: {IsTokenGenarated}");
        if (IsTokenGenarated)
            _mono.StartCoroutine(IEGetLeaderboardPVP1V1(_callback));
        else
        {
            DPDebug.Log($"=====> VAY DAY KO");
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEGetLeaderboardPVP1V1(_callback));
                else // TH false
                    _callback?.Invoke(false, "Cannot Connect");
            });
        }
        _mono.StartCoroutine(IEGetLeaderboardPVP1V1(_callback));
    }
    private static IEnumerator IEGetLeaderboardPVP1V1(Action<bool, string> _callback)
    {
        var _uri = new Uri(urlGetLeaderboardPVP1V1);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get leaderboard PVP 1v1 data successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Get leaderboard PVP 1v1 data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Get leaderboard data PVP 1v1 with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetLeaderboardGlobal(MonoBehaviour _mono, bool _isGlocal, Action<bool, string> _callback)
    {
        DPDebug.Log($"=======> IS TOKEN GENARATED: {IsTokenGenarated}");
        if (IsTokenGenarated)
            _mono.StartCoroutine(IEGetLeaderboard(_isGlocal, _callback));
        else
        {
            DPDebug.Log($"=====> VAY DAY KO");
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEGetLeaderboard(_isGlocal, _callback));
                else // TH false
                    _callback?.Invoke(false, "Cannot Connect");
            });
        }
    }
    private static IEnumerator IEGetLeaderboard(bool _isGlocal, Action<bool, string> _callback)
    {
        var _uri = new Uri(urlGetLeaderboard);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get leaderboard data successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Get leaderboard data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Get leaderboard data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetLeaderboardEndless(MonoBehaviour _mono, Action<bool, string> _callback)
    {
        if (IsTokenGenarated)
            _mono.StartCoroutine(IEGetLeaderboardEndless(_callback));
        else
        {
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEGetLeaderboardEndless(_callback));
                else // TH false
                    _callback?.Invoke(false, "Cannot Connect");
            });
        }
        _mono.StartCoroutine(IEGetLeaderboardEndless(_callback));
    }
    private static IEnumerator IEGetLeaderboardEndless(Action<bool, string> _callback)
    {
        var _uri = new Uri(urlGetLeaderboardEndless);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get leaderboard endless data successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Get leaderboard endless data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Get leaderboard endless data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetLeaderboardCountry(MonoBehaviour _mono, Action<bool, string> _callback)
    {
        if (IsTokenGenarated)
            _mono.StartCoroutine(IEGetLeaderboardCountry(_callback));
        else
        {
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEGetLeaderboardCountry(_callback));
                else // TH false
                    _callback?.Invoke(false, "Cannot Connect");
            });
        }
        _mono.StartCoroutine(IEGetLeaderboardCountry(_callback));
    }
    private static IEnumerator IEGetLeaderboardCountry(Action<bool, string> _callback)
    {
        var _uri = new Uri(urlGetLeaderboardCountry);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get leaderboard country data successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Get leaderboard country data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Get leaderboard country data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    public static void GetLeaderboardEvent(MonoBehaviour _mono, Games.Events.EVENT_TYPE _type, Action<bool, string> _callback)
    {
        if (IsTokenGenarated)
            _mono.StartCoroutine(IEGetLeaderboardEvent(_type, _callback));
        else
        {
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEGetLeaderboardEvent(_type, _callback));
                else
                    _callback?.Invoke(false, "Cannot Connect");
            });
        }
    }

    private static IEnumerator IEGetLeaderboardEvent(Games.Events.EVENT_TYPE _type, Action<bool, string> _callback)
    {
        var _url = $"{urlGetLeaderboardEvent}?eventType={_type}";
        DPDebug.Log($"Get leaderboard event URL: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get leaderboard event successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Get leaderboard event error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Get leaderboard event error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    //==============================================================================================================================
    public static void DeleteUser(MonoBehaviour _mono, string _id, Action<bool> _callback)
    {
        _mono.StartCoroutine(IEDeleteUser(_id, _callback));
    }
    private static IEnumerator IEDeleteUser(string _id, Action<bool> _callback)
    {
        string _url = $"{urlUser}{_id}";
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Delete(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                DPDebug.Log($"Delete user data success--Code[{_request.responseCode}]");
                if (_request.responseCode < 404) _callback.Invoke(true);
                else _callback.Invoke(false);
                break;
            default:
                DPDebug.LogWarning($"Request delete user data with error! Request state: {_request.error}");
                _callback.Invoke(false);
                break;
        }
    }
    //===============================================================================================================================
    public static void GetCountry(MonoBehaviour _mono)
    {
        //Debug.Log($"Get country--[{Player.Country}]");
        if (!string.IsNullOrEmpty(Player.Country))
        {
            //Debug.Log($"Country not null or empty!");
            return;
        }
        //
        //Debug.Log($"Start get country");
        _mono.StartCoroutine(IEGetCountry());
    }
    private static IEnumerator IEGetCountry()
    {
        UnityWebRequest _request = UnityWebRequest.Get("https://ipinfo.io/json");
        _request.timeout = TimeOut;
        _request.chunkedTransfer = false;
        yield return _request.Send();
        //  textState.text = "Locating...";

        if (_request.isNetworkError)
        {
            Debug.Log("============> ERROR GET COUNTRY: " + _request.downloadHandler.text);
            // textState.text = "error : " + request.error;
        }
        else
        {
            Debug.Log($"Get country successful--[{_request.isDone}]");
            if (_request.isDone)
            {
                Debug.Log("============> geoDATA: " + _request.downloadHandler.text);
                var _geoData = JsonUtility.FromJson<GeoData>(_request.downloadHandler.text);
                Player.Country = _geoData.country;
            }
        }
    }
    //===============================================================================================================================
    public static void OnSendGiftCode(MonoBehaviour _mono, string _code, Action<bool, string> _callback)
    {
        _mono.StartCoroutine(IESendGiftCode(_code, _callback));
    }
    private static IEnumerator IESendGiftCode(string _code, Action<bool, string> _callback)
    {
        var _giftCode = new GiftCode(Player.GUID.ToString(), _code, NetMonobehavior.Instance.accessKey);
        var _dataSend = JsonUtility.ToJson(_giftCode);
        var _byteData = Encoding.ASCII.GetBytes(_dataSend.ToCharArray());

        DownloadHandlerBuffer downloadHandlerBuffer = new DownloadHandlerBuffer();
        //
        UploadHandlerRaw uploadHandlerRaw = new UploadHandlerRaw(_byteData);
        uploadHandlerRaw.contentType = "application/json";
        UnityWebRequest www = new UnityWebRequest(urlGiftCode, "POST", downloadHandlerBuffer, uploadHandlerRaw);
        www.SetRequestHeader("Accept", "text/json");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("access-key", NetMonobehavior.Instance.accessKey);
        www.timeout = 5;
        yield return www.SendWebRequest();
        switch (www.result)
        {
            case UnityWebRequest.Result.Success:
                Debug.Log("====> get giftcode success!");
                _callback?.Invoke(true, www.downloadHandler.text);
                break;
            default:
                _callback?.Invoke(false, www.downloadHandler.error);
                break;

        }
    }
    public static void OnMailBox(MonoBehaviour _mono, MailBoxType _type, string _param1, string _param2, Action<bool, string> _callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            GenerateToken(_mono, () =>
            {
                if (IsTokenGenarated)
                    _mono.StartCoroutine(IEOnMailbox(_type, _param1, _param2, _callback));
                else // TH false
                    _callback?.Invoke(false, "Cannot Connect");
            });
            return;
        }
        _mono.StartCoroutine(IEOnMailbox(_type, _param1, _param2, _callback));
    }
    private static IEnumerator IEOnMailbox(MailBoxType _type, string _param1, string param2, Action<bool, string> _callback)
    {
        /*
        //string _url = _type switch
        //{
        //    MailBoxType.GetAll => urlGetListMailbox + $"/{_param1}?section={param2}",
        //    MailBoxType.MarkRead => urlMarkAsRead + $"/{_param1}?userid={param2}",
        //    _ => urlClaimReward + $"/{_param1}?userid={param2}"
        //};
        //DPDebug.Log("_url mailbox: " + _url);

        ////var _request = new HTTPRequest(_url, _type == MailBoxType.GetAll ? HTTPMethods.Get : HTTPMethods.Post)
        ////{
        ////    Authenticator = new BearerTokenAuthenticator(token)
        ////};
        //var _request = new HTTPRequest(new Uri(_url), methodType: _type == MailBoxType.GetAll ? HTTPMethods.Get : HTTPMethods.Post);
        //_request.SetHeader("Authorization", $"Bearer {token}");
        //_request.Send();

        //yield return _request;
        //string _error;
        //switch (_request.State)
        //{
        //    case HTTPRequestStates.Finished:
        //        // 5. Process response
        //        if (_request.Response.IsSuccess)
        //        {
        //            if (_request.Response.StatusCode < 404)
        //            {
        //                var _json = _request.Response.DataAsText;
        //                DPDebug.Log($"Get mail successful json: \n{_json}");
        //                _callback?.Invoke(true, _json);
        //            }
        //            else
        //            {
        //                _error = $"Get mail with an error: {_request.Response.StatusCode}";
        //                DPDebug.LogWarning(_error);
        //                _callback?.Invoke(false, _error);
        //            }
        //        }
        //        else
        //        {
        //            // 6. Error handling
        //            _error = $"Get mail with an error: {_request.Response.StatusCode}-{_request.Response.Message}";
        //            DPDebug.LogWarning(_error);
        //            DPDebug.LogWarning($"Get mail text: \n{_request.Response.DataAsText}");
        //            _callback?.Invoke(false, _error);
        //        }

        //        break;
        //    // 6. Error handling
        //    default:
        //        _error = $"Request Get mail with error! Request state: {_request.State}";
        //        DPDebug.LogWarning(_error);
        //        _callback.Invoke(false, _error);
        //        break;
        //}*/
        string _url;

        UnityWebRequest _request;
        switch (_type)
        {
            case MailBoxType.GetAll:
                _url = urlGetListMailbox + $"/{_param1}?section={param2}";
                _request = UnityWebRequest.Get(_url);

                break;
            case MailBoxType.MarkRead:
                _url = urlMarkAsRead + $"/{_param1}?userid={param2}";
                _request = UnityWebRequest.Post(_url, "");

                break;
            default:
                _url = urlClaimReward + $"/{_param1}?userid={param2}";
                _request = UnityWebRequest.Post(_url, "");

                break;
        }
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        DPDebug.Log("_url mailbox: " + _url);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get mail data successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request get mail data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request get mail data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    public static void DeleteAllMailBox(MonoBehaviour _mono, string _id, Action<bool, string> _callback)
    {
        _mono.StartCoroutine(IEDeleteAllMail(_id, _callback));
    }
    private static IEnumerator IEDeleteAllMail(string _id, Action<bool, string> _callback)
    {
        string _url = urlDeleteAllMail + _id;

        DPDebug.Log("_url mailbox: " + _url);
        /*
        //var _request = new HTTPRequest(_url, _type == MailBoxType.GetAll ? HTTPMethods.Get : HTTPMethods.Post)
        //{
        //    Authenticator = new BearerTokenAuthenticator(token)
        //};
        //var _request = new HTTPRequest(new Uri(_url), methodType: HTTPMethods.Delete);
        //_request.SetHeader("Authorization", $"Bearer {token}");
        //_request.Send();

        //yield return _request;
        //string _error;
        //switch (_request.State)
        //{
        //    case HTTPRequestStates.Finished:
        //        // 5. Process response
        //        if (_request.Response.IsSuccess)
        //        {
        //            if (_request.Response.StatusCode < 404)
        //            {
        //                var _json = _request.Response.DataAsText;
        //                DPDebug.Log($"Delete mail successful json: \n{_json}");
        //                _callback?.Invoke(true, _json);
        //            }
        //            else
        //            {
        //                _error = $"Delete mail with an error: {_request.Response.StatusCode}";
        //                DPDebug.LogWarning(_error);
        //                _callback?.Invoke(false, _error);
        //            }
        //        }
        //        else
        //        {
        //            // 6. Error handling
        //            _error = $"Delete mail with an error: {_request.Response.StatusCode}-{_request.Response.Message}";
        //            DPDebug.LogWarning(_error);
        //            DPDebug.LogWarning($"Delete mail text: \n{_request.Response.DataAsText}");
        //            _callback?.Invoke(false, _error);
        //        }

        //        break;
        //    // 6. Error handling
        //    default:
        //        _error = $"Request delete mail with error! Request state: {_request.State}";
        //        DPDebug.LogWarning(_error);
        //        _callback.Invoke(false, _error);
        //        break;
        //}*/
        var _request = UnityWebRequest.Delete(_url);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Delete mail data successful json");
                    _callback.Invoke(true, string.Empty);
                }
                else
                {
                    _error = $"Request delete mail data with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request delete mail data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    public static void ClaimAllMail(MonoBehaviour _mono, string _id, Action<bool, string> _callback)
    {
        _mono.StartCoroutine(IEClaimAllMail(_id, _callback));
    }

    private static IEnumerator IEClaimAllMail(string id, Action<bool, string> _callback)
    {
        string _url = urlClaimAllMail + id;

        DPDebug.Log("_url claimMailbox: " + _url);
        /*
        //var _request = new HTTPRequest(_url, _type == MailBoxType.GetAll ? HTTPMethods.Get : HTTPMethods.Post)
        //{
        //    Authenticator = new BearerTokenAuthenticator(token)
        //};
        //var _request = new HTTPRequest(new Uri(_url), methodType: HTTPMethods.Post);
        //_request.SetHeader("Authorization", $"Bearer {token}");
        //_request.Send();

        //yield return _request;
        //string _error;
        //switch (_request.State)
        //{
        //    case HTTPRequestStates.Finished:
        //        // 5. Process response
        //        if (_request.Response.IsSuccess)
        //        {
        //            if (_request.Response.StatusCode < 404)
        //            {
        //                var _json = _request.Response.DataAsText;
        //                DPDebug.Log($"Claim all mail successful json: \n{_json}");
        //                _callback?.Invoke(true, _json);
        //            }
        //            else
        //            {
        //                _error = $"Claim all mail with an error: {_request.Response.StatusCode}";
        //                DPDebug.LogWarning(_error);
        //                _callback?.Invoke(false, _error);
        //            }
        //        }
        //        else
        //        {
        //            // 6. Error handling
        //            _error = $"Claim all mail with an error: {_request.Response.StatusCode}-{_request.Response.Message}";
        //            DPDebug.LogWarning(_error);
        //            DPDebug.LogWarning($"Claim all mail text: \n{_request.Response.DataAsText}");
        //            _callback?.Invoke(false, _error);
        //        }

        //        break;
        //    // 6. Error handling
        //    default:
        //        _error = $"Request delete mail with error! Request state: {_request.State}";
        //        DPDebug.LogWarning(_error);
        //        _callback.Invoke(false, _error);
        //        break;
        //}*/
        var _request = UnityWebRequest.Post(_url, "");
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Claim all successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request claim all with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request claim all data with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    //===============================================================================================================================
    public static void GetAllEvent(
        MonoBehaviour _mono,
        Action<bool, string> _callback,
        bool _isIncludeLeaderboard = false)
    {
        _mono.StartCoroutine(IEAllEvent(_callback, _isIncludeLeaderboard));
    }

    /// <summary>Lấy riêng dữ liệu của một event theo eventName, không join event.</summary>
    public static void GetEvent(MonoBehaviour _mono, Action<bool, string> _callback, EVENT_TYPE _type, bool _isIncludeLeaderboard = false)
    {
        _mono.StartCoroutine(IEGetEvent(_callback, _type, _isIncludeLeaderboard));
    }

    private static IEnumerator IEGetEvent(
        Action<bool, string> _callback,
        EVENT_TYPE _type,
        bool _isIncludeLeaderboard)
    {
        var _includeLeaderboard = _isIncludeLeaderboard ? "true" : "false";
        var _url = $"{urlAllEvent}{Player.Id}?eventName={_type}&isIncludeLB={_includeLeaderboard}";
        DPDebug.Log($"Get event[{_type}] url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;

        yield return _request.SendWebRequest();
        DPDebug.Log($"Get event[{_type}] result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get event[{_type}] successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Get event[{_type}] with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Get event[{_type}] with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    private static IEnumerator IEAllEvent(Action<bool, string> _callback, bool _isIncludeLeaderboard)
    {
        var _includeLeaderboard = _isIncludeLeaderboard ? "true" : "false";
        var _url = $"{urlAllEvent}{Player.Id}?isIncludeLB={_includeLeaderboard}";
        DPDebug.Log($"Check url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get all event successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Get all event with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Get all event with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetEventBattleTeamActive(MonoBehaviour _mono, Action<bool, string> _callback)
    {
        _mono.StartCoroutine(IEEventBattleTeamActive(_callback));
    }
    private static IEnumerator IEEventBattleTeamActive(Action<bool, string> _callback)
    {
        var _url = $"{urlEventBattleTeamActive}";
        DPDebug.Log($"Check url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get event BATTLE TEAM active time successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request event BATTLE TEAM error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request event BATTLE TEAM error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void GetEventActiveTime(MonoBehaviour _mono, Action<bool, string> _callback, EVENT_TYPE _type)
    {
        _mono.StartCoroutine(IEEventActiveTime(_callback, _type));
    }
    private static IEnumerator IEEventActiveTime(Action<bool, string> _callback, EVENT_TYPE _type)
    {
        var _url = $"{urlEventActiveTime}{_type}";
        DPDebug.Log($"Check url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Get(_uri);
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get event[{_type}] active time successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request event[{_type}] error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request event[{_type}] error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
    public static void ConnectEvent(MonoBehaviour _mono, Action<bool, string> _callback, bool _isIncludeLeaderboard, EVENT_TYPE _type)
    {
        _mono.StartCoroutine(IEConnectEvent(_callback, _isIncludeLeaderboard, _type));
    }
    private static IEnumerator IEConnectEvent(Action<bool, string> _callback, bool _isIncludeLeaderboard, EVENT_TYPE _type)
    {
        var _url = $"{urlConnectEventEndless}{Player.Id}?eventName={_type}";
        if (_isIncludeLeaderboard)
            _url += "&includeLeaderboard=true";
        DPDebug.Log($"Check url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Post(_uri, "");
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Check player join successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Request Check player join with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }
                break;
            default:
                _error = $"Request Check player join with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }

    public static void ClaimEvent(MonoBehaviour _mono, Action<bool, string> _callback, bool _isIncludeLeaderboard, EVENT_TYPE _type)
    {
        _mono.StartCoroutine(IEClaimEvent(_callback, _isIncludeLeaderboard, _type));
    }

    private static IEnumerator IEClaimEvent(Action<bool, string> _callback, bool _isIncludeLeaderboard,
        EVENT_TYPE _type)
    {
        var _url = $"{urlClaimEventEndless}{Player.Id}?eventName={_type}";
        if (_isIncludeLeaderboard) _url += "&includeLeaderboard=true";
        DPDebug.Log($"Check url: {_url}");
        var _uri = new Uri(_url);
        var _request = UnityWebRequest.Post(_uri, "");
        _request.timeout = TimeOut;
        _request.certificateHandler = new BypassCertificate();
        _request.SetRequestHeader("Authorization", "Bearer " + token);
        string _error;
        // 3. Send
        yield return _request.SendWebRequest();
        DPDebug.Log($"_request.result: {_request.result}");
        switch (_request.result)
        {
            case UnityWebRequest.Result.Success:
                var _json = _request.downloadHandler.text;
                if (_request.responseCode < 203)
                {
                    DPDebug.Log($"Get player claim event successful json: \n{_json}");
                    _callback.Invoke(true, _json);
                }
                else
                {
                    _error = $"Get Check player claim event with an error: {_request.error}";
                    DPDebug.LogWarning(_error);
                    _callback.Invoke(false, _error);
                }

                break;
            default:
                _error = $"Get Check player claim event with an error: {_request.error}";
                DPDebug.LogWarning(_error);
                _callback.Invoke(false, _error);
                break;
        }
    }
}

public class MailboxResponseMessage
{
    public string message;
}
public enum MailBoxType
{
    GetAll,
    MarkRead,
    ClaimReward
}
