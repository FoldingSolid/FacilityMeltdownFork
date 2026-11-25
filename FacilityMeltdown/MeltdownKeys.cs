using Dawn;

namespace FacilityMeltdown;

public static class MeltdownKeys
{
    public static readonly NamespacedKey<DawnItemInfo> GeigerCounter = NamespacedKey<DawnItemInfo>.From("facility_meltdown", "geiger_counter");
}

public static class MeltdownTags
{
    public static readonly NamespacedKey NotSpawnable = NamespacedKey.From("facility_meltdown", "meltdown_not_spawnable");
}
