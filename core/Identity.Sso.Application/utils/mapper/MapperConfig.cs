using Mapster;

namespace Identity.Sso.Application.Utils.Mapper;

public static class MapperConfig
{
    public static void RegisterAppMappings()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MapperConfig).Assembly);
    }

}
