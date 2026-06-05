using Microsoft.EntityFrameworkCore;
using TabulaRasa.Api.Persistence.Entities;

namespace TabulaRasa.Api.Persistence
{
    public sealed class SimulationDbContext : DbContext
    {
        public SimulationDbContext(DbContextOptions<SimulationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SimulationRunEntity> Runs => Set<SimulationRunEntity>();
        public DbSet<SimulationCheckpointEntity> Checkpoints => Set<SimulationCheckpointEntity>();
        public DbSet<SimulationEventEntity> Events => Set<SimulationEventEntity>();
        public DbSet<SimulationTickSummaryEntity> TickSummaries => Set<SimulationTickSummaryEntity>();
        public DbSet<SimulationScenarioEntity> Scenarios => Set<SimulationScenarioEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SimulationRunEntity>(entity =>
            {
                entity.ToTable("simulation_runs");
                entity.HasKey(run => run.Id);
                entity.Property(run => run.Id).HasColumnName("id").HasMaxLength(64);
                entity.Property(run => run.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(run => run.Status).HasColumnName("status").HasMaxLength(32);
                entity.Property(run => run.CurrentTick).HasColumnName("current_tick");
                entity.Property(run => run.AgentCount).HasColumnName("agent_count");
                entity.Property(run => run.AliveAgentCount).HasColumnName("alive_agent_count");
                entity.Property(run => run.DeadAgentCount).HasColumnName("dead_agent_count");
                entity.Property(run => run.FoodCount).HasColumnName("food_count");
                entity.Property(run => run.GridWidth).HasColumnName("grid_width");
                entity.Property(run => run.GridHeight).HasColumnName("grid_height");
                entity.Property(run => run.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb");
                entity.Property(run => run.SourceSimulationId).HasColumnName("source_simulation_id").HasMaxLength(64);
                entity.Property(run => run.SourceTick).HasColumnName("source_tick");
                entity.Property(run => run.StorageBytes).HasColumnName("storage_bytes");
                entity.Property(run => run.CheckpointBytes).HasColumnName("checkpoint_bytes");
                entity.Property(run => run.EventBytes).HasColumnName("event_bytes");
                entity.Property(run => run.CreatedAt).HasColumnName("created_at");
                entity.Property(run => run.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(run => run.UpdatedAt);
            });

            modelBuilder.Entity<SimulationCheckpointEntity>(entity =>
            {
                entity.ToTable("simulation_checkpoints");
                entity.HasKey(checkpoint => checkpoint.Id);
                entity.Property(checkpoint => checkpoint.Id).HasColumnName("id");
                entity.Property(checkpoint => checkpoint.RunId).HasColumnName("run_id").HasMaxLength(64);
                entity.Property(checkpoint => checkpoint.Tick).HasColumnName("tick");
                entity.Property(checkpoint => checkpoint.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
                entity.Property(checkpoint => checkpoint.CompressedPayload).HasColumnName("compressed_payload");
                entity.Property(checkpoint => checkpoint.PayloadBytes).HasColumnName("payload_bytes");
                entity.Property(checkpoint => checkpoint.IsCompressed).HasColumnName("is_compressed");
                entity.Property(checkpoint => checkpoint.CreatedAt).HasColumnName("created_at");
                entity.HasOne(checkpoint => checkpoint.Run)
                    .WithMany(run => run.Checkpoints)
                    .HasForeignKey(checkpoint => checkpoint.RunId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(checkpoint => new { checkpoint.RunId, checkpoint.Tick }).IsUnique();
            });

            modelBuilder.Entity<SimulationEventEntity>(entity =>
            {
                entity.ToTable("simulation_events");
                entity.HasKey(simulationEvent => simulationEvent.Id);
                entity.Property(simulationEvent => simulationEvent.Id).HasColumnName("id");
                entity.Property(simulationEvent => simulationEvent.RunId).HasColumnName("run_id").HasMaxLength(64);
                entity.Property(simulationEvent => simulationEvent.Tick).HasColumnName("tick");
                entity.Property(simulationEvent => simulationEvent.Sequence).HasColumnName("sequence");
                entity.Property(simulationEvent => simulationEvent.Type).HasColumnName("type").HasMaxLength(128);
                entity.Property(simulationEvent => simulationEvent.SourceSystem).HasColumnName("source_system").HasMaxLength(200);
                entity.Property(simulationEvent => simulationEvent.Message).HasColumnName("message");
                entity.Property(simulationEvent => simulationEvent.EntityId).HasColumnName("entity_id").HasMaxLength(128);
                entity.Property(simulationEvent => simulationEvent.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
                entity.Property(simulationEvent => simulationEvent.PayloadBytes).HasColumnName("payload_bytes");
                entity.Property(simulationEvent => simulationEvent.CreatedAt).HasColumnName("created_at");
                entity.HasOne(simulationEvent => simulationEvent.Run)
                    .WithMany(run => run.Events)
                    .HasForeignKey(simulationEvent => simulationEvent.RunId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(simulationEvent => new { simulationEvent.RunId, simulationEvent.Tick });
                entity.HasIndex(simulationEvent => new { simulationEvent.RunId, simulationEvent.Tick, simulationEvent.Sequence }).IsUnique();
            });

            modelBuilder.Entity<SimulationTickSummaryEntity>(entity =>
            {
                entity.ToTable("simulation_tick_summaries");
                entity.HasKey(summary => summary.Id);
                entity.Property(summary => summary.Id).HasColumnName("id");
                entity.Property(summary => summary.RunId).HasColumnName("run_id").HasMaxLength(64);
                entity.Property(summary => summary.Tick).HasColumnName("tick");
                entity.Property(summary => summary.DurationMilliseconds).HasColumnName("duration_milliseconds");
                entity.Property(summary => summary.EventCount).HasColumnName("event_count");
                entity.Property(summary => summary.PopulationCount).HasColumnName("population_count");
                entity.Property(summary => summary.AliveAgentCount).HasColumnName("alive_agent_count");
                entity.Property(summary => summary.DeadAgentCount).HasColumnName("dead_agent_count");
                entity.Property(summary => summary.ResourceContainerCount).HasColumnName("resource_container_count");
                entity.Property(summary => summary.CreatedAt).HasColumnName("created_at");
                entity.HasOne(summary => summary.Run)
                    .WithMany(run => run.TickSummaries)
                    .HasForeignKey(summary => summary.RunId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(summary => new { summary.RunId, summary.Tick }).IsUnique();
            });

            modelBuilder.Entity<SimulationScenarioEntity>(entity =>
            {
                entity.ToTable("simulation_scenarios");
                entity.HasKey(scenario => scenario.Id);
                entity.Property(scenario => scenario.Id).HasColumnName("id").HasMaxLength(64);
                entity.Property(scenario => scenario.Name).HasColumnName("name").HasMaxLength(200);
                entity.Property(scenario => scenario.Version).HasColumnName("version");
                entity.Property(scenario => scenario.ScenarioJson).HasColumnName("scenario_json").HasColumnType("jsonb");
                entity.Property(scenario => scenario.IsValid).HasColumnName("is_valid");
                entity.Property(scenario => scenario.ValidationErrorsJson).HasColumnName("validation_errors_json").HasColumnType("jsonb");
                entity.Property(scenario => scenario.CreatedAt).HasColumnName("created_at");
                entity.Property(scenario => scenario.UpdatedAt).HasColumnName("updated_at");
                entity.HasIndex(scenario => scenario.UpdatedAt);
            });
        }
    }
}
