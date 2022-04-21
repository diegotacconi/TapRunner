using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenTap;

namespace TapRunner
{
    internal class TestPlanRunner
    {
        private static readonly TraceSource log = Log.CreateSource("Main");

        public static Verdict RunPlanForDut(TestPlan plan, List<ResultParameter> metadata,
            CancellationToken cancellationToken)
        {
            plan.PrintTestPlanRunSummary = true;
            return plan.ExecuteAsync(ResultSettings.Current, metadata, null, cancellationToken).Result.Verdict;
        }

        public static void SetSettingsDir(string dir)
        {
            ComponentSettings.PersistSettingGroups = false;
            var settingsSetDir = Path.Combine(ComponentSettings.SettingsDirectoryRoot, "Bench", dir);
            if (dir != "Default" && !Directory.Exists(settingsSetDir))
                throw new ArgumentException($"Could not find settings directory \"{settingsSetDir}\"");

            ComponentSettings.SetSettingsProfile("Bench", dir);
            log.TraceInformation("Settings: " + ComponentSettings.GetSettingsDirectory("Bench"));
        }
    }
}