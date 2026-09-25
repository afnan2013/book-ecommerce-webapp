# Book Ecom - React Client

This file is both **agent instructions** and a **refresher for the author**.
The project is a learning exercise, so this document explains *why* each piece exists, not just *what* it is.
Read it top to bottom once when you come back after a break; after that, jump to the section you need.

---

## 1. What this app is

A marketplace for books with two flows the product eventually wants:

1. **Sell** - a user lists a book (new or used), another user buys it.
2. **Rent** - a user rents a book for a period and must return it by a deadline.

Payment is mocked (no real provider).

**What is actually built in the client today is not the marketplace.**
It is the **admin back-office**: log in, and depending on who you are, you land on an admin panel for managing users, roles and permissions.
Buyer and seller pages exist as placeholders only.
Think of it as: the shop's staff-room is finished, the shop floor is not.

### Where this client points

The client talks to the **paused ASP.NET backend** in `server_asp/`, on `VITE_API_BASE_URL` (default `http://localhost:5027/api`).
The active backend work is the Go rewrite in `server_go/`, which does not serve this client yet.
Cutover happens when Go reaches parity.
Until then, **do not change client endpoints to match Go** unless explicitly asked.

---

## 2. Running it

```bash
cd client
cp .env.example .env     # first time only
npm install
npm run dev              # Vite dev server
npm run build            # tsc -b && vite build  <- this is the real typecheck
npm run lint             # eslint
```

`npm run build` is the gate that matters.
`tsc -b` runs with `noUnusedLocals`, `noUnusedParameters` and `erasableSyntaxOnly`, so code that "works" in dev can still fail the build.
Run it before claiming a change is done.

---

## 3. The single most important idea: two kinds of state

Almost every structural decision in this client follows from one distinction.
If you remember nothing else, remember this.

**Server state** is data that lives in the database and is only *borrowed* by the browser.
The list of users, the list of roles, the permission catalog.
You do not own it.
Someone else can change it while you are looking at it.
Your copy is always potentially stale.

**Client state** is data that only exists in the browser and has no authority anywhere else.
Who is currently logged in, the JWT token, whether a dialog is open, what is typed in a search box.
Nobody else can change it.
It is true by definition, because you are the one who set it.

These two need completely different machinery.

Server state needs: caching, "is this stale?", background refetching, loading flags, error flags, retry, deduplication of identical requests, and invalidation after a write.
Client state needs: a variable, a setter, and a way for components to subscribe.

Trying to use one tool for both is where React codebases go wrong.
That is why this project has **two** state libraries and they are not redundant.

| Kind | Tool | Example in this app |
| --- | --- | --- |
| Server state | **TanStack React Query** | users list, roles list, permissions catalog, `/users/me` |
| Client state (global) | **Zustand** | `authStore` - the logged-in user and their token |
| Client state (local) | plain `useState` | dialog open/closed, search text, checkbox selections before save |
| Form state | **react-hook-form** + **zod** | login, register, create-user |

---

## 4. Zustand - what it is and what problem it solves

### The problem

The logged-in user is needed in many unrelated places: the header (to show the email), the route guards (to decide if you may enter `/admin`), the axios interceptor (to attach the token to every request).
These components are nowhere near each other in the tree.

Passing `user` down as a prop through every layer is "prop drilling", and it is miserable.
React's built-in answer is Context, but Context has a real flaw: **when the context value changes, every component that consumes it re-renders**, even components that only cared about a field that did not change.

### What Zustand is

A tiny store that lives **outside** React.
It is a plain JavaScript object with a subscribe mechanism, plus a React hook to read from it.

Analogy: think of it as a **noticeboard in the staff room**.
Anyone can walk up and read it.
Anyone can pin a new note.
But each reader says in advance *which line* of the noticeboard they care about, and they are only tapped on the shoulder when that specific line changes.

That "which line" part is the **selector**, and it is the whole point:

```ts
// Subscribes to `user` only. Re-renders when user changes, NOT when token changes.
const user = useAuthStore((s) => s.user);
```

Compare with Context, where you would get the whole object and re-render on any change.

### The store itself

`src/stores/authStore.ts` - the only store in the app, on purpose.

```ts
interface AuthState  { user: User | null; token: string | null }
interface AuthActions { setAuth; setUser; clear }
```

Three deliberate details:

**1. State and actions live in the same store.**
Unlike Redux there are no reducers, no action types, no dispatch.
`clear()` is just a function that calls `set({ user: null, token: null })`.

