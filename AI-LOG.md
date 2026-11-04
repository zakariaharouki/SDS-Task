# AI log

## Tools
| Tool | Used for | Why this one |
|---|---|---|
Claude Code | developing project skeleton based on my design.md and my rules. | The tool the I am used to.
Claude Code | reviewing my controller and service code, then writing validators and missing logic one approved step at a time. | It can build and run the code, so review findings come with test evidence.
graphify | giving Claude Code a graph of the code to navigate instead of reading whole files. | To reduce token usage.

## Entries
Every entry from 2026-11-03 onwards comes from one Claude Code session. Session export: link to add.

### 2026-11-02 — Design doc (Claude)
- Goal: draft design.md from the brief + my decisions
- Prompt: https://claude.ai/share/8d58a74c-8473-4f72-bd64-16d2a4385914
- Kept: concurrency/version field.
- Changed/rejected: Show "n/a" instead of 0% when a scheme has no homes,Block duplicates when the name and the address match,Make a scheme have at least 1 home once it’s past Appraisal.
- Verified by: Zakaria

### 2026-11-03 — Solution scaffold (Claude Code)
- Goal: set up the empty solution, the Scheme model with MongoDB mapping, config and .gitignore, with no features.
- Prompt: "Set up the empty solution only, no features yet" plus the list of projects, models and config.
- Kept: the plan-first approach from my CLAUDE.md rules. Its suggestions to include `totals` and `duplicateKey` as plain fields with no logic, and to add a mapping test that needs no database. Explicit camelCase Mongo field names, which it added because the driver would otherwise not match the stored shape in design 2.6.
- Changed/rejected: Claude pointed out that my folder request differed from design sections 5 and 6, and I chose my request (`backend/`, `frontend/`, two projects). I added a requirement it hadn't planned: a `.env` file that must never reach GitHub. I rejected its reading of "repository layer" (see Rejected / corrected output).
- Verified by: Zakaria approved the plan. Build, 6 mapping tests and the frontend build passed. `git check-ignore` confirmed `.env` is ignored. Commit 2adf8f2.

### 2026-11-03 — Layers, pre-push hook and CI (Claude Code)
- Goal: scaffold DTOs, mapping, empty controllers, services and interfaces, register services in Program.cs, block `.env` from being pushed, and prepare for MongoDB as a GitHub secret.
- Prompt: "Yes I meant repository layer by creating DTOs, Mapping, Controllers (Empty I will fill in the logic later), Services and Interfaces."
- Kept: working on a branch instead of main. A hook kept in `.githooks` with `core.hooksPath`, so it's versioned with the repo instead of living only in `.git/hooks`. Failing at startup when Mongo settings are missing, which it added. Passing the affordable percentage into the mapping, so the formula stays out of it.
- Changed/rejected: my request replaced design decisions from the first draft: "no service layer" and separate Domain and Infrastructure projects. Claude warned that the hook can still be bypassed with `git push --no-verify` and is per clone, and I accepted that.
- Verified by: real pushes to a throwaway local remote. `.env`, `frontend/.env`, `.env.production` and a `.env` added then deleted were all blocked, and `.env.example` was allowed. 11 tests passed. Commit 11bfa58.

### 2026-11-03 — CI credentials and design update (Claude Code)
- Goal: decide how MongoDB credentials are handled in CI, and bring design.md in line with my scope change.
- Prompt: "Remove it from CI I don't want credentials to be passed." and "Yes Update the design.md because the scope was changed by me."
- Kept: its recommendation to remove the secret from CI. Its suggestion that `SchemeService` recalculates totals and `duplicateKey` before each save, instead of the repository, and I recorded the trade-off in design.md.
- Changed/rejected: I refused its offer to commit at this point ("No do not Commit I need to check") and reviewed the work first. I rejected passing the secret to CI.
- Verified by: Zakaria reviewed, then approved commit and push. GitHub Actions passed both jobs with no credentials. Only `.env.example` is on the remote branch.

### 2026-11-03 — Review of my controller and service (Claude Code)
- Goal: check my controller and service changes before deploying, reporting missing validation and logic without changing anything until I approved.
- Prompt: "If there is a validation missing let me know before you do any change. If there is a logic that I missed out do the same and wait for my approval."
- Kept: all the findings (see AI code review findings) and these recommendations: separate `SchemeCalculator`, rounding midpoints away from zero, and strict query `status`. Claude writes the validators and missing logic while I validate each step.
- Changed/rejected: design section 3 contradicted itself on `id` and `createdAt`, and I decided to reject them if sent. I declined to install Docker for now and chose to rely on CI for Mongo integration tests.
- Verified by: Claude tested my code in a scratch copy with an in-memory repository, so real MongoDB behaviour is still untested. I made each decision.

