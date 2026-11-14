namespace EfCoreInterceptors.Interceptors.Tenancy;

/// <summary>Holds the current tenant. In a web app this would come from the request.</summary>
public class TenantContext
{
    public string Current { get; set; } = "tenant-a";
}
