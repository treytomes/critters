
using Newtonsoft.Json;

namespace Critters;

static class ObjectExtensions
{
	public static string Dump(this object @this)
	{
		return JsonConvert.SerializeObject(@this, new JsonSerializerSettings()
		{
			Formatting = Formatting.Indented,
			StringEscapeHandling = StringEscapeHandling.EscapeNonAscii | StringEscapeHandling.EscapeHtml,
			NullValueHandling = NullValueHandling.Include,
		});
	}
}