**2. `persist` middleware.**
Wrapping the store in `persist` writes it to `localStorage` under the key `book-ecom-auth` on every change, and rehydrates it on page load.
Without this, refreshing the page would log you out.
`partialize` controls exactly which fields get written, so you never accidentally persist transient junk.

**3. `selectIsAuthenticated` is exported as a standalone function.**

```ts
export const selectIsAuthenticated = (s: AuthStore): boolean => Boolean(s.token && s.user);
```

It is defined once at module level rather than inline, so it is a **stable function reference**.
Zustand compares the selector's *result* with `Object.is`, and because this returns a boolean, the component only re-renders when the true/false answer actually flips.
If it returned a new object each time, you would re-render on every store change.
This is the classic Zustand/Redux selector gotcha.

### Reading the store outside React

`src/lib/api/client.ts` does this:

```ts
const token = useAuthStore.getState().token;   // no hook, no component
```

This is a Zustand superpower.
The store is a normal object, so non-React code (the axios interceptor) can read and write it.
You cannot do that with Context.

### Interview questions this invites

- Why not Context? (re-render granularity, and usability outside React)
- Why not Redux? (no boilerplate needed for this scale; Zustand is ~1KB)
- What does `persist` cost you? (writes on every change; hydration is async in some setups; XSS exposure for the token)
- Why is putting a JWT in `localStorage` a risk, and what is the alternative? (httpOnly cookie + CSRF protection; here the mitigation is a short token lifetime)

---

## 5. React Query - what it is and what problem it solves

### The problem

The naive way to load the users list:

```tsx
const [users, setUsers] = useState([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState(null);
useEffect(() => { fetch(...).then(setUsers).catch(setError).finally(() => setLoading(false)); }, []);
```

This is 8 lines to do one thing, and it is **also wrong** in ways that are not obvious:

- Two components both needing users fire two identical requests.
- Navigating away and back refetches from scratch and shows a spinner again, even though you fetched two seconds ago.
- After you create a user, this list has no idea it is now stale.
- Race condition: a slow response from an old render can overwrite a newer one.
- No retry, no cancellation, no background revalidation.

Every one of those is a caching problem, not a React problem.

### What React Query is

**A cache for server data, keyed by a query key, shared across the whole app.**

That is genuinely the whole idea.
It is not a data fetching library; it does not care how you fetch (here it is axios).
It cares about *what happens to the result afterwards*.

Analogy: a **librarian's desk**.
You ask for "the users list".
If a recent copy is already on the desk, you get it instantly.
If the copy is older than the freshness rule, you still get it instantly, and the librarian quietly goes and fetches a new one in the background and hands it over when it arrives.
If ten people ask at the same moment, the librarian makes one trip, not ten.

### The two hooks

**`useQuery` = a read.**
You give it a key and a function that returns data.
It hands you `{ data, isLoading, isError, error, refetch, isFetching }`.

**`useMutation` = a write.**
You give it a function that performs the write.
It hands you `{ mutate, isPending, ... }`.
Writes are not cached, because you never want to "reuse" a POST.
Instead, after a successful write you tell the cache what is now stale.

### Query keys are the addressing system

`src/lib/queryKeys.ts` centralises every key in the app:

```ts
users: {
  all:  ['users'],
  list: () => ['users', 'list'],
  byId: (id) => ['users', id],
}
```

Keys are arrays, and React Query matches them by **prefix**.
So invalidating `['users']` invalidates `['users','list']` *and* `['users', 7]` in one call.
That hierarchy is why the keys are shaped this way.

Centralising them in one file matters because a typo in a key string is a silent bug: the cache simply never invalidates and the UI shows stale data with no error anywhere.
**Never write a raw key array in a component or hook. Always use `queryKeys`.**

### The defaults, and why

`src/app/queryClient.ts`:

```ts
staleTime: 30_000,             // data is "fresh" for 30s -> no refetch on remount
gcTime:    5 * 60_000,         // unused cache entries are dropped after 5min
retry: false,                  // do NOT retry failures
refetchOnWindowFocus: false,   // do NOT refetch when you alt-tab back
```

`staleTime` vs `gcTime` is a classic interview question.

- **`staleTime`** = how long the data is trusted without re-checking. Still *displayed* after it expires; it just triggers a background refetch.
- **`gcTime`** = how long the data stays in memory after *no component is using it any more*. This is about memory, not freshness.

`retry: false` is deliberate for an admin app.
Retrying a 403 or a 409 three times is pointless and makes errors feel slow.
In a flaky-network consumer app you might turn it back on for reads only.

