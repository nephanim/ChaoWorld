using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ChaoWorld.Core
{
    public static class BuildInfoService
    {
        public static string Version { get; private set; }

        public static async Task LoadVersion()
        {
            Version = "1.0";

            /*using (var stream = typeof(BuildInfoService).Assembly.GetManifestResourceStream("version"))
            {
                // if this happens, something broke
                if (stream == null) Version = "(unknown version) ";
                else using (var reader = new StreamReader(stream)) Version = await reader.ReadToEndAsync();
            }

            // cheap hack to remove newline
            Version = Version.Remove(Version.Length - 1);*/
        }
    }
}