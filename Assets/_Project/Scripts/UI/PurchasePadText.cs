// Presentation only: callers retain all payment, level and save calculations.
public static class PurchasePadText
{
    private static string Title(string name)
    {
        switch (name)
        {
            case "추가 입고 시설":
            case "두 번째 입고 시설": name = "두 번째\n입고 시설"; break;
            case "입고 직원 고용": name = "입고 직원\n고용"; break;
            case "출고 직원 고용": name = "출고 직원\n고용"; break;
            case "플레이어 운반량": name = "플레이어\n운반량"; break;
            case "직원 이동 속도": name = "직원 이동\n속도"; break;
        }
        return $"<b>{name}</b>\n";
    }

    private static string Price(int cost, int paid)
    {
        return paid > 0 ? $"남은 금액 ₩{cost - paid:N0}" : $"₩{cost:N0}";
    }

    public static string Purchase(string name, int cost, int paid)
    {
        return Title(name) + $"<size=80%><b>{Price(cost, paid)}</b></size>";
    }

    public static string Upgrade(string name, int level, bool canUpgrade, int cost, int paid,
        string currentEffect = "", string nextEffect = "")
    {
        if (!canUpgrade)
            return Title(name) + $"<size=78%>Lv.{level}  <b>MAX</b></size>\n<size=70%>{currentEffect}</size>";
        string effect = string.IsNullOrEmpty(currentEffect) ? "" :
            $"<size=70%>{currentEffect} → {nextEffect}</size>\n";
        return Title(name) + $"<size=78%>Lv.{level} → Lv.{level + 1}</size>\n" +
            effect + $"<size=80%><b>{Price(cost, paid)}</b></size>";
    }
}