### 2026-11-03 — Graphify (Claude Code)
- Goal: reduce token usage when Claude Code reads the project.
- Prompt: "use graphify skill from now on to read through my project and reduce usage of tokens"
- Kept: installing it outside the repo, and excluding `graphify-out/` through `.git/info/exclude` so it can't be committed.
- Changed/rejected: nothing. Claude noted that the graph shows structure, not behaviour, so reviews still need exact lines and test runs.
- Verified by: the graph built locally from code only, with no LLM or API key, and `git status` stayed clean.

### 2026-11-04 — Commit my code and move the formulas (Claude Code)
- Goal: commit my controller and service work, then move my formulas into `SchemeCalculator`.
- Prompt: "Move my formulas and add midpoint rounding"
- Kept: `MidpointRounding.AwayFromZero`. Correcting the total value example in design.md.
- Changed/rejected: my CLAUDE.md says Claude must not write `SchemeCalculator`. My earlier reply was ambiguous, so it asked instead of assuming, and I gave a one-off exception for moving the formulas. Its tests remain mine.
- Verified by: a line-by-line comparison showed the rounding as the only difference. The API comparison showed one change (1 of 400 homes went from 0.2 to 0.3). Zakaria approved. Commits e37fdd7 and 44794db.

### 2026-11-04 — Step 1: request parsing (Claude Code)
- Goal: reject invalid request bodies with friendly messages and camelCase field paths.
- Prompt: "Reject them if sent." and "Yes as you suggested", from my answers on the review decisions.
- Kept: a strict status converter, rejecting unknown fields, and one place that formats parsing errors.
- Changed/rejected: nothing. I accepted two limits Claude reported: parsing stops at the first error, and detecting missing and unknown fields depends on .NET's message wording, which the tests guard.
- Verified by: 50 tests passed. Removing each behaviour made tests fail (5, 10 and 34). Zakaria approved. Commit 782e337.

### 2026-11-04 — Step 2: validators (Claude Code)
- Goal: apply every design section 3 field rule with FluentValidation on create and update.
- Prompt: "I want you to write the missing logic and validators while I validate each step"
- Kept: one validator shared by create and update, validation running inside the service, and an exception filter producing the same 400 format.
- Changed/rejected: Claude's first version used an API removed from FluentValidation 12 (see Rejected / corrected output). I accepted two behaviours it flagged: an invalid body on an unknown id returns 400 rather than 404, and negative homes can show two errors.
- Verified by: 100 tests passed. Removing each behaviour made tests fail (9, 9 and 2). Zakaria approved. Commit 51242ed.

### 2026-11-04 — Step 3: query parameters (Claude Code)
- Goal: reject invalid `status`, `sortBy` and `sortDir` values, and make "no filter" mean the same for the list and the summary.
- Prompt: "Yes as you suggested", my answer on making the query `status` as strict as the body.
- Kept: a strict status binder, sharing one exact-match check with the JSON converter, sort rules validated in the service, and "no filter" always reaching the repository as `null`.
- Changed/rejected: nothing. Claude flagged that unknown query parameters are ignored and parameter names aren't case-sensitive, and I kept that as it recommended.
- Verified by: 140 tests passed. Removing each behaviour made tests fail (16, 9, 1 and 2). Zakaria approved. Commit d7c0e8c.

### 2026-11-04 — Step 4: duplicate key (Claude Code)
- Goal: set `duplicateKey` on every create and update, using the normalising rules in design section 3.
- Prompt: "Concerning the duplicatekey also I want it to be handled in my service."
- Kept: `SchemeService` sets the key on every save, as I decided. The formula is in a small pure class so it can be unit tested.
- Changed/rejected: Claude found that a `|` in a name or address can create a false duplicate. I took its recommendation to document that as a known limit rather than add escaping.
- Verified by: 156 tests passed. Removing each behaviour made tests fail (3, 1, 1, 7, 5 and 2). Duplicates aren't blocked until the unique index and the 409 exist. Zakaria approved. Commit e11e458.

