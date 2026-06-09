using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TabulaRasa.Api.Persistence.Migrations
{
    public partial class InitialSimulationPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "simulation_runs",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_tick = table.Column<long>(type: "bigint", nullable: false),
                    agent_count = table.Column<int>(type: "integer", nullable: false),
                    alive_agent_count = table.Column<int>(type: "integer", nullable: false),
                    dead_agent_count = table.Column<int>(type: "integer", nullable: false),
                    food_count = table.Column<int>(type: "integer", nullable: false),
                    grid_width = table.Column<int>(type: "integer", nullable: false),
                    grid_height = table.Column<int>(type: "integer", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_simulation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_tick = table.Column<long>(type: "bigint", nullable: true),
                    storage_bytes = table.Column<long>(type: "bigint", nullable: false),
                    checkpoint_bytes = table.Column<long>(type: "bigint", nullable: false),
                    event_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulation_scenarios",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    scenario_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_valid = table.Column<bool>(type: "boolean", nullable: false),
                    validation_errors_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulation_checkpoints",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tick = table.Column<long>(type: "bigint", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    compressed_payload = table.Column<byte[]>(type: "bytea", nullable: true),
                    payload_bytes = table.Column<long>(type: "bigint", nullable: false),
                    is_compressed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_checkpoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_simulation_checkpoints_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tick = table.Column<long>(type: "bigint", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_system = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    payload_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_simulation_events_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulation_tick_summaries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tick = table.Column<long>(type: "bigint", nullable: false),
                    duration_milliseconds = table.Column<double>(type: "double precision", nullable: false),
                    event_count = table.Column<int>(type: "integer", nullable: false),
                    population_count = table.Column<int>(type: "integer", nullable: false),
                    alive_agent_count = table.Column<int>(type: "integer", nullable: false),
                    dead_agent_count = table.Column<int>(type: "integer", nullable: false),
                    resource_container_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulation_tick_summaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_simulation_tick_summaries_simulation_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "simulation_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_simulation_runs_updated_at",
                table: "simulation_runs",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_simulation_scenarios_updated_at",
                table: "simulation_scenarios",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "ix_simulation_checkpoints_run_id_tick",
                table: "simulation_checkpoints",
                columns: ["run_id", "tick"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_simulation_events_run_id_tick",
                table: "simulation_events",
                columns: ["run_id", "tick"]);

            migrationBuilder.CreateIndex(
                name: "ix_simulation_events_run_id_tick_sequence",
                table: "simulation_events",
                columns: ["run_id", "tick", "sequence"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_simulation_tick_summaries_run_id_tick",
                table: "simulation_tick_summaries",
                columns: ["run_id", "tick"],
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "simulation_checkpoints");
            migrationBuilder.DropTable(name: "simulation_events");
            migrationBuilder.DropTable(name: "simulation_scenarios");
            migrationBuilder.DropTable(name: "simulation_tick_summaries");
            migrationBuilder.DropTable(name: "simulation_runs");
        }
    }
}
