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
- Verified by: GitHub Actions run 41809380943 passed 237 tests with none skipped. The image was built and run on a custom `$PORT` as a non-root user, with a real save (201) and a duplicate (409) against throwaway MongoDB. It hasn't run against Atlas yet. Commits a043157, 4ccae26, 6b6a5d1 and 64d49f1. I merged it (PR #3) and deployed the API to Render myself.

### 2026-11-04 — Vercel config (Claude Code)
- Goal: make deep links such as `/schemes/new` work once the frontend is on Vercel.
- Prompt: "I merged and deployed render. Do B5 and I will prepare the setup in vercel"
- Kept: one rewrite sending all routes to `index.html`, as design section 8 specifies. I'm setting up Vercel myself.
- Changed/rejected: nothing yet.
- Verified by: the frontend build passed. I merged it (PR #4) and deployed to Vercel myself. Claude then checked the live site: both `/` and `/schemes/new` return 200, so the rewrite works. The site still shows the stock page until the frontend is built.

### 2026-11-04 — Live API check (Claude Code)
- Goal: confirm the deployed API really saves to Atlas, not just to the throwaway MongoDB in CI.
- Prompt: "1. Create the test scheme"
- Kept: one clearly named test scheme created through the live API. `/health`, `/swagger`, the list and the summary all worked, and a duplicate returned 409.
- Changed/rejected: nothing. The test scheme is still in Atlas, and I need to delete it before reviewers see the app.
- Verified by: the live API's responses. This check also found that `POST` returned an `http://` `Location` header on the live site (see the next entries).

### 2026-11-04 — Review of my frontend types and components (Claude Code)
- Goal: check the types, `SchemesTable`, `SummaryCards`, `StatusBadge` and `schemes.css` I wrote by hand, and suggest minor edits without changing them.
- Prompt: "check the types and components I added test them and suggest any minor edits" and "Show me before you fix what the edit will be"
- Kept: Claude tested in a scratch copy against real API responses and changed nothing in my project until I answered.
- Changed/rejected: nothing rejected. I chose the recommended fixes (next entries), so the optional ones were not made: right-aligned number columns, moving the badge styles to CSS, and removing `App.css`.
- Verified by: my types matched the live API exactly. 29 throwaway tests passed, and each of 5 deliberate faults made one fail. A real browser in dark mode showed the invisible caption.

### 2026-11-04 — `Location` header fix (Claude Code)
- Goal: make `POST` return an `https://` `Location` on Render, which handles HTTPS before requests reach the app.
- Prompt: "apply the changes for the location fix", after Claude showed me the exact edit first, as I'd asked.
- Kept: reading only `X-Forwarded-Proto`, on its own branch and in its own commit (a050a25).
- Changed/rejected: nothing. It accepts the header from any proxy, because Render's addresses aren't fixed, and it only affects the scheme of generated links.
- Verified by: GitHub Actions run 41836831761 passed 239 tests with none skipped. Without `UseForwardedHeaders`, the new https test fails. I merged it (PR #5). The live header still needs one successful `POST` to confirm, which I'll check during my manual tests.

### 2026-11-04 — Frontend edits and test tools (Claude Code)
- Goal: apply the recommended review fixes and add Vitest and React Testing Library, as design section 7.3 expects.
- Prompt: "do all recommended fixes to the frontend" and "yes" to adding the test tools.
- Kept: the dark-mode caption fix, shared formatting in `lib/format.ts`, the empty message from design section 6, Edit button labels that include the address, the "Affordable %" heading, and types for requests and Problem Details. `npm test` added to CI.
- Changed/rejected: the throwaway tests became 28 project tests instead of 29. Claude dropped the checks of my types against a saved API response, because a frozen copy can't notice the API changing, and the contrast checks with hard-coded colours. The badge contrast test now reads the colour the component actually renders. Claude asked whether `SummaryCards` should show the `byStatus` breakdown, and I decided to leave it out. Design section 6 asks for a shared `Percent` component, and I decided to keep `formatPercent` instead, since it also gives one place to test "n/a".
- Verified by: 28 tests, the build and lint passed locally, including three fresh `npm ci` runs following the CI steps. 13 of 13 deliberate faults were caught. In a real browser in dark mode, caption contrast went from 1.0 to 16.25. GitHub Actions run 41838591234 passed all three jobs. One test worker crashed once (exit code 134) on the first local run after installing and didn't come back in 8 later local runs or in CI, so the cause is unknown. I had my original components committed first, unchanged (5c3e7c8), then the fixes (8dfa9a0) and the test tools (1015f81), so the history shows who wrote what.

### 2026-11-04 — Frontend step 1: API client (Claude Code)
- Goal: typed calls for the scheme list and summary, using `VITE_API_BASE_URL`, with errors the UI can show. No visible change until the list page in step 2.
- Prompt: "yes approved" to the four-step frontend plan, starting with step 1.
- Kept: one `ApiError` type for every failure, with a friendly message, the status and any Problem Details. Claude added two cases I hadn't listed: a missing API address fails clearly, and a 200 that isn't JSON is rejected, because a wrong address can return a web page. Also `frontend/.env.example`, because Vite doesn't read the `.env` in the repo root. Updated design sections 2.5 and 6 to name `formatPercent`, as I decided.
- Changed/rejected: nothing yet. Filter and sort parameters wait for step 3.
- Verified by: 40 tests, the build and lint passed locally, and 12 of 12 deliberate faults in the client were caught. A throwaway run against real servers, read only: the live API returned the list and summary, my Vercel site gave the "unexpected response" error, an unknown API path gave a 404, and a closed port gave the "could not reach" error. Commit 78b497a. It reached the branch just after I merged PR #6, so it went into `main` with PR #7.

### 2026-11-04 — Frontend step 2: list page (Claude Code)
- Goal: replace the stock page with the real list page, showing my `SummaryCards` and `SchemesTable` with live data, and the loading, "Waking up the server…", error-with-retry and empty states from design section 6.
- Prompt: "you can commit and push and start with step 2"
- Kept: TanStack Query and React Router, as design section 6 specifies. Figures already on screen stay there if a background refresh fails. Requests still run when the browser reports being offline, so the error and retry button appear instead of the page waiting forever. Claude added `/schemes/new` as a placeholder, like the edit page, and a "Page not found" page, which weren't in the plan. It removed the stock `App.css`, images and `icons.svg`, stopped `#root` centring all text, and changed the page title.
- Changed/rejected: nothing yet. Until step 4, Edit opens a placeholder and the empty message has no button to add a scheme. I kept the test scheme in Atlas for my own manual tests.
- Verified by: 59 tests, the build and lint passed, including a fresh `npm ci`, and 20 of 20 deliberate faults were caught. In a real browser through a local proxy to the live API: real figures, Edit and back, deep links, offline and recovery, and "Waking up the server…" after 3 seconds with a delayed response. Calling the API directly from `localhost` gave a real CORS refusal, which showed the error and retry. Looking at the screenshots found a bug no test caught (see Rejected / corrected output). I reviewed it and approved the commit (cf83172), and GitHub Actions run 41886220444 passed all three jobs. I merged it (PR #7) and Vercel deployed it. Claude then checked the real site in a browser: the live figures loaded, the API allowed my Vercel address on every request, Edit and back worked, direct links and an unknown address showed the right pages, there were no console errors, and on a phone in dark mode the page didn't overflow and the heading and caption fixes were live.

### 2026-11-04 — Frontend step 3: filter and sort (Claude Code)
- Goal: filter by status and sort the table, with both kept in the address so reload, the back button and shared links work.
- Prompt: "1. several statuses 2. Yes as recommended 3. sort the way you assumed no need for additional column 4. yes 5.yes"
- Kept: my decisions on the questions Claude asked. Checkboxes for several statuses. Summary cards that follow the filter, with a "Showing 2 of 4 statuses" note. Sorting on Name, Status, Total homes and Total value, with no new column for `updatedAt`. Bad links show the normal list. "No schemes match this filter." with a "Clear filter" button. Claude also kept the current table on screen while a change loads, and treated every status ticked as no filter. It changed my `SchemesTable` to add sort buttons and moved the status labels from `StatusBadge` to `lib/format.ts`, so the filter and the badge share them.
- Changed/rejected: nothing yet. Known gap for step 4: "Back to schemes" on the placeholder page returns to the plain list and drops the filter.
- Verified by: 127 tests, the build and lint passed from a fresh `npm ci`, and 46 of 46 deliberate faults were caught. In a real browser against a production build: the live API accepted the filter and sort requests, and made-up responses in the browser, with nothing written to Atlas, covered sorting with several rows, reload, back and forward, bad links, the table staying during loading, keyboard use, and a failed request. I reviewed it and approved the commit (52e53da), and GitHub Actions run 41897837202 passed all three jobs, with 127 frontend and 239 backend tests. I merged it (PR #8). Claude then checked the real site in a browser: the filter, no-match message, reload, sort directions, shared links and bad links all worked, the API allowed my Vercel address on every request, nothing but reads was sent, and there were no console errors. One request in that first run asked for `status=Appraisal&status=Complete`, which the test never clicked. It didn't come back in six more logged rounds, so it's unexplained. Sorting several real rows still needs more than the one scheme on the live API.

### 2026-11-04 — Frontend step 4a: create form (Claude Code)
- Goal: add schemes from the site, with the §3 rules for instant feedback, server errors on the right fields, and a preview of the figures.
- Prompt: "1. yes I approve 4a 2. approved 3.yes as it should 4. yes you can"
- Kept: my decisions that a new form starts at Appraisal with every tenure at 0, and that a save returns to the list with the filter and sort kept and a "Scheme saved." message. react-hook-form, as design section 6 specifies. The preview adds up whole pence, because JavaScript has no decimal type and my rule is that money is never a double; the real figures still come from the API. "Back to schemes" now keeps the filter, closing the gap from step 3. Left out: a warning when leaving a form with unsaved changes.
- Changed/rejected: nothing yet. I told Claude to finish only the remaining checks and move on, so not every earlier deliberate fault was rerun on the final code. Editing is still a placeholder until step 4b.
- Verified by: 254 tests, the build and lint passed. A fresh install ran on the form code just before the last two tests were added. All 22 faults aimed at the final form code were caught, the last 2 after adding tests for them; 56 of 58 earlier faults were caught before the fixes. In a real browser against a production build: the live API rejected a real duplicate with a 409, shown on both name and address, with nothing saved. I reviewed it and approved the commit (384503b), and GitHub Actions run 41911458095 passed all three jobs, with 254 frontend and 239 backend tests. I merged it (PR #9). Claude then saved one real scheme through the form on my Vercel site: one click, one `POST`, a `201`, the saved total matched the preview (£40,537,042.95), and the `Location` header was `https://`, which settles the fix from PR #5. Sorting and filtering also worked with two real rows.

### 2026-11-04 — Frontend step 4b: edit form (Claude Code)
- Goal: edit an existing scheme in the same form, sending `version` so two people can't silently overwrite each other.
- Prompt: "done", after Claude said it would start step 4b next.
- Kept: a scheme loads once per visit, so nothing refreshes over what's being typed, and the next visit loads the latest values. A version conflict keeps my typing and offers "Reload latest version" instead of a retry. A missing scheme says it could not be found.
- Changed/rejected: nothing yet. A real successful edit hasn't been run, because it would change data, so it needs my go-ahead after deploy.
- Verified by: 285 tests, the build and lint passed. 19 of 20 deliberate faults were caught on a fresh install; the miss showed two settings did nothing, so Claude removed them, and the 2 faults for the later reload fix were also caught. In a real browser against the live API, with nothing changed: the form loaded the real scheme, a real stale version got a 409 and the reload prompt, a real duplicate showed on both fields, and a bad id gave "could not be found" after one request. Saving successfully used a made-up response. I reviewed it, approved the commit, and approved one real edit of the test scheme after deploy.

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
- **Overlapping page heading:** Claude's list page set a 32px heading but kept a fixed line height of about 23px from the stock stylesheet, so the title's two lines overlapped on a phone. All tests and contrast checks passed. Claude only found it by looking at a screenshot, then set the line height and checked again.
- **Save click lost in step 4a:** in a real browser, the first click on Save could do nothing. An error disappearing or appearing as the field lost focus moved the button between mouse down and mouse up. All tests had passed, and it only showed in the browser and a screenshot. Claude stopped flagging fields before they're reached and kept focus in the field on mouse down.
- **Duplicate error lost in step 4a:** that fix then made the duplicate error vanish from the address field, because moving focus re-ran the client check and erased the server's error. The final browser run caught it. Claude keeps server errors separately now, so they stay until the value changes.
- **Second save in step 4a:** a test showed a disabled button didn't stop every submit, which could have sent a second save. The form now blocks it.
- **Fault runs in step 4a:** one deliberate fault made the page redirect forever and stalled a run, and one run was invalid because a mistake in a test made every fault look caught. Claude discarded that result, and the script now refuses to run unless all tests pass first.
- **Reload button in step 4b:** "Reload latest version" only replaced the typing if the server's version had changed. A browser test that forced a conflict showed it. The form now restarts on every reload, so the button always does what it says.
- **Overlapping test runs in step 3:** after a session interruption, Claude started a new fault-injection run in the same step as its process check, so it didn't see that the old run was still going. Both used the same scratch folder, so Claude threw away that result and reran it alone. My project files weren't involved.
- **Sort direction test gap in step 3:** the overlapping run still pointed to a real gap. If the page always told the table "ascending", no test failed, because the reverse-sort test only checked the address and the request. Claude added the missing checks, and the clean rerun caught that fault.
- **Leftover test server:** Claude reported stopping its scratch Vite server after the frontend review, but it was still running. It noticed when a new server couldn't take the port and the page title was wrong, so it didn't trust that page's readings as the fixed code. It used them as the "before" reading instead, then stopped the server and confirmed the ports were free.

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
| `frontend/vercel.json` didn't exist | Fixed (PR #4) | Deep links would have failed on Vercel |
| The frontend still shows the stock page, and my components aren't displayed anywhere | Fixed (step 2) | The list page now shows them with live data |
| `POST` returned an `http://` `Location` on Render | Fixed (PR #5, confirmed on the live site) | Render handles HTTPS, so the app needs `X-Forwarded-Proto` to build correct links |
| Table caption was invisible in dark mode | Fixed (8dfa9a0) | The table's white background doesn't reach the caption, so it was dark text on a dark page |
| Money, number and percent formatting was written twice | Fixed (8dfa9a0) | Design section 6 wants one place for it |
| Empty message differed from design section 6 | Fixed (8dfa9a0) | Design section 6 specifies the wording |
| Two schemes with the same name had identical Edit button labels | Fixed (8dfa9a0) | Design section 3 allows the same name at another address, and a screen reader couldn't tell them apart |
| "Affordable" column showed a percentage | Fixed (8dfa9a0) | "Affordable %" is clearer |
| No types for requests or Problem Details | Fixed (8dfa9a0) | The form will need them. Checked against real error responses from the live API |
