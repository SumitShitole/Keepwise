# ADR-002 Backend architecture

- **Context:** ASP.NET Core 10 REST API with several modules.
- **Options:** Microservices; classic Clean Architecture layers; modular monolith.
- **Decision:** Modular monolith with Domain / Application / Infrastructure / Api. Vertical slices inside modules.
- **Reason:** One team, one deployable, reminder jobs colocated with the API. Microservices unjustified.
- **Trade-off:** Must keep module boundaries disciplined as the team grows.