`refetchOnWindowFocus: false` is a taste decision: this is an admin tool, and surprise refetches while you are mid-edit are annoying.

### The write-then-invalidate cycle

This is the loop the whole admin panel runs on.
Example from `features/admin-users/hooks.ts`:

```ts
export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all });
    },
  });
}
```

You never manually push the new user into a local array.
You tell the cache "anything under `users` is now suspect", and React Query refetches whatever is currently on screen.
**The server stays the single source of truth.**
No local/remote drift is possible, because there is no local copy to drift.

A slightly smarter variant is used where the server returns the updated entity:

```ts
onSuccess: (user) => {
  queryClient.setQueryData(queryKeys.users.byId(id), user);   // seed the detail cache, zero refetch
  queryClient.invalidateQueries({ queryKey: queryKeys.users.list() });  // list must refetch
}
```

`setQueryData` writes the response straight into the cache, so the detail page updates with no extra round trip.
The list still gets invalidated because the response only described one user.

### Interview questions this invites

- Difference between `staleTime` and `gcTime`.
- `invalidateQueries` vs `refetchQueries` vs `setQueryData`.
- What is `isLoading` vs `isFetching`? (`isLoading` = first load, no data yet; `isFetching` = any in-flight request including background ones. `UsersListPage` uses both: a table skeleton for the first, a quiet "Refreshing..." line for the second.)
- How would you do optimistic updates? (`onMutate` + `setQueryData` + rollback in `onError`. Not used here, deliberately, because these writes are concurrency-checked and rolling back a 409 correctly is fiddly.)

---

## 6. The dividing line, stated once

> **If a server owns it, it goes in React Query. If only the browser knows about it, it goes in Zustand or `useState`.**

The one interesting case is the current user, which is *both*.

- The **token** is client state (Zustand, persisted) - only the browser has it.
- The **user profile** is server state (`useMe()` query against `/users/me`) - the server owns it.

`features/auth/hooks.ts` bridges them: `useMe` fetches from the server and then calls `setUser` to push the fresh copy into Zustand, so that non-React code and route guards can read it synchronously without waiting on a query.
That is the one deliberate exception to the rule, and it exists because route guards must decide *immediately* on first render, before any fetch could resolve.

---

## 7. Folder tour

```
client/src/
  app/                  cross-cutting app wiring (not a feature, not a UI kit)
    queryClient.ts        the single QueryClient + global defaults
    routes/
      RequireAuth.tsx        "are you logged in at all?"
      RequireUserType.tsx    "are you the right kind of user?"
      RoleLandingRedirect.tsx  "/" -> /admin | /buyer | /seller

  components/           UI shared across features
    ui/                   shadcn components - WE OWN THESE, edit freely
    layout/               PublicLayout / AdminLayout / DashboardLayout
    FormField.tsx         label + control + error message, in one place
    NotFoundState.tsx     "X not found" + a back button

  features/             one folder per product capability
    auth/  admin-users/  admin-roles/  permissions/  buyer/  seller/  shared/

  lib/                  framework-agnostic plumbing
    api/client.ts         the ONE axios instance + interceptors
    api/ApiError.ts       typed error class, RFC 7807 aware
    errors/describeApiError.ts   error -> user-facing sentence
    queryKeys.ts          every cache key in the app
    types/                shared wire types (user, role, permission, book)
    utils.ts              cn() - clsx + tailwind-merge

  stores/authStore.ts   the only Zustand store
  router.tsx            the whole route tree
  main.tsx              providers: QueryClient -> Router -> Toaster
  index.css             Tailwind v4 + shadcn theme tokens
```

### Why "feature folders" instead of `components/ pages/ hooks/ api/`

The by-type layout (all hooks in one folder, all pages in another) feels tidy and scales badly.
To change one thing you open five folders.
To delete a feature you hunt through five folders and inevitably leave orphans.

The by-feature layout puts everything about one capability in one place.
Deleting `admin-roles/` deletes the entire roles feature cleanly.

**Note the naming, because it matters.**
The folders are not `users/` and `roles/`, they are **`admin-users/` and `admin-roles/`**.
That is a bet on the future: when the buyer-facing "browse books" screens arrive, they will be a separate `buyer/` feature, not crammed into a generic `books/`.
A feature is a **use case for a specific audience**, not a database table.

### The anatomy of a feature

Every full feature has the same four files.
This regularity is the point: you always know where to look.