### 2026-11-04 — Step 5: conflicts and errors (Claude Code)
- Goal: return separate 409s for a stale version and a duplicate scheme, and a generic 500 with no internal details outside Development.
- Prompt: "I want you to write the missing logic and validators while I validate each step"
- Kept: the repository reports duplicates and the service reports stale versions, with one filter producing both 409s, each with its own title.
- Changed/rejected: nothing. I accepted that the stale version check assumes nothing gets deleted, which holds because delete is out of scope.
- Verified by: 172 tests passed using a fake repository. Removing each behaviour made tests fail (5, 2, 4 and 3). The real Mongo duplicate error is handled in step 6. Zakaria approved. Commit f693fdb.

### 2026-11-04 — Step 6: repository (Claude Code)
- Goal: implement the MongoDB repository and indexes, and test them against real MongoDB in CI.
- Prompt: "rely on CI because docker will need sometime to be installed in my environment" and "pushing 6 is ok"
- Kept: pipeline-order status sorting with stable tie-breakers, atomic version checks, translating Mongo's duplicate error, and one status order list shared with the summary.
- Changed/rejected: the tests use MongoDB 7.0 instead of 8.0, because 8.0 and newer refuse to start on the Linux kernel CI runners use. Claude's first fix for that was wrong (see Rejected / corrected output).
- Verified by: GitHub Actions run 41737747279 passed with 197 tests and none skipped, including 25 against real MongoDB. Commits 4d8fcc1, 2b541f5 and 5255080. Zakaria approved, including testing on MongoDB 7.0.

### 2026-11-04 — Step 7: endpoint tests (Claude Code)
- Goal: send HTTP requests through the whole API to real MongoDB, covering every endpoint and the design section 7.2 cases.
- Prompt: "A and step 6 is approved", choosing endpoint tests before deployment setup.
- Kept: one test host running the real API against a throwaway database, never Atlas. `SchemeCalculator` tests stay mine.
- Changed/rejected: the tests found one real bug (see AI code review findings), and I approved the fix Claude recommended. I took its advice to delete the redundant `MONGODB_USERNAME` and `MONGODB_PASSWORD` secrets, since the connection string is enough. I rejected committing my `UserSecretsId`, because I don't want it shared.
- Verified by: GitHub Actions run 41804539402 ran 218 tests and 1 failed on that bug. After the fix, run 41807014655 passed all 219 with none skipped. Zakaria approved. Commits c5f5beb and 9eff229.

### 2026-11-04 — Deployment setup: health, CORS, hosting and Docker (Claude Code)
- Goal: prepare the API to run on Render, without deploying anything yet.
- Prompt: "I approve option B but once you prepare the docker for render notify me"
- Kept: its recommendations that an empty CORS list still lets the API start, that a `*` is ignored, that HTTPS redirection runs only in Development because Render handles HTTPS, and that the seed script and `docker-compose` stay out of this plan. A CI job that builds the image and runs it the way Render would.
- Changed/rejected: nothing yet.
- Verified by: tests for each behaviour, each shown to fail with that behaviour removed. CI run to be recorded. Awaiting my review.

## Refined prompt
- v1: "I want you to use repository Layer Architecture."
- Final: "Yes I meant repository layer by creating DTOs, Mapping, Controllers (Empty I will fill in the logic later), Services and Interfaces. I also want you to add all Services to program.cs as usual."
- What the difference made: v1 was too vague, so Claude produced only one repository interface and asked whether that was what I meant. The final version named each layer and said controllers stay empty, which gave me the full structure and left the logic for me to write.

## Rejected / corrected output
- **Repository layer:** Claude read it as a single `ISchemeRepository` interface. That was too little for what I wanted. I caught it because it asked me to confirm, and I gave the full list of layers.
- **Secret passed to CI:** Claude's workflow gave the MongoDB secret to the test step, although it reported that nothing used it and recommended removing it later. Real credentials shouldn't reach test runs, so I had it removed straight away.
- **Status converter:** Claude's built-in `JsonStringEnumConverter` accepted `99`, `1` and `"planning"`, but design section 3 requires the exact string. Claude flagged it and the review tests confirmed it. It was replaced with a strict converter.
- **"11 tests passed" during the review:** my running API had locked the build output, so the build failed and that result came from old binaries, not my new code. Claude caught this in the build output and re-ran everything in a scratch copy.
- **Wrong total in design.md:** the examples showed £37,400,000, but the mix adds up to £36,800,000. A throwaway check failed against the example, and working it out by hand showed that the document was wrong, not the code.
- **FluentValidation `Transform`:** Claude wrote the validator with an API removed in version 12. The build failed, and it rewrote those rules to check the trimmed value directly.
- **Pinning CI to Ubuntu 24.04:** when MongoDB 8.0 wouldn't start in CI, Claude assumed Ubuntu 24.04 had an older Linux kernel. It didn't: the run printed `7.0.0`, which is still in the blocked range, and all 25 tests failed again. Claude had printed the kernel version to check that assumption, so the next run showed it was wrong. It undid the pin and moved the tests to MongoDB 7.0.
- **`id` and `createdAt` in design section 3:** the table said ignore them, while another rule said reject unknown properties. Claude found the contradiction and I decided to reject them.

