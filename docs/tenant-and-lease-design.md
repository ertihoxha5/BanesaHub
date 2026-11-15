# Tenant and lease design

M04.1 implements tenant contacts and occupancy history for FR-03 and establishes the data boundary for FR-04.

- A tenant contact belongs to one organization. Name is required. Email and phone are optional and are not identity keys. Portal access will require an explicit account association and a resource authorization check.
- An occupancy period joins a tenant and unit within that organization. Start and end are local calendar dates. The end date is inclusive, and a missing end date means no departure has been recorded. A new period can start the day after the preceding period ends.
- Follow the PDF's one-primary-tenant proposal for the MVP. Keep occupancy dates separate from contractual dates so termination and actual departure can differ. Preserve prior occupancy as separate rows.
- SQL Server checks the date order, prevents identical start dates for the same unit and prevents cross-organization links. The lease lifecycle checkpoint will enforce full overlap prevention in a transaction. This schema does not activate leases or write occupancy periods by itself.
- Keep deposits separate from rental income and omit automatic proration in the MVP, as proposed in the PDF. First and last month handling, billing due dates and correction rules remain decisions for the billing checkpoints.

## Lease records and versions

A lease identifies one unit and one primary tenant within an organization. Its lifecycle state is draft, active, ended or cancelled. Each version keeps a numbered snapshot of the contractual dates, effective date, monthly rent and optional deposit in EUR, with a UTC recording timestamp. Amendments will append versions so prior terms remain available.

Owners and property managers can create a draft with version 1 using POST /api/v1/organizations/{organizationId}/leases. POST /api/v1/organizations/{organizationId}/leases/{leaseId}/activate checks the latest terms against active and ended leases for the same unit, using the actual end date for ended leases. Both endpoints require the existing session and antiforgery token.

Activation locks the unit first in a SQL Server serializable transaction. The check treats both endpoints of a contract as inclusive and a missing end date as open-ended. An overlap returns HTTP 409. Repeating activation for an already active lease succeeds without writing again. Amendment and ending use the same unit lock.

Lease activation records a contractual state change. Actual occupancy dates and automatic expiration are separate lifecycle work.

POST /api/v1/organizations/{organizationId}/leases/{leaseId}/amend-rent adds a new rent version for an active lease. The effective date must be later than the latest version's effective date and within the contract. The expected version protects against overwriting a concurrent amendment. This operation retains the original term dates and deposit.

POST /api/v1/organizations/{organizationId}/leases/{leaseId}/end records the inclusive actual end date. The date must fall inside the contract and cannot be later than today in Kosovo. Repeating the same end request succeeds. Prior versions remain available, and future billing must disregard terms that take effect after the actual end date. Replacement leases must start after that date to avoid overlapping the recorded contractual history.

## Tenant contact API

Owners and property managers can list tenant contacts at GET /api/v1/organizations/{organizationId}/tenants. Use search for a name substring and offset and limit for pagination. POST to the collection creates a contact. GET, PUT and DELETE on /tenants/{id} read, replace or archive it. The API validates the name and optional email and phone length, and requires an antiforgery token for writes.

Archived contacts disappear from the contact API and cannot be selected for a new draft or activated draft. Existing leases and occupancy history retain their links to the archived contact. Archiving a contact does not end a lease.

The Albanian MVC directory is available from the organization's property list under Qiramarrësit. Staff can search by name, open a contact profile and create, edit or archive a contact. The browser forms and the server-to-server API writes each use antiforgery protection.
