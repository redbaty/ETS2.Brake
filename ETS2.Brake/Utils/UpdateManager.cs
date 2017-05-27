using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHubUpdate;

namespace ETS2.Brake.Utils
{
    public static class UpdateManager
    {
        public static void CheckForUpdates()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

                    var checker =
                        new UpdateChecker("redbaty", "ETS2.Brake",
                            fvi.ProductVersion); // uses your Application.ProductVersion

                    var update = await checker.CheckUpdate();

                    if (update != UpdateType.None)
                        Report.Info("There's an update available");

                    Thread.Sleep(new TimeSpan(0, 1, 0));
                }
            });
        }
    }
}