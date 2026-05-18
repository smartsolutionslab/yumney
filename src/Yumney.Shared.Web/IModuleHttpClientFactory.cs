namespace SmartSolutionsLab.Yumney.Shared.Web;

public interface IModuleHttpClientFactory
{
	IModuleHttpClient For(string upstreamName);
}
