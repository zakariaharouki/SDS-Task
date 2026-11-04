# Scheme Pipeline Tracker

![CI](https://github.com/zakariaharouki/SDS-Task/actions/workflows/ci.yml/badge.svg)

A small app for a housing developer's team to keep track of the schemes in their pipeline. You can add and edit schemes, filter and sort them by status, and see the figures that matter: total homes, percentage affordable and total value, for each scheme and for the whole portfolio.

**Start here: [open the live app](https://sds-task-theta.vercel.app/).** There's nothing to install. The API is on a free plan and sleeps when idle, so the first load can take up to a minute. The page tells you when that's happening.

- API docs: [Swagger](https://sds-task-c7p5.onrender.com/swagger)
- Full design: [design.md](design.md)
- Everything I did with AI, step by step: [AI-LOG.md](AI-LOG.md)

## Try it in a few minutes

1. Click **Add scheme**, enter a name and address, and save with the tenures at 0. An early scheme is allowed to have no homes yet. Its percentage shows as "n/a", not 0%.
2. Add another scheme and change its status to Planning while it still has 0 homes. You get a warning, and it won't save until you add a home.
3. Enter homes and average values and watch the preview update. After saving, check that the list shows the same figures.
4. Try to save a scheme with the same name and address as an existing one. Both fields show the error. The same name at a different address is fine.
5. Tick a couple of statuses, click a column heading to sort, then reload the page or copy the link. Your view stays as it was, and the summary cards follow the filter.
6. Open the same scheme in two tabs and save in both. The second save is stopped and offers to reload the latest version, so nobody overwrites anyone else by accident.

## Running it yourself

You don't need to. It's deployed, and that's the easy way to use it. If you want to work on the code, you'll need .NET 8, Node 22 and your own hosted MongoDB, such as a free Atlas database.

**API:**

```bash
cd backend/SchemeTracker.Api
export Mongo__ConnectionString="your connection string"
export Mongo__Database="your database name"
dotnet run --launch-profile http
```

On Windows PowerShell, set them with `$env:Mongo__ConnectionString = "..."` instead. The API stops straight away if it can't reach the database, because the unique index that blocks duplicates has to exist first.

**Frontend**, in a second terminal:

```bash
cd frontend
cp .env.example .env
npm ci
npm run dev
```

Then open http://localhost:5173. A local frontend has to use a local API, because the live API only accepts browser requests from the live site.

**Tests:**

```bash
dotnet test backend/SchemeTracker.sln
cd frontend && npm test
```

The database tests start a real MongoDB in Docker, and they skip themselves if Docker isn't there. GitHub Actions runs everything on every push, with no database credentials.

After cloning, run `git config core.hooksPath .githooks` once. It turns on a hook that stops a `.env` file from being pushed.

## How it's built

| Part | Choice | Hosted on |
|---|---|---|
| API | .NET 8 Web API, in controller, service and repository layers | Render |
| Frontend | React with TypeScript, using Vite | Vercel |
| Database | MongoDB | Atlas |
| Tests | xUnit, Testcontainers, Vitest, React Testing Library | GitHub Actions |

## The data

A scheme has a name, a site address, a status and a tenure mix.

- **Status** is one of Appraisal, Planning, On site or Complete. Sorting by status follows that pipeline order, not the alphabet.
- **Tenure mix** always has the same four lines: social rent, affordable rent, shared ownership and market sale. Each has a number of homes and an average value.
- **Figures** are worked out by the API. Total homes and total value add up all four lines. Affordable homes are everything except market sale.
- **Money** is `decimal` in the API and `Decimal128` in MongoDB, never a floating point number. The form shows a preview while you type, calculated in whole pence, but the saved figures always come from the API.
- **Version** goes up with every save, which is how two people editing at once get caught.

## The rules

The API is the source of truth. The form repeats the same rules so you find out sooner.

- Name is required, up to 200 characters. Site address is required, up to 300. Both are trimmed first.
- Homes on each line must be a whole number from 0 to 10,000. Something like 2.5 is rejected, not rounded.
- Average value is from £0 to £10,000,000 with at most 2 decimal places, and must be above £0 if the line has homes.
- A scheme past Appraisal needs at least 1 home.
- No two schemes can share the same name and address, ignoring case and extra spaces. "Rd" and "Road" count as different, on purpose.
- Fields the client shouldn't set, such as `id` or `createdAt`, are rejected instead of quietly ignored.

Errors come back as Problem Details with field paths, so the form can put each message on the right input.

## Decisions and trade-offs

- **Blocking duplicates with a unique index, not a check in code.** Two saves at the same moment can't both get through. The cost is an extra stored key.
- **Saving the calculated figures with each scheme.** Sorting and the summary stay simple. They could drift if someone edits the database by hand.
- **"n/a" instead of 0% when there are no homes.** Unknown isn't the same as no affordable housing.
- **Any status change is allowed.** Schemes really do go backwards, so I didn't guess at workflow rules.
- **Filter and sort live in the address.** Reload, the back button and shared links all work.
- **Atlas accepts connections from anywhere.** Render's free plan has no fixed IP address, so security rests on a strong password and TLS. On a paid plan I'd restrict it.

**Left out on purpose**, to keep it small and finished: logins, delete, pagination, status history, address lookup, multiple currencies and rate limiting. [Section 9 of the design](design.md#9-deliberately-left-out) gives the reason for each.

## What I'd do next

- Add logins with Entra ID, then record who changed what.
- Add archiving, not hard delete, and a history of status changes.
- Warn about possible duplicates such as "Rd" and "Road", without blocking them.
- Add a chart of the tenure mix for each scheme.
- Add rate limiting if the API were ever public.

## How I used AI

**Tools.** I used Claude to draft the design and Claude Code to build. Claude Code could build and run the code, so what it told me came with test results. I also used graphify to give it a map of the code, which cut down how much it had to read.

**How I steered it.** I wrote the design decisions first, and I kept a [CLAUDE.md](Claude.md) with my rules: propose a plan before writing code, one feature per step, ask when unsure, money is never a double, and hands off `SchemeCalculator`. I approved each plan, and I decided every commit and push.

**One prompt I had to refine:**

- First try: "I want you to use repository Layer Architecture."
- What worked: "Yes I meant repository layer by creating DTOs, Mapping, Controllers (Empty I will fill in the logic later), Services and Interfaces. I also want you to add all Services to program.cs as usual."

The first was too vague, and I got a single interface back. Naming each layer got me the full structure and left the logic for me to write.

**One thing I rejected.** Claude Code's first CI workflow passed the MongoDB secret to the test step, even though it said nothing used it and suggested removing it later. That note in its own report is how I caught it. Real credentials have no place in test runs, so I had it removed straight away. CI has run without any database credentials since.

**Where I didn't use AI.** I wrote the controller actions and the service logic myself, including the totals and percentage formulas, because I wanted to own that logic. Claude Code reviewed them, and later moved the formulas into `SchemeCalculator` with my approval. The `SchemeCalculator` tests are reserved for me too, and I haven't written them yet.

**The conversations.** [Design chat](https://claude.ai/share/8d58a74c-8473-4f72-bd64-16d2a4385914). Claude Code session: link to add.

## AI code review

I asked Claude Code to review my controllers and services before deploying, and to tell me what was missing before changing anything. [AI-LOG.md](AI-LOG.md) has the full list.

| Area | What it found | What I did |
|---|---|---|
| Security | Errors exposed raw framework messages, clients could send `id` and `createdAt`, and CORS wasn't set up | Fixed: friendly messages, those fields rejected, and only configured origins allowed |
| Validation | Blank names, status values like `99` and `"planning"`, out-of-range homes and values, and 3 decimal places were all accepted | Fixed, on the API and again on the form |
| Edge cases | Banker's rounding gave 0.2 for 0.25%, a stale save returned 404 instead of 409, and `duplicateKey` was never set | Fixed |
| Test gaps | Not every endpoint was tested against a real database, and that testing then found a timestamp mismatch | Fixed |
| Browser only | The first click on Save could be lost, because an error moved the button mid-click | Fixed. All the tests had passed, so I learned to look at the real page as well |

**Where I disagreed.** A `|` in a name or address could cause a false duplicate. It takes a very unlikely pair of schemes, and rewording gets past it, so I documented the limit instead of adding code for it.
