using System;
using System.Collections.Generic;
using System.Linq;
using DP.Utilities;
using UnityEngine;

// =============================================================================
// America Liberation Grid Event — Monetization (Phase 3)
// partial class GridEvent.
//   - Daily Refresh Pack (reset mỗi ngày).
//   - 4 Event Packs (giới hạn mua trong toàn event).
//   - 3 Rotating Offers khi hết vé: xuất hiện lần lượt/xoay vòng, đã mua không
//     quay lại, có đếm ngược, biến icon; KHÔNG hiện gói mới đến khi hết đếm
//     ngược gói cũ.
//
// IAP: product id (config.ProductId) phải trùng tên 1 giá trị enum IAP_Product
// (vd "aa_gridevent1"). Mua qua CC_Interface.current.PurchaseItem(product).
// Khi thành công, CC_Interface gọi lại GridEvent.OnPurchaseSuccess(product).
// =============================================================================

public static partial class GridEvent
{
    public static event Action OnShopChanged;

    // ───────────────────────── DAILY PACK ─────────────────────────

    public static GridPackConfig GetDailyPack()
    {
        var list = GridEvents.Cfg.DailyPacks;
        return (list != null && list.Count > 0) ? list[0] : null;
    }

    public static bool CanBuyDailyPack()
    {
        var pack = GetDailyPack();
        return pack != null && IsAvailable && !data.DailyPackPurchased;
    }

    // ───────────────────────── EVENT PACKS ─────────────────────────

    public static List<GridPackConfig> GetEventPacks() => GridEvents.Cfg.EventPacks;

    public static int GetEventPackPurchasedCount(int _index)
        => data?.EventPackPurchases.FirstOrDefault(p => p.Index == _index)?.PurchasedCount ?? 0;

    /// <summary>Giới hạn mua mỗi event pack (mặc định 1 lần/event nếu config &le; 0).</summary>
    static int EventPackLimit(GridPackConfig _cfg) => _cfg.LimitCount > 0 ? _cfg.LimitCount : 1;

    public static bool CanBuyEventPack(int _index)
    {
        var packs = GetEventPacks();
        if (packs == null || _index < 0 || _index >= packs.Count) return false;
        // Event pack: mua 1 lần trong CẢ EVENT (EventPackPurchases không reset).
        return IsAvailable && GetEventPackPurchasedCount(_index) < EventPackLimit(packs[_index]);
    }

    public static int GetEventPackRemaining(int _index)
    {
        var packs = GetEventPacks();
        if (packs == null || _index < 0 || _index >= packs.Count) return 0;
        return Mathf.Max(0, EventPackLimit(packs[_index]) - GetEventPackPurchasedCount(_index));
    }

    // ───────────────────────── OFFERS (rotating) ─────────────────────────

    public static List<GridOfferConfig> GetOffers() => GridEvents.Cfg.Offers;

    public static GridOfferConfig GetOfferById(int _id)
        => GetOffers()?.FirstOrDefault(o => o.ID == _id);

    /// <summary>Offer đang active (còn đếm ngược). null nếu không có.</summary>
    public static GridOfferConfig GetActiveOffer()
    {
        RefreshOfferState();
        return data.ActiveOfferId != -1 ? GetOfferById(data.ActiveOfferId) : null;
    }

    public static bool IsOfferActive
    {
        get { RefreshOfferState(); return data != null && data.ActiveOfferId != -1; }
    }

    /// <summary>Thời gian còn lại của offer đang active (cho countdown + icon).</summary>
    public static TimeSpan OfferTimeRemaining
    {
        get
        {
            if (data == null || data.ActiveOfferId == -1) return TimeSpan.Zero;
            var end = new DateTime(data.ActiveOfferEndTicks, DateTimeKind.Utc);
            var remain = end - DPUtils.GetUtcNow;
            if (remain <= TimeSpan.Zero) return TimeSpan.Zero;

            // CLAMP theo duration cấu hình hiện tại: end được lưu tuyệt đối lúc kích hoạt, nên nếu
            // đồng hồ/timezone máy lùi lại, HOẶC end lưu từ khi duration còn dài hơn (đổi 48h→24h
            // nhưng offer cũ vẫn đang active) thì remain có thể > duration → đếm nhảy thành 48h.
            // Chặn lại: không bao giờ hiện quá thời lượng cấu hình.
            var cfg = GetOfferById(data.ActiveOfferId);
            if (cfg != null)
            {
                var max = TimeSpan.FromSeconds(Mathf.Max(1, cfg.DurationSeconds));
                if (remain > max) remain = max;
            }
            return remain;
        }
    }

