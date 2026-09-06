using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InterviewPrep.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "domains",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_domains", x => x.id);
                    table.CheckConstraint("ck_domains_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    preferred_language = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.CheckConstraint("ck_users_preferred_language", "preferred_language IN ('en','uk')");
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    domain_id = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_topics", x => x.id);
                    table.CheckConstraint("ck_topics_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.ForeignKey(
                        name: "fk_topics_domains_domain_id",
                        column: x => x.domain_id,
                        principalTable: "domains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scope_type = table.Column<string>(type: "text", nullable: false),
                    scope_domain_slug = table.Column<string>(type: "text", nullable: true),
                    scope_topic_slug = table.Column<string>(type: "text", nullable: true),
                    answered_count = table.Column<int>(type: "integer", nullable: true),
                    correct_count = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => x.id);
                    table.CheckConstraint("ck_sessions_scope", "(scope_type = 'all' AND scope_domain_slug IS NULL AND scope_topic_slug IS NULL) OR (scope_type = 'domain' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NULL) OR (scope_type = 'topic' AND scope_domain_slug IS NOT NULL AND scope_topic_slug IS NOT NULL)");
                    table.CheckConstraint("ck_sessions_scope_type", "scope_type IN ('all','domain','topic')");
                    table.ForeignKey(
                        name: "fk_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "concepts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    topic_id = table.Column<int>(type: "integer", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concepts", x => x.id);
                    table.CheckConstraint("ck_concepts_level", "level IN ('M','S','L')");
                    table.CheckConstraint("ck_concepts_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                    table.ForeignKey(
                        name: "fk_concepts_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "concept_translations",
                columns: table => new
                {
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_translations", x => new { x.concept_id, x.language });
                    table.CheckConstraint("ck_concept_translations_language", "language IN ('en','uk')");
                    table.ForeignKey(
                        name: "fk_concept_translations_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "katas",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    time_limit_minutes = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_katas", x => x.id);
                    table.CheckConstraint("ck_katas_id", "id ~ '^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.kata\\.[0-9]+$'");
                    table.CheckConstraint("ck_katas_language", "language IN ('csharp','typescript','sql')");
                    table.CheckConstraint("ck_katas_time_limit", "time_limit_minutes BETWEEN 5 AND 120");
                    table.ForeignKey(
                        name: "fk_katas_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    deprecated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                    table.CheckConstraint("ck_questions_id", "id ~ '^[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.[a-z0-9]+(-[a-z0-9]+)*\\.(mcq|open)\\.[0-9]+$'");
                    table.CheckConstraint("ck_questions_level", "level IN ('M','S','L')");
                    table.CheckConstraint("ck_questions_type", "type IN ('mcq','open')");
                    table.ForeignKey(
                        name: "fk_questions_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kata_translations",
                columns: table => new
                {
                    kata_id = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    task = table.Column<string>(type: "text", nullable: false),
                    rubric = table.Column<string>(type: "jsonb", nullable: false),
                    reference_solution = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kata_translations", x => new { x.kata_id, x.language });
                    table.CheckConstraint("ck_kata_translations_language", "language IN ('en','uk')");
                    table.ForeignKey(
                        name: "fk_kata_translations_katas_kata_id",
                        column: x => x.kata_id,
                        principalTable: "katas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: true),
                    kata_id = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: false, computedColumnSql: "COALESCE(question_id, kata_id)", stored: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    answer = table.Column<string>(type: "jsonb", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: true),
                    score = table.Column<double>(type: "double precision", nullable: true),
                    grading = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempts", x => x.id);
                    table.CheckConstraint("ck_attempts_item", "(item_type = 'question' AND question_id IS NOT NULL AND kata_id IS NULL) OR (item_type = 'kata' AND kata_id IS NOT NULL AND question_id IS NULL)");
                    table.CheckConstraint("ck_attempts_item_type", "item_type IN ('question','kata')");
                    table.CheckConstraint("ck_attempts_score", "score IS NULL OR (score >= 0 AND score <= 1)");
                    table.ForeignKey(
                        name: "fk_attempts_katas_kata_id",
                        column: x => x.kata_id,
                        principalTable: "katas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attempts_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attempts_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_attempts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "question_translations",
                columns: table => new
                {
                    question_id = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    explanation = table.Column<string>(type: "text", nullable: false),
                    options = table.Column<string>(type: "jsonb", nullable: true),
                    rubric = table.Column<string>(type: "jsonb", nullable: true),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_translations", x => new { x.question_id, x.language });
                    table.CheckConstraint("ck_question_translations_language", "language IN ('en','uk')");
                    table.CheckConstraint("ck_question_translations_options_xor_rubric", "(options IS NULL) <> (rubric IS NULL)");
                    table.ForeignKey(
                        name: "fk_question_translations_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "review_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: true),
                    kata_id = table.Column<string>(type: "text", nullable: true),
                    item_id = table.Column<string>(type: "text", nullable: false, computedColumnSql: "COALESCE(question_id, kata_id)", stored: true),
                    algorithm = table.Column<string>(type: "text", nullable: false, defaultValue: "sm2"),
                    interval_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ease = table.Column<double>(type: "double precision", nullable: false, defaultValue: 2.5),
                    repetitions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    algorithm_state = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_states", x => x.id);
                    table.CheckConstraint("ck_review_states_item", "(item_type = 'question' AND question_id IS NOT NULL AND kata_id IS NULL) OR (item_type = 'kata' AND kata_id IS NOT NULL AND question_id IS NULL)");
                    table.CheckConstraint("ck_review_states_item_type", "item_type IN ('question','kata')");
                    table.ForeignKey(
                        name: "fk_review_states_katas_kata_id",
                        column: x => x.kata_id,
                        principalTable: "katas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_states_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_review_states_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attempts_kata_id",
                table: "attempts",
                column: "kata_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_question_id",
                table: "attempts",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_session_id",
                table: "attempts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_user_id_item_id_answered_at",
                table: "attempts",
                columns: new[] { "user_id", "item_id", "answered_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_concepts_topic_id_slug",
                table: "concepts",
                columns: new[] { "topic_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domains_slug",
                table: "domains",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_domains_sort_order",
                table: "domains",
                column: "sort_order");

            migrationBuilder.CreateIndex(
                name: "ix_katas_concept_id",
                table: "katas",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_questions_concept_id",
                table: "questions",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_states_kata_id",
                table: "review_states",
                column: "kata_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_states_question_id",
                table: "review_states",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_states_user_id_due_at",
                table: "review_states",
                columns: new[] { "user_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_review_states_user_id_item_id",
                table: "review_states",
                columns: new[] { "user_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id_started_at",
                table: "sessions",
                columns: new[] { "user_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_topics_domain_id_slug",
                table: "topics",
                columns: new[] { "domain_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempts");

            migrationBuilder.DropTable(
                name: "concept_translations");

            migrationBuilder.DropTable(
                name: "kata_translations");

            migrationBuilder.DropTable(
                name: "question_translations");

            migrationBuilder.DropTable(
                name: "review_states");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropTable(
                name: "katas");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "concepts");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "domains");
        }
    }
}
