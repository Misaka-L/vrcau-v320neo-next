namespace VAU.V320NeoNext.Runtime.Bus
{
    /// <summary>
    /// 航电总线的数据 id 定义。
    /// <para>
    /// 参数命名约定：<c>飞机名称_更新频繁程度_系统_自定义变量名</c>，例如
    /// <c>V32NN_Frequent_ADR_AltitudeFeet</c>：
    /// <list type="bullet">
    /// <item><description>飞机名称：V32NN</description></item>
    /// <item><description>更新频繁程度：Frequent_ / Infrequent_</description></item>
    /// <item><description>系统：例如 ADR</description></item>
    /// <item><description>自定义变量名：例如 AltitudeFeet</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 每个 enum 的成员值即 AvionicsBus 中对应数组的下标，末尾的 <c>Count</c> 是分配数组用的哨兵，
    /// 必须始终放在最后且不要给它赋值以外的用途。
    /// </para>
    /// </summary>
    public enum AvionicsBusFloatDataIds
    {
        // ATA22_40. Auto Flight / Flight Augmentation / FAC
        V32NN_Frequent_FAC_1_Stall_Warning_Speed,

        // ATA34. Navigation / ADIRS / ADR
        V32NN_Frequent_ADIRS_ADR_1_AltitudeFeet,
        V32NN_Frequent_ADIRS_ADR_1_IndicatedAirspeed,
        V32NN_Frequent_ADIRS_ADR_1_VerticalSpeedFeetPerMinute,
        V32NN_Frequent_ADIRS_ADR_1_MachNumber,
        V32NN_Frequent_ADIRS_ADR_1_AoA,
        // ATA34. Navigation / ADIRS / IR
        V32NN_Frequent_ADIRS_IR_1_Vertical_G_Load,

        // ATA34. Navigation / Radio Altimeter
        V32NN_Frequent_RA_1_RadioAltitude,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusIntDataIds
    {
        // ATA27. Flight Control / Flaps
        V32NN_Infrequent_Flap_LevelerPosition,
        V32NN_Infrequent_Flap_SFCC_1_ActualFlapPosition,

        V32NN_Frequent_ADR_HeadingDegrees,
        V32NN_Frequent_ADIRS_AlignmentState,
        Count
    }

    public enum AvionicsBusByteDataIds
    {
        // ATA31. Indicating Recording / EFIS Control
        // Left EFIS
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayFilter,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayPage,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayRange,
        V32NN_Infrequent_EFIS_Left_Sync_NavigationDisplayVorAdfSelector,

        // Right EFIS
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayFilter,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayPage,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayRange,
        V32NN_Infrequent_EFIS_Right_Sync_NavigationDisplayVorAdfSelector,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusBoolDataIds
    {
        // ATA31. Indicating Recording / EFIS Control
        // Left EFIS
        V32NN_Infrequent_EFIS_Left_Sync_FlightDirectorOn,
        V32NN_Infrequent_EFIS_Left_Sync_LandingSystemOn,

        // Right EFIS
        V32NN_Infrequent_EFIS_Right_Sync_FlightDirectorOn,
        V32NN_Infrequent_EFIS_Right_Sync_LandingSystemOn,

        // Avionics Bus
        Count
    }

    public enum AvionicsBusStringDataIds
    {
        V32NN_Infrequent_ECAM_ActiveMessage = 0,
        Count = 1
    }

    public enum AvionicsBusVector3DataIds
    {
        V32NN_Frequent_ADR_VelocityNED = 0,
        V32NN_Infrequent_ND_WindVector = 1,
        Count = 2
    }
}