| File | Job | Rule |
| --- | --- | --- |
| `api.ts` | typed axios calls, one function per endpoint | knows nothing about React |
| `hooks.ts` | React Query wrappers around `api.ts` | owns cache keys + invalidation |
| `schemas.ts` | zod schemas for forms | types derived via `z.infer` |
| `pages/` | route-level screens | composes components, no fetching logic |
| `components/` | feature-local UI | dumb-ish; takes props, calls a hook to save |

The layering is strict and worth being able to defend:
`api.ts` is pure transport and could be reused by a CLI or a test.
`hooks.ts` is the only place that knows about caching.
Pages compose; they do not fetch by hand.

**Cross-feature imports go through the feature root, never into `pages/`.**
`UserEditPage` imports `useRoles` from `@/features/admin-roles/hooks` - fine.
Importing `@/features/admin-roles/pages/RoleEditPage` from another feature would be a violation.

### `lib/` vs `app/` vs `components/`

- `lib/` = could be copy-pasted into a different React app and still work.
- `app/` = specific to *this* app's composition (routing, query client).
- `components/` = shared UI.

---

## 8. Walkthrough: one full request, end to end

Tracing "admin changes a user's roles" touches every layer.
This is the best single thing to re-read when you come back cold.

**1. Route.**
`/admin/users/7` matches in `router.tsx`.
It is nested under `<RequireAuth>` and then under `<RequireUserType userType={UserType.Employee}>`, so both guards run first.

**2. Guards.**
`RequireAuth` reads `selectIsAuthenticated` from Zustand (synchronous, already rehydrated from localStorage).
If false, `<Navigate to="/login" state={{ from: location.pathname }} />` - note it remembers where you were going, and `LoginPage` sends you back there after login.
It also calls `useMe()`, which fetches `/users/me` **once**, when `RequireAuth` first mounts.
Not on every navigation - `RequireAuth` stays mounted the whole time you move around inside the guarded area.
See 9.11, because this surprises people.

**3. Page.**
`UserEditPage` reads `:id` from the URL, converts to a number, and runs three queries: `useUser(id)`, `useRoles()`, `usePermissions()`.
All three are independent, all three run in parallel, all three are cached.
`useRoles()` is the *same* query the roles list page uses, so if you were just on that page, it is already in the cache and resolves instantly.
That sharing is free and is exactly what you lose with `useEffect` fetching.

**4. Render.**
`<UserRolesSection>` receives `initialRoleIds` as a prop, seeds a `useState<Set<number>>` from it, and lets you tick checkboxes.
This local selection is **client state** - it is a draft, the server knows nothing about it yet.

**5. Save.**
`useUpdateUserRoles(id).mutate({ roleIds, concurrencyStamp })`.

**6. Transport.**
`api.ts` calls `apiClient.put('/users/7/roles', ...)`.
The request interceptor pulls the token out of Zustand and sets `Authorization: Bearer ...`.

**7. Response, happy path.**
The server returns the updated `UserDetail`.
`onSuccess` does `setQueryData(users.byId(7), user)` and `invalidateQueries(users.list())`.
The component toasts "Roles updated."

**8. Response, 401.**
The response interceptor sees 401, calls `useAuthStore.getState().clear()`.
Zustand notifies subscribers, `RequireAuth` re-evaluates to false, and you are bounced to `/login`.
No component wrote a line of code for this.

**9. Response, 409 conflict.**
The interceptor wraps the body in `new ApiError(409, body)`.
`onError` in the hook sees `err.kind === 'conflict'` and invalidates `users.byId(7)`.
The component's `onError` toasts `describeApiError(err, ..., 'user')`, which produces the server's message or falls back to "This user was modified by someone else. Please refresh and try again."
The invalidation refetches the user, which produces a **new `concurrencyStamp`**, which changes the `key` on the section, which **remounts it** with fresh `initialRoleIds`.
Your stale draft is discarded and you are looking at current truth.

That last step is the cleverest pattern in the codebase.
See section 9.

---

## 9. The patterns worth knowing by name

### 9.1 `key={concurrencyStamp}` instead of `useEffect` sync

**The problem.**
A section holds local draft state (`selected` checkboxes) seeded from server data.
When the server data changes underneath, the draft must be thrown away and re-seeded.

**The tempting wrong answer.**

```tsx
useEffect(() => { setSelected(new Set(initialRoleIds)); }, [initialRoleIds]);
```

This renders once with stale state, then corrects itself - a visible flash.
It also needs exhaustive-deps care, and `initialRoleIds` is a new array every render unless memoised.

**The answer used here.**

```tsx
<UserRolesSection key={`roles-${userQuery.data.concurrencyStamp}`} ... />
```

