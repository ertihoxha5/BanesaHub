# BanesaHub personas and user stories

Draft based on the product specification v1.0, sections 01–04 and 12. The profiles and priorities still need interview validation under M00.2. US01–US10 follow the PDF backlog. US11–US12 cover two additional documented requirements. Priorities apply within the listed release.

## Personas

- Individual landlord (Organization Owner). Typically manages 2–20 units. Needs to see occupancy, rent due, payments recorded and open maintenance requests.
- Property manager. Manages several properties on behalf of an organization. Needs to maintain inventory and lease history and coordinate repairs within their assigned permissions.
- Finance staff. Records manual payments and reconciles balances. Needs accurate allocation, reversals and organization reports.
- Maintenance staff. Handles assigned requests. Needs the priority, work history and allowed status changes for each ticket.
- Tenant. Needs to see their own contract, charges, recorded payments and repair updates.
- Building administrator. Needs to preview and explain shared expense allocations before posting them in V1.

## Prioritized stories

| ID | Release / priority | Story | Acceptance |
| --- | --- | --- | --- |
| US01 | MVP / Must | As an owner, I want to create an organization so I can manage my portfolio. | The organization is created and the creator receives its Owner membership. |
| US02 | MVP / Must | As a manager, I want to register residential units so I can manage occupancy. | Required fields are validated and duplicate unit numbers within a building are rejected. |
| US03 | MVP / Must | As a manager, I want to activate a lease so rent can be billed. | Activation rejects overlapping active leases for the same unit. |
| US04 | MVP / Must | As finance staff, I want to filter charges so I can find unpaid rent. | Month and status filters return only charges I am authorized to view. |
| US05 | MVP / Must | As finance staff, I want to allocate a payment so balances reflect receipts. | Posting EUR 120 against EUR 300 leaves EUR 180 due. A failed allocation leaves no partial posting. |
| US06 | MVP / Must | As a tenant, I want to report a maintenance issue for my unit. | The ticket is tied to my eligible unit and the request fails for units I cannot access. |
| US07 | MVP / Must | As an owner, I want a dashboard so I can see what needs attention. | Occupancy, overdue charges, recorded rent and open tickets match the organization's records and selected filters. |
| US08 | MVP / Must | As an organization member, I want my organization's records protected. | Attempts to read or edit a resource in another organization return 403 or 404 and leave the resource unchanged. |
| US09 | MVP / Should | As a manager, I want in-app updates so I can follow relevant activity. | The notification is visible to its recipient and can be marked as read. |
| US10 | V1 / Should | As a building administrator, I want to preview a shared expense allocation. | The preview shows the approved basis, exclusions and amounts before confirmation creates individual charges. |
| US11 | MVP / Must | As a manager, I want to attach a lease document so authorized users can retrieve it. | The upload is validated, the file is stored privately and a user without access to the lease cannot download it. |
| US12 | V1 / Could | As a user, I want optional email notifications for relevant updates. | Email respects preferences and records delivery attempts and failures separately from in-app notification state. |
