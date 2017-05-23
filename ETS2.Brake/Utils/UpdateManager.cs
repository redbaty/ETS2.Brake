using System;
using System.Threading;
using System.Threading.Tasks;
using Squirrel;

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

                    using (var mgr =
                        Squirrel.UpdateManager.GitHubUpdateManager("https://github.com/redbaty/ETS2.Brake"))
                    {
                        await mgr.Result.UpdateApp();
                    }

                    Thread.Sleep(new TimeSpan(0,1,0));
                }
            });
        }
    }
}