    /// <summary>
    /// Khi offer hết đếm ngược → TỰ chuyển sang gói kế tiếp (auto-chain, xoay vòng,
    /// bỏ gói đã mua; gói cuối → quay lại gói 1 nếu chưa mua). Nếu không còn gói chưa
    /// mua → tắt hẳn. (Mua gói KHÔNG đi qua đây — OnPurchaseSuccess xoá active nên
    /// chuỗi dừng, chờ người chơi hết vé lần sau mới hiện gói tiếp.)
    /// </summary>
    public static void RefreshOfferState()
    {
        if (data == null || data.ActiveOfferId == -1) return;

        // Kéo end về đúng thời lượng cấu hình nếu đang lưu quá dài (đổi duration 48h→24h khi offer
        // còn active, hoặc đồng hồ máy lùi) → offer không sống/đếm quá duration.
        var activeCfg = GetOfferById(data.ActiveOfferId);
        if (activeCfg != null)
        {
            long maxEndTicks = DPUtils.GetUtcNow.AddSeconds(Mathf.Max(1, activeCfg.DurationSeconds)).Ticks;
            if (data.ActiveOfferEndTicks > maxEndTicks)
            {
                data.ActiveOfferEndTicks = maxEndTicks;
                SaveData();
            }
        }

        var end = new DateTime(data.ActiveOfferEndTicks, DateTimeKind.Utc);
        if (DPUtils.GetUtcNow < end) return; // còn đếm ngược → giữ nguyên

        AdvanceRotationPast(data.ActiveOfferId); // qua gói vừa hết giờ
        var next = PickNextOffer();
        if (next != null)
        {
            data.ActiveOfferId = next.ID;
            data.ActiveOfferEndTicks = DPUtils.GetUtcNow.AddSeconds(Mathf.Max(1, next.DurationSeconds)).Ticks;
        }
        else
        {
            data.ActiveOfferId = -1; // hết gói chưa mua → tắt
            data.ActiveOfferEndTicks = 0;
        }
        SaveData();
        OnShopChanged?.Invoke();
    }

    /// <summary>
    /// Gọi khi người chơi bấm mở ô nhưng đã HẾT VÉ. Quyết định offer hiển thị:
    ///  - Nếu offer cũ còn đếm ngược → giữ nguyên (không hiện gói mới).
    ///  - Nếu không có offer active → kích hoạt gói kế tiếp trong vòng xoay (bỏ gói đã mua).
    /// Trả về offer cần hiển thị (null nếu hết gói / không khả dụng).
    /// </summary>
    public static GridOfferConfig EvaluateOfferOnOutOfTickets()
    {
        if (data == null || !IsAvailable) return null;
        RefreshOfferState();

        if (data.ActiveOfferId != -1)
            return GetOfferById(data.ActiveOfferId);

        var next = PickNextOffer();
        if (next == null) return null;

        data.ActiveOfferId = next.ID;
        data.ActiveOfferEndTicks = DPUtils.GetUtcNow.AddSeconds(Mathf.Max(1, next.DurationSeconds)).Ticks;
        SaveData();
        OnShopChanged?.Invoke();
        DPDebug.Log($"[GridEvent] Activate offer {next.ProductId} ({next.DurationSeconds}s)");
        return next;
    }

    /// <summary>Gói offer kế tiếp chưa mua, bắt đầu từ con trỏ xoay vòng.</summary>
    static GridOfferConfig PickNextOffer()
    {
        var offers = GetOffers();
        if (offers == null || offers.Count == 0) return null;

        for (int step = 0; step < offers.Count; step++)
        {
            int idx = (data.OfferRotationIndex + step) % offers.Count;
            var o = offers[idx];
            if (!data.PurchasedOfferIds.Contains(o.ID))
                return o;
        }
        return null; // đã mua hết
    }

    static void AdvanceRotationPast(int _offerId)
    {
        var offers = GetOffers();
        if (offers == null || offers.Count == 0) return;
        int idx = offers.FindIndex(o => o.ID == _offerId);
        if (idx >= 0)
            data.OfferRotationIndex = (idx + 1) % offers.Count;
    }

    // ───────────────────────── PURCHASE REQUEST ─────────────────────────

    public static void RequestPurchaseDailyPack()
    {
        var pack = GetDailyPack();
        if (pack != null) RequestPurchase(pack.ProductId);
    }

    public static void RequestPurchaseEventPack(int _index)
    {
        var packs = GetEventPacks();
        if (packs != null && _index >= 0 && _index < packs.Count)
            RequestPurchase(packs[_index].ProductId);
    }

    public static void RequestPurchaseOffer(int _offerId)
    {
        var offer = GetOfferById(_offerId);
        if (offer != null) RequestPurchase(offer.ProductId);
    }