React treats a changed `key` as "this is a different component".
It unmounts the old one and mounts a fresh one, so `useState(() => initialSet)` runs again with the new data.
One render, no flash, no effect, no dependency array.

**The concept:** `key` is React's identity mechanism, not just a list optimisation.
Changing `key` is the idiomatic way to say "reset all local state here".
The React docs call this "resetting state with a key".

The `concurrencyStamp` is perfect for this because the server changes it on every write, so it is a natural version identifier.

### 9.2 Optimistic concurrency, and why the client sends a stamp back

Two admins open user 7.
Admin A adds the Moderator role and saves.
Admin B, who loaded the page before that, unticks something and saves.
Without protection, B silently overwrites A's change.
This is the **lost update** problem.

The fix: every mutation sends back the `concurrencyStamp` it was rendered from.
The server compares it with the current stamp and returns **409 Conflict** if they differ.
"Optimistic" means we do not lock anything; we assume conflicts are rare and only detect them at write time.

The client's job is threefold, and all three are implemented:
1. Send the stamp (`api.ts`).
2. On 409, refetch so the user sees current truth (`hooks.ts` `onError`).
3. Remount so the draft is discarded (`key=` in the page).

### 9.3 One axios instance with two interceptors

`lib/api/client.ts` is the only place in the app that knows a JWT exists on the wire.

**Request interceptor** - attaches `Authorization` from Zustand.
Reading the store with `getState()` at request time (not at module load) means the token is always current.

**Response interceptor** - two jobs.
On 401, clear auth globally, which cascades into a redirect via the guards.
On any error with a JSON body, throw a typed `ApiError` instead of a raw `AxiosError`.

That second job is why no component in this codebase ever writes `err.response.data.title`.
By the time an error reaches a component, it is already a clean, typed object.

### 9.4 `ApiError` - branch on meaning, not on numbers

```ts
export type ApiErrorKind = 'conflict' | 'validation' | 'unauthorized' | 'forbidden' | 'notFound' | 'server' | 'unknown';
```

`kindFromStatus` maps HTTP codes to these once.
Callers write `err.kind === 'conflict'`, not `err.status === 409`.
The reason is readability and a single point of change: if the server ever signals conflict differently, one function changes.

The class tolerates **two** wire shapes on purpose: RFC 7807 `{ type, title, status, detail }` and the older ad-hoc `{ error: "..." }`.
The server has since been retrofitted to 7807, but the tolerance is harmless and costs nothing.

### 9.5 `describeApiError` - errors become sentences in exactly one place

```ts
describeApiError(err, 'Failed to update roles.', 'user')
```

Server message if there is one; an entity-aware conflict sentence if it is a 409 with no message; the fallback otherwise.
Every `toast.error` and every inline error box goes through it.
That is why error copy is consistent across the app without anyone policing it.

### 9.6 Forms: react-hook-form + zod

Two libraries, two distinct jobs.

**react-hook-form** manages the form.
Its trick is **uncontrolled inputs**: `{...register('email')}` attaches a ref to the DOM node instead of putting the value in React state.
Typing therefore causes **zero re-renders** of the form.
Compare with `useState` per field, where every keystroke re-renders.

**zod** describes what valid data looks like.

```ts
export const loginSchema = z.object({
  email: z.email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
});
export type LoginInput = z.infer<typeof loginSchema>;
```

`z.infer` is the payoff: **the schema is the single source of truth for both runtime validation and the TypeScript type**.
They cannot drift, because one is generated from the other.

`zodResolver` glues them: RHF asks zod to validate, zod's messages land in `formState.errors`.

**`<Controller>` is for inputs that cannot be `register`ed.**
The account-type `<select>` needs `Number(e.target.value)` because HTML select values are always strings and `UserType` is numeric.
`register` cannot express that transform, so `Controller` takes manual control of value/onChange.
Same reason you would need it for a Radix Select or a date picker.

**Where this rule is bent:** `RoleRenameSection` and `CreateRoleDialog` use plain `useState` because they are single trivial text fields.
Defensible, but inconsistent - see section 12.

### 9.7 No TypeScript enums, ever

`erasableSyntaxOnly: true` in `tsconfig.app.json` forbids `enum`, because `enum` emits real JavaScript at runtime and the bundler is configured to only strip types, never transform them.

The replacement:

```ts
export const UserType = { Employee: 1, Seller: 2, Buyer: 3 } as const;
export type UserType = (typeof UserType)[keyof typeof UserType];  // 1 | 2 | 3
```

