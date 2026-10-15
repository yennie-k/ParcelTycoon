// Presentation only: callers retain all payment, level and save calculations.
public static class PurchasePadText
{
    private static string Title(string name)
    {
        switch (name)
        {
            case "추가 입고 시설": name = "추가 입고\n시설"; break;
            case "입고 직원 고용": name = "입고 직원\n고용"; break;
            case "출고 직원 고용": name = "출고 직원\n고용"; break;
            case "플레이어 운반량": name = "플레이어\n운반량"; break;
            case "직원 이동 속도": name = "직원 이동\n속도"; break;
        }
        return $"<b>{name}</b>\n";
    }

    private static string Price(int cost, int paid)
    {
        return paid > 0
            ? $"남은 {cost - paid:N0}원\n<size=85%>결제 {paid:N0}원</size>"
            : $"구매 {cost:N0}원";
    }

    public static string Purchase(string name, int cost, int paid)
    {
        return Title(name) + $"<size=72%>{Price(cost, paid)}</size>";
    }

    public static string Upgrade(string name, int level, bool canUpgrade, int cost, int paid)
    {
        if (!canUpgrade)
            return Title(name) + $"<size=80%>Lv.{level} · <b>MAX</b></size>";
        // Keep a partial payment on one secondary line so long names still fit.
        string price = paid > 0 ? $"남은 {cost - paid:N0}원" : $"다음 {cost:N0}원";
        return Title(name) + $"<size=72%>Lv.{level} → Lv.{level + 1}\n{price}</size>";
    }
}
