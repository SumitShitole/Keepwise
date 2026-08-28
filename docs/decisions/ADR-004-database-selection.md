# ADR-004 Database selection

- **Context:** Relational data, EF Core 10, India-first cost.
- **Options:** SQL Server, MySQL, PostgreSQL.
- **Decision:** PostgreSQL 16 + Npgsql + EF Core 10.
- **Reason:** Cost, JSON/indexing, Cloud SQL Mumbai path, excellent EF support.
- **Trade-off:** Team must know Postgres, not SQL Server-specific tooling.