`as const` freezes the literal values so TypeScript infers `1` rather than `number`.
The indexed access then builds the union.
The const and the type share a name, which is legal because TS keeps value and type namespaces separate, so `UserType.Buyer` (value) and `userType: UserType` (type) both read naturally.

`UserTypeLabel` sits next to it as a `Record<UserType, string>`, so adding a new user type is a compile error until you add its label.
That is the pattern doing real work.

### 9.8 Route guards are components, not config

Three guards, three distinct jobs:

| Guard | Question | Failure |
| --- | --- | --- |
| `RequireAuth` | Logged in at all? | `/login`, remembering origin |
| `RequireUserType` | Right kind of user? | `/forbidden` |
| `RoleLandingRedirect` | Where should `/` go? | `/forbidden` |

`RequireAuth` uses `<Outlet />` because it wraps a whole route subtree.
`RequireUserType` uses `children` because it wraps a single element inline in the route config.
Both shapes are idiomatic; pick by how you are nesting.

**Important:** these are UX, not security.
Anyone can edit localStorage.
The real enforcement is `[HasPermission(...)]` on the server.
The client guard exists so a buyer sees a friendly "Forbidden" page instead of an admin screen full of failed requests.
Be able to say this out loud in an interview.

### 9.9 Hooks-before-returns discipline

`RequireAuth` calls `useMe()` *before* the `if (!isAuthed) return <Navigate/>`.
`UserEditPage` runs all three queries before the invalid-id early return.
That is the Rules of Hooks: hook call order must be identical on every render, so no hook may sit after a conditional return.

The cost is a query that is created even when it will not be used, which is why `useUser` and `useRole` carry `enabled: Number.isFinite(id) && id > 0` - `enabled: false` means the hook exists but never fires a request.

### 9.10 shadcn/ui is source code, not a dependency

`components/ui/*` were generated by the shadcn CLI into this repo.
They are **our files**.
Edit them directly when a variant or a behaviour is missing; do not wrap them in yet another wrapper.

This is a real architectural choice worth defending: you get Radix's accessibility primitives (focus trapping, ARIA, keyboard handling in `dialog.tsx`, `checkbox.tsx`) without the "fight the library's opinions" problem you get with MUI or Chakra.

Styling is Tailwind v4 with the Radix Nova preset.
Theme tokens live as CSS custom properties in `index.css` (`--primary`, `--muted-foreground`, `--destructive`, ...), with a `.dark` variant block.
`cn()` in `lib/utils.ts` is `clsx` + `tailwind-merge`: clsx handles conditional classes, tailwind-merge resolves conflicts so a later `px-4` correctly beats an earlier `px-2` instead of both landing in the class string.

### 9.11 When React Query actually hits the network

The most common confusion with this library: **data going stale does not cause a fetch.**
There is no background timer watching `staleTime` and firing a request when it expires.

`staleTime` only answers a yes/no question - "is this data still trusted?" - and that answer is consulted **only when something else asks**.
If nothing asks, nothing happens, forever.

There are exactly five things that ask:

| Trigger | Controlled by | In this app |
| --- | --- | --- |
| A new observer mounts (a component calls `useQuery` with that key) | `refetchOnMount`, default `true` *if stale* | the main one |
| The browser window regains focus | `refetchOnWindowFocus` | **off** globally |
| The network reconnects | `refetchOnReconnect`, default `true` | on, rarely fires |
| A timer you set | `refetchInterval` | **not used anywhere** |
| You ask explicitly | `invalidateQueries` / `refetch()` | the write-then-invalidate cycle |

Now apply that to `useMe`.
It is called in exactly one place, `RequireAuth`, and `RequireAuth` is a **pathless layout route** wrapping `/`, `/admin`, `/buyer` and `/seller`.
React Router keeps a layout route mounted while you navigate between its children.
So going `/admin/users` -> `/admin/roles` -> `/admin/users/7` never unmounts `RequireAuth`, which means the observer never re-mounts, which means trigger 1 never fires.
Focus is off, there is no interval, and nothing in the codebase ever invalidates `['auth','me']`.

**Result: `GET /users/me` fires once per page load, and then never again.**
That is not a bug, it is the cache doing its job.

Where it *does* fire:

- Right after login, when you cross from `/login` (outside `RequireAuth`) into `/` (inside it) and `RequireAuth` mounts for the first time.
- On every hard refresh (F5), because the React Query cache lives in memory only and is wiped.

