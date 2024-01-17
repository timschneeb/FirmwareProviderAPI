using System.ComponentModel;

namespace FirmwareProviderAPI
{
    public enum Models
    {
        [Description("Unknown model")]
        Unknown,
        [Description("Galaxy Buds")]
        Buds,
        [Description("Galaxy Buds+")]
        BudsPlus,
        [Description("Galaxy Buds Live")]
        BudsLive,
        [Description("Galaxy Buds Pro")]
        BudsPro,
        [Description("Galaxy Buds2")]
        Buds2,
        [Description("Galaxy Buds2 Pro")]
        Buds2Pro,
        [Description("Galaxy Buds FE")]
        BudsFe
    }
}