## Where I didn't use AI (and why)
- **Controller actions and `SchemeService` logic:** I wrote these myself, including the totals and percentage formulas, because I wanted to own that logic (my CLAUDE.md rule). Claude only reviewed them, and later moved the formulas and added validation calls with my approval.
- **`SchemeCalculator` tests:** these are reserved for me under the same rule, and I haven't written them yet.
- **Decisions:** the scope changes, what to accept or reject, and every commit and push approval were mine.

## AI code review findings
| Finding | Fixed / Disagreed | Why |
|---|---|---|
| Blank names and addresses, and over-long ones, were accepted | Fixed (step 2) | Design section 3 requires 1–200 and 1–300 characters after trimming |
| Status `99`, `1` and `"planning"` were accepted in request bodies | Fixed (step 1) | Design section 3 requires the exact string |
| Out-of-range homes and average values, and 3 decimal places, were accepted | Fixed (step 2) | Design section 3 limits; also prevented overflowing totals |
| Average value of 0 was accepted when homes was above 0 | Fixed (step 2) | Design section 3 cross-field rule |
| 0 homes was accepted past Appraisal | Fixed (step 2) | My rule: at least 1 home once past Appraisal |
| Unknown JSON properties, `id` and `createdAt` were accepted | Fixed (step 1) | I decided to reject them, which resolved a contradiction in design section 3 |
| Parsing errors exposed raw framework messages and `Name`-style keys | Fixed (step 1) | The frontend maps errors to fields by camelCase path |
| Formulas were private in `SchemeService`, so they couldn't be unit tested directly | Fixed | Moved to `SchemeCalculator`, as design section 2.5 planned |
| Banker's rounding gave 0.2 for 0.25% | Fixed | I chose midpoints away from zero, the result most people expect |
| Total value example in design.md was wrong | Fixed | Corrected to £36,800,000 |
| Invalid `sortBy` and `sortDir` were accepted, and query `status` accepted `planning` and `1` | Fixed (step 3) | Design section 4.1 requires 400 |
| Summary passed an empty array with no filter, while the list passed `null` | Fixed (step 3) | An empty Mongo `$in` filter could have made the summary show zeros |
| `duplicateKey` was never set | Fixed (step 4) | Once the unique index exists, the second scheme saved would have been rejected |
| A `\|` in a name or address can create a false duplicate key | Disagreed | Too rare to justify escaping, so it's documented as a known limit in design section 3 |
| A stale version returned 404 instead of 409 | Fixed (step 5) | Design section 5.4 requires 409 |
| Duplicate schemes had no 409 | Fixed (step 5), real Mongo error handled in step 6 | Design section 3 requires 409 |
| Repository methods were not implemented, and indexes didn't exist | Fixed (step 6) | Every real request that reached the repository failed with a 500 |
| Malformed ids must return 404 | Fixed (step 6) | Design section 4.2 |
| Not every endpoint had a test through to real MongoDB | Fixed (step 7) | My CLAUDE.md rule requires at least one test per endpoint |
| `POST` returned timestamps with more precision than MongoDB stores, so a following `GET` didn't match | Fixed (step 7) | Found by the step 7 tests. I approved setting timestamps to whole milliseconds, instead of re-reading after every insert |
| Duplicate `using` in `SchemeService` and unused `using` in `SchemesController` | Open | Small cleanups I haven't decided on |
| `/health`, CORS, `Dockerfile`, `$PORT`, Swagger outside Development were missing | Fixed (deployment setup) | Needed before deploying to Render |
| `frontend/vercel.json` and the frontend itself don't exist yet (stock page only) | Open | Needed before deploying to Vercel |