That second point is a useful contrast: the **token survives a reload** (Zustand `persist` -> localStorage) but the **query cache does not** (memory only).
That asymmetry is deliberate, not an oversight.
Caching a user list to disk means showing data that could be hours old; re-fetching it costs one cheap request.

**Why you probably could not see it in the Network tab.**
Chrome only records network activity while DevTools is open, so if you logged in first and opened DevTools afterwards, the request already happened and was never recorded.
Also check the **Fetch/XHR** filter is selected and **Preserve log** is ticked.
To see it reliably: park on `/admin/users`, open DevTools, then hard-refresh.

**Use the right tool.**
The Network tab is a poor instrument for React Query, because the entire purpose of the cache is to *not* make network calls.
An absent request is the success case, and the Network tab cannot distinguish "served from cache" from "never asked".
`ReactQueryDevtools` is already wired up in `main.tsx` and shows as a button at the bottom-left in dev.
Open it and you can see the `["auth","me"]` entry, whether it is fresh or stale, its data, and when it was last updated.
That is where you watch the cache think.

### 9.12 A cache entry appears even when the query never runs

Reload a protected URL like `/admin/users` while **logged out** and the devtools will show an `["auth","me"]` entry, coloured yellow (stale), that never produces a network request.
This looks broken. It is not.

Two separate facts combine:

**A cache entry is created by observation, not by fetching.**
The moment any component calls `useQuery` with a key, React Query builds a `Query` object for that key and registers an observer on it.
That happens before it decides whether to fetch, and it happens even if it decides not to.
The entry in the devtools means "someone is watching this key", not "a request was sent".

**`enabled: false` means the entry exists but `queryFn` is never called.**
`useMe` has `enabled: Boolean(token)`.
Logged out, the token is `null`, so the query is disabled and no request is ever made.

Why is `useMe()` called at all when you are logged out?
Look at `RequireAuth.tsx`: `useMe()` is on line 9, and the `if (!isAuthed) return <Navigate to="/login" />` is on line 11.
The Rules of Hooks forbid putting a hook after a conditional return, so the hook must run unconditionally, even on the render that is about to redirect you away.
See 9.9.
`enabled` exists precisely for this: **the hook call cannot be conditional, but the fetch can.**

The devtools label is what makes it confusing.
The installed version computes the label as `fetching -> inactive (no observers) -> paused -> stale -> fresh`, with **no "disabled" case**.
A disabled query has no data (so `isStale()` is true) and has one observer (so it is not inactive), which lands it on **"stale"**.
It is not stale-and-waiting-to-refetch; it is switched off.

Watch the whole sequence in the devtools and it makes sense:

1. `RequireAuth` mounts -> `["auth","me"]` appears, yellow "stale", zero requests.
2. `RequireAuth` returns `<Navigate to="/login">` and unmounts -> observers drop to 0 -> the entry turns grey **"inactive"**.
3. Five minutes later (`gcTime`) it is garbage collected and disappears.

**Confirming test:** reload on `/login` instead of `/admin/users`.
`/login` sits outside `RequireAuth`, so `useMe()` is never called and **no entry appears at all**.

Without `enabled`, the logged-out case would fire `GET /users/me` with no `Authorization` header, get a 401, and trip the response interceptor's `clear()` on every visit.
Harmless, but a wasted round trip and a pointless redirect race.

---

## 10. Conventions - the rules for writing new code here

1. **Every server read is a `useQuery`. Every server write is a `useMutation`.** No `useEffect` + `fetch`, ever.
2. **Every cache key comes from `lib/queryKeys.ts`.** Never inline an array.
3. **Every HTTP call goes through `lib/api/client.ts`.** Never import `axios` directly in a feature.
4. **Every user-facing error string goes through `describeApiError`.**
5. **Forms use react-hook-form + zod.** Derive the type with `z.infer`. No `useState` per field.
6. **No TypeScript `enum`.** Use `as const` + derived union.
7. **Feature folders own `api.ts` / `hooks.ts` / `schemas.ts` / `pages/` / `components/`.** Cross-feature imports hit the feature root, never `pages/`.
8. **Global client state goes in a Zustand store.** No Context, no Redux.
9. **Edit sections that hydrate from a query get `key={concurrencyStamp}`.** No `useEffect` state sync.
10. **Mutations that can 409 must invalidate the detail query in `onError`.**
11. **shadcn components in `components/ui/` are editable source.** No MUI, no Chakra.
12. **Run `npm run build` before declaring done.** `tsc -b` is stricter than the dev server.

---

## 11. What exists today

### Working

| Area | Detail |
| --- | --- |
| Auth | Login, register (Buyer/Seller only), JWT in Zustand + localStorage, `/users/me` revalidation |
| Routing | Public / authed split, user-type guards, role-based landing redirect, route-level error page |
| Admin - Users | List with client-side search, create dialog, delete dialog (self-delete blocked), detail page |
| Admin - User detail | Assign roles, grant direct permissions, both concurrency-checked |
| Admin - Roles | List, create dialog, delete dialog, rename, attach permissions |
| Permissions | Read-only catalog, cached 5 min (it barely changes) |
| Errors | Typed `ApiError`, 401 auto-logout, 409 conflict handling with remount, toasts via sonner |
| Layouts | `PublicLayout` (centered card), `AdminLayout` (sidebar), `DashboardLayout` (top bar) |

### Placeholders

- `BuyerHomePage` - "Browse books" heading and a "coming later" line.
- `SellerHomePage` - same shape.
- `lib/types/book.ts` - a `Book` interface with no feature folder using it.

### Not built at all

Book listings, browse/search, cart, orders, rental lifecycle, returns, late fees, mock checkout UI, refresh tokens, pagination, tests, dark-mode toggle (tokens exist, no switch), permission-aware UI (the admin nav shows everything to every Employee regardless of their actual permissions).

---

## 12. Known gaps and inconsistencies

Honest list.
Fix opportunistically when you are already in the file; do not do a sweep unless asked.

1. **Root `CLAUDE.md` names a file that does not exist.**
   It refers to `lib/handleMutationError.ts`; the real file is `lib/errors/describeApiError.ts`, and it only builds a message - it does not toast or invalidate.
   Toasting is done by callers, invalidation by the hooks.
   The root doc should be corrected when it is next touched.

2. **`RolesTable` "Edit" is not actually disabled for SuperAdmin.**
   `<Button asChild disabled>` renders a `<Link>`, and `disabled` does nothing on an anchor.
   Neither the `disabled:opacity-50` style nor the click block applies, so the row looks and behaves as enabled.
   The Delete button next to it works correctly because it is a real `<button>`.
   Fix: conditionally render a plain disabled `<Button>` instead of the link, or drop the prop and rely on the server's guard rail.

3. **Form handling is inconsistent.**
   `LoginPage`, `RegisterPage` and `CreateUserDialog` use react-hook-form + zod; `RoleRenameSection` and `CreateRoleDialog` use plain `useState`.
   Rule 5 says RHF; these two predate it.

4. **`LoginPage` renders its error inline, `RegisterPage` uses `<FormField>`.**
   Same visual result, two different code paths.
   `LoginPage` should use `FormField`.

5. **The account-type `<select>` is a raw HTML element** with hand-written Tailwind that approximates `Input`'s styling.
   There is no shadcn `select.tsx` installed.
   It will not match `Input` exactly (focus ring, dark mode) under close inspection.

6. **Users list search is client-side only.**
   Fine for a demo, wrong past a few hundred users.
   Pagination is an open question in the root `CLAUDE.md`.

7. **No UI reacts to the user's actual permissions.**
   Any `Employee` sees the full admin sidebar; the server rejects what they may not do.
   The JWT already carries `role` and `perm` claims, so a `useHasPermission` hook is straightforward whenever it is wanted.

8. **`client/README.md` is still the untouched Vite template.**
   This file supersedes it.

9. **Project memory is empty on this machine.**
   The root `CLAUDE.md` points at memory notes (`project_rbac.md`, `feedback_client_patterns.md`, `project_api_review_2026-04-22.md`) that were written on the previous machine and did not travel.
   Treat this document plus the root `CLAUDE.md` changelog as the surviving record.

---

## 13. Quick self-test

If you can answer these without opening a file, you have the client back in your head.

1. Why does this app need both Zustand and React Query?
2. What breaks if you replace `selectIsAuthenticated` with an inline arrow that returns `{ token, user }`?
3. What is the difference between `staleTime` and `gcTime`?
4. Why is `queryKeys.users.all` `['users']` and not `['users','all']`?
5. Where does a 401 get handled, and how does the redirect happen without any component knowing?
6. Why does `UserEditPage` pass `key={...concurrencyStamp}` to its sections?
7. What is `concurrencyStamp` protecting against, and which HTTP status signals the failure?
8. Why can this project not use `enum UserType { Employee = 1 }`?
9. Why does the account-type select need `<Controller>` when the email input does not?
10. Are the route guards security? What is?
11. You click around the admin panel for five minutes and `/users/me` never appears in the Network tab. Why is that correct?
