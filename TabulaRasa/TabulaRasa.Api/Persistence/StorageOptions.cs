namespace TabulaRasa.Api.Persistence
{
    public sealed class StorageOptions
    {
        public int CheckpointIntervalTicks { get; set; } = 25;
        public int RunPageSize { get; set; } = 50;
        public int? RetainLatestRuns { get; set; } = 100;
        public int? RetainRunsForDays { get; set; } = 30;
        public int CompressCheckpointsOlderThanDays { get; set; } = 7;
        public bool ApplyMigrationsOnStartup { get; set; } = true;
    }
}
