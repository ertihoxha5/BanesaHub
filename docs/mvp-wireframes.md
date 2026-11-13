# BanesaHub MVP wireframes and workflows

Draft from specification v1.0, sections 04, 07 and 09. Related views share a sketch so eight sketches cover all nine screen groups. Interview validation is pending.

Authenticated staff views share role-appropriate navigation and an organization and account header. On mobile, collapse navigation and stack filters above results. Use Albanian labels, EUR, local date formatting, visible focus and text status labels. Include loading, empty, error and success states where applicable.

### S1 — Hyrja dhe ftesat

    [Email] [Fjalëkalimi] [Hyr]
    [Harrova fjalëkalimin] [Krijo organizatë]
    Ftesë | Organizata | Roli | [Prano ftesën]

Show an expired-invitation error with guidance to request a new invitation.

### S2 — Përmbledhje

    [Prona ▼] [Muaji ▼]
    [Njësi të zëna] [Detyrime të vonuara]
    [Qira e arkëtuar] [Kërkesa të hapura]

Owners and authorized managers see scoped metrics. Exclude deposits from rent. An empty portfolio points to property setup.

### S3 — Pronat dhe njësitë

    [Kërko] [Gjendja ▼] [Shto pronë]
    Prona > Ndërtesa > Njësia A-03
    Nr. | Kati | Sipërfaqja | Gjendja | [Hap]
    [Kontrata] [Historiku] [Mirëmbajtja]
    [Shto ndërtesë] [Shto njësi] [Ruaj si draft]

Allow incomplete property setup as a draft. The unit detail links to the current lease, history and maintenance.

### S4 — Qiramarrësit dhe kontratat

    [Kërko] [Shto qiramarrës]
    Profili | Kontakti | Historiku | Dokumentet
    [Njësia ▼] [Fillimi] [Fundi] [Qiraja] [Depozita]
    [Ngarko dokument] [Ruaj si draft] [Aktivizo]

Show date conflicts at activation. Only authorized users can retrieve lease documents.

### S5 — Detyrimet dhe pagesat

    [Muaji ▼] [Gjendja ▼]
    Njësia | Afati | Shuma | Paguar | Mbetur
    [Regjistro pagesë] [Shiko historikun]
    [Data] [Metoda] [Referenca] [Shuma]
    Detyrimi | Mbetur | Shuma për alokim
    [Konfirmo regjistrimin]

Show allocation and balance before posting. History distinguishes recorded and reversed payments. Reversal requires permission and confirmation.

### S6 — Mirëmbajtja

    [Gjendja ▼] [Prioriteti ▼]
    Kërkesa | Njësia | Prioriteti | Përgjegjësi
    [Cakto] [Ndrysho gjendjen]
    [Përshkrimi] [Fotot] [Komentet] [Historiku]

Managers see scoped requests. Maintenance staff see assigned work and the available transitions.

### S7 — Portali i qiramarrësit

    [Kontrata ime] [Detyrimet] [Pagesat] [Njoftimet]
    Kërkesat e mia | Gjendja | [Hap]
    [Njësia ▼] [Kategoria] [Përshkrimi] [Foto]
    [Dërgo kërkesën]

Show only the current user's records and eligible units. Resolved requests offer confirmation or reopening where allowed.

### S8 — Cilësimet

    [Organizata] [Anëtarët] [Njoftimet]
    Anëtari | Roli | Gjendja
    [Email] [Roli ▼] [Dërgo ftesë]
    Preferencat personale | [Ruaj]

Owners manage memberships and invitations. Users manage their own preferences. Optional email follows release scope.

## Workflow sketches

### W1 — Property setup (S1 → S3 → S8)

Owner creates organization → adds property, building and unit → invites manager → activates management. Incomplete property setup can be saved as a draft. Duplicate unit numbers within the same building prompt correction. The manager gains access after accepting an eligible invitation.

### W2 — Lease and monthly billing (S3 → S4 → S5 → S7)

Manager selects available unit → creates tenant profile → saves lease draft → activates lease → monthly generation creates charge → tenant sees amount and due date. Overlapping leases prevent activation. Repeating generation for the same lease, period and charge type preserves the existing charge. Due dates and proration follow the agreed billing policy.

### W3 — Partial payment and correction (S5 → S7)

Finance opens a EUR 300 charge → allocates and posts EUR 120 → sees EUR 180 remaining and Pjesërisht e paguar → authorized staff confirm reversal if needed → balance returns to EUR 300 if no other allocations exist. Reject excess allocation. A failed posting preserves the prior balance. History retains the original payment and reversal.

### W4 — Maintenance resolution (S7 → S6 → S7)

Tenant submits a request for an eligible unit with optional photo → manager sets priority and assigns staff → assignee records progress and resolution → tenant confirms closure or asks to reopen. Keep status history and notify relevant users in the app.