    /// <summary>
    /// Gói grid đang chờ callback mua. Trên iOS các gói grid dùng chung SKU gempack,
    /// nên khi callback về case gempack trong CC_Interface không còn biết gói thật —
    /// phải nhớ lại từ lúc request.
    /// </summary>
    static IAP_Product pendingPurchase = IAP_Product.None;

    static void RequestPurchase(IAP_Product _productId)
    {
        pendingPurchase = _productId;
        CC_Interface.current.PurchaseItem(_productId);
    }

    /// <summary>Gọi khi mua thất bại/huỷ để gói chờ không bị áp nhầm cho lần mua gempack sau.</summary>
    public static void ClearPendingPurchase() => pendingPurchase = IAP_Product.None;

    // ───────────────────────── PURCHASE SUCCESS (callback từ CC_Interface) ──

    /// <summary>
    /// Bản không tham số cho case gempack (iOS): iap_Product lúc đó là aa_gempackN,
    /// không dùng được — áp dụng gói đã nhớ khi RequestPurchase. Không có gói chờ
    /// (vd mua/restore gempack thật) → bỏ qua, không cộng vé nhầm.
    /// </summary>
    public static void OnPurchaseSuccess()
    {
        var _product = pendingPurchase;
        if (_product == IAP_Product.None) return;
        OnPurchaseSuccess(_product);
    }

    /// <summary>Áp dụng kết quả mua thành công. Gọi từ CC_Interface.PurchaseSuccessCallback.</summary>
    public static bool OnPurchaseSuccess(IAP_Product _product)
    {
        pendingPurchase = IAP_Product.None;
        if (data == null) { InitializeIfNeeded(); }

        // 1. Daily pack
        var daily = GetDailyPack();
        if (daily != null && daily.ProductId == _product)
        {
            AddTickets(daily.TicketAmount, true);
            data.DailyPackPurchased = true;
            SaveData();
            GridEventLog.Fire(GridEventLog.GRID_DAILY_PACK);
            FireTicketsPurchased(daily.TicketAmount); // vé bay lên top
            OnShopChanged?.Invoke();
            return true;
        }

        // 2. Event packs (aa_gridevent1..4)
        var packs = GetEventPacks();
        if (packs != null)
        {
            for (int i = 0; i < packs.Count; i++)
            {
                if (packs[i].ProductId != _product) continue;
                AddTickets(packs[i].TicketAmount, true);
                var rec = data.EventPackPurchases.FirstOrDefault(p => p.Index == i);
                if (rec == null) { rec = new GridPackPurchase { Index = i }; data.EventPackPurchases.Add(rec); }
                rec.PurchasedCount++;
                SaveData();
                FireEventPackLog(i);
                FireTicketsPurchased(packs[i].TicketAmount); // vé bay lên top
                OnShopChanged?.Invoke();
                return true;
            }
        }

        // 3. Offers (aa_gridprogress1..3)
        var offers = GetOffers();
        if (offers != null)
        {
            for (int i = 0; i < offers.Count; i++)
            {
                if (offers[i].ProductId != _product) continue;
                AddTickets(offers[i].TicketAmount, true);
                FireTicketsPurchased(offers[i].TicketAmount); // vé bay lên top
                if (!data.PurchasedOfferIds.Contains(offers[i].ID))
                    data.PurchasedOfferIds.Add(offers[i].ID);
                // Gói đã mua → đóng offer active, xoay sang gói kế.
                AdvanceRotationPast(offers[i].ID);
                if (data.ActiveOfferId == offers[i].ID)
                {
                    data.ActiveOfferId = -1;
                    data.ActiveOfferEndTicks = 0;
                }
                SaveData();
                FireOfferLog(i);
                OnShopChanged?.Invoke();
                return true;
            }
        }

        DPDebug.LogWarning($"[GridEvent] OnPurchaseSuccess: product '{_product}' không khớp pack/offer nào.");
        return false;
    }

    static void FireEventPackLog(int _index)
    {
        switch (_index)
        {
            case 0: GridEventLog.Fire(GridEventLog.GRID_PACK1); break;
            case 1: GridEventLog.Fire(GridEventLog.GRID_PACK2); break;
            case 2: GridEventLog.Fire(GridEventLog.GRID_PACK3); break;
            case 3: GridEventLog.Fire(GridEventLog.GRID_PACK4); break;
        }
    }

    static void FireOfferLog(int _index)
    {
        switch (_index)
        {
            case 0: GridEventLog.Fire(GridEventLog.GRID_PROGRESS_PACK1); break;
            case 1: GridEventLog.Fire(GridEventLog.GRID_PROGRESS_PACK2); break;
            case 2: GridEventLog.Fire(GridEventLog.GRID_PROGRESS_PACK3); break;
        }
    }
}
