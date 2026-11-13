# BanesaHub repository guidance

- Use docs/product-requirements.md and docs/milestones.md to find scope and the current checkpoint. Implement only the checkpoint the owner has approved. Record validation and scoped commit commands in docs/development-log.md. The owner stages, commits and pushes unless they explicitly authorize those actions.
- Target .NET 10. Keep Domain independent, Application dependent on Domain, and Infrastructure dependent on Application and Domain. API composes Application and Infrastructure. Web communicates with API via HTTP.
- Use English for code and routes and Albanian for interface text. Store timestamps in UTC and use decimal for money.
- Enforce organization scope and resource authorization for business records. Keep credentials and real participant or tenant data out of Git.
- Run relevant restore, build and tests. Inspect the diff and report checks that could not run.
