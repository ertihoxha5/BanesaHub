# BanesaHub product requirements

M00.5 draft based on the product specification v1.0, sections 01–04 and 07–16. Discovery remains open until 3–5 owners or managers are interviewed and at least two real users confirm a priority problem. Review priorities, personas and workflows against that evidence before closing M00.

## Product and audience

BanesaHub gives Kosovo property owners and managers one place to track inventory, leases, amounts due, recorded payments and repair requests. Finance and maintenance staff have scoped work areas. The tenant portal provides access to the tenant's own records and requests. See the [personas and prioritized stories](personas-and-user-stories.md) and [wireframes and workflows](mvp-wireframes.md). Use Albanian, EUR and local formats by default.

## Prioritized delivery backlog

| Requirements | Delivery | Acceptance |
| --- | --- | --- |
| FR-01 Identity and organizations | MVP / Must · M02 · US01, US08 | Registration, login, invitations, reset and lockout work. Membership and resource checks prevent cross-organization access. |
| FR-02 Properties | MVP / Must · M03 · US02 | Owners can create properties, buildings and uniquely numbered units. Incomplete setup can remain a draft. |
| FR-03–FR-04 Tenants and leases | MVP / Must · M04 · US03 | Contacts and occupancy history are traceable. Lease versions preserve changes and activation rejects overlapping active leases. |
| FR-12 Documents | MVP / Must, minimal · M04 · US11 | Validate uploads, retain metadata and authorize downloads. Advanced document workflows are V1. |
| FR-05 Monthly charges | MVP / Must · M05 · US04 | Generate charges with due dates from active leases. Repeating the same period creates no duplicates. |
| FR-06 Manual payments | MVP / Must · M06 · US05 | Partial allocations derive the balance. EUR 120 against EUR 300 leaves EUR 180. Reversal restores the prior balance and leaves an audit record. |
| FR-07 Maintenance | MVP / Must · M07 · US06 | A tenant submits a scoped request with optional photo. Staff can prioritize, assign and resolve it with comments and history. |
| FR-08 Notifications | MVP / Should, in-app · M07 · US09 | Relevant recipients see status changes and can mark notifications as read. Optional email is V1 / Could (US12). |
| FR-09 Dashboard | MVP / Must · M08 · US07 | Occupancy, overdue charges, recorded rent and open maintenance match validated organization records and selected filters. |
| FR-10 Audit and security | MVP / Must · M02–M09 · US08 | Critical actions have an audit trail. Tests verify resource authorization, including tenant and organization isolation. |
| FR-11 Shared expenses | V1 / Should · US10 | Preview the approved allocation basis, exclusions and amounts before posting individual charges. |

MVP excludes card processing, escrow, certified tax accounting, automatic legally binding contracts and automated tenant or financial decisions. Bank integrations, mobile apps and predictive analytics are later options. Live payments require explicit approval. Decide multi-tenant leases and deposit treatment before implementing leases, and agree due dates and first/last month treatment before billing.

## Definition of done

- The change meets its story and acceptance criteria. Server validation and resource authorization are enforced. The UI provides understandable Albanian feedback and applicable loading, empty, error and success states.
- The affected projects restore and build cleanly. Relevant unit and SQL Server integration tests pass, including failure paths for organization isolation or financial integrity where affected. Apply any migration to a clean test database.
- Review the diff for credentials and sensitive data, update relevant documentation and show a short demo with synthetic data. Record checkpoint evidence and prepare a scoped Conventional Commit for the owner.
- For the pilot, test with at least three organizations and pass at least 90% of critical acceptance scenarios. Verify isolation and backup recovery. Demonstrate the full property-to-payment cycle, with at least one owner completing it unassisted. Measure workflow time and validate the under-two-second page target under agreed conditions.
