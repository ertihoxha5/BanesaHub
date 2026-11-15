# Tenant and lease design

M04.1 implements tenant contacts and occupancy history for FR-03 and establishes the data boundary for FR-04.

- A tenant contact belongs to one organization. Name is required. Email and phone are optional and are not identity keys. Portal access will require an explicit account association and a resource authorization check.
- An occupancy period joins a tenant and unit within that organization. Start and end are local calendar dates. The end date is inclusive, and a missing end date means no departure has been recorded. A new period can start the day after the preceding period ends.
- Follow the PDF's one-primary-tenant proposal for the MVP. Keep occupancy dates separate from contractual dates so termination and actual departure can differ. Preserve prior occupancy as separate rows.
- SQL Server checks the date order, prevents identical start dates for the same unit and prevents cross-organization links. The lease lifecycle checkpoint will enforce full overlap prevention in a transaction. This schema does not activate leases or write occupancy periods by itself.
- Keep deposits separate from rental income and omit automatic proration in the MVP, as proposed in the PDF. First and last month handling, billing due dates and correction rules remain decisions for the billing checkpoints.

## Lease records and versions

A lease identifies one unit and one primary tenant within an organization. Its lifecycle state is draft, active, ended or cancelled. Each version keeps a numbered snapshot of the contractual dates, effective date, monthly rent and optional deposit in EUR, with a UTC recording timestamp. Amendments will append versions so prior terms remain available.

The lease activation service will select the applicable terms and check for overlapping active leases in a transaction. Expiration, early termination and recording actual occupancy dates belong to the lifecycle workflow.
