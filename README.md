# MarketLine Dashboard

ASP.NET Core MVC (.NET 8) project matching the dashboard mockup:
green/red/blue/purple summary cards, dark navy top nav with a
hamburger menu on mobile, "Recent Sales" glass panel with a sparkline,
and the "GO TO SALE" pill button.

## Project layout

```
MarketLine/
├── Controllers/
│   └── DashboardController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── Sale.cs
│   └── DashboardViewModel.cs
├── Views/
│   ├── Dashboard/Index.cshtml
│   ├── Shared/_Layout.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/site.css
│   └── js/site.js
├── Program.cs
├── appsettings.json
└── MarketLine.csproj
```

## 1. Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine for dev) — or swap to SQLite, see below
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef` (skip if already installed)

## 2. Restore packages

```bash
cd MarketLine
dotnet restore
```

## 3. New: "Add Goods" gallery

`/Goods` is a product gallery wired to the "Add Goods" nav link. It:

- Shows a "PRESTIGE / SOFT DRINK STORE" header (edit the text directly in
  `Views/Goods/Index.cshtml` if your store name differs).
- Lists every `Product` from the database as a card (image, title, price,
  small edit/delete icon buttons — no button labels, per the mockup).
- "Add New Item" opens a modal to add a product (name, price, optional
  image). No page reload — it POSTs to `/Goods/Create` and the new card
  is appended via JS.
- The pencil icon opens the same modal pre-filled for editing
  (`/Goods/Edit`); the trash icon opens a **confirmation modal** first,
  and only calls `/Goods/Delete` if you confirm.
- Deleting removes the row from the database **and** the uploaded image
  file from `wwwroot/uploads/goods`.
- Products are a normal EF Core table (`Products`), so any other page
  (e.g. a future "Add Sale" form) can query the same list.

Uploaded images are saved to `wwwroot/uploads/goods/` with a generated
GUID filename; only jpg/jpeg/png/webp/gif up to 5 MB are accepted.

## 4. Set up the database

The connection string lives in `appsettings.json` under
`ConnectionStrings:DefaultConnection`. The default targets LocalDB —
change it if you're pointing at a real SQL Server instance:

```json
"DefaultConnection": "Server=YOUR_SERVER;Database=MarketLineDb;User Id=...;Password=...;TrustServerCertificate=True"
```

Then create and apply the migration (this also seeds the 6 sample
sales rows used to build the "Highest / Lowest / Total / Recent
Sales" figures, and creates the `Products` table used by `/Goods`):

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

If you already ran a migration before pulling in the Goods feature,
just add a second one on top instead of starting over:

```bash
dotnet ef migrations add AddProducts
dotnet ef database update
```

### Prefer SQLite instead of SQL Server?

1. `dotnet add package Microsoft.EntityFrameworkCore.Sqlite`
2. In `Program.cs` change `options.UseSqlServer(...)` to
   `options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))`
3. In `appsettings.json` change the connection string to
   `"Data Source=marketline.db"`

## 4. Run it

```bash
dotnet run
```

Browse to the URL shown in the console (e.g. `https://localhost:5001`).
The Dashboard is the home page (`/`), wired via the default route in
`Program.cs`.

## Notes on the pieces you asked for

- **Controller**: `DashboardController.Index()` reads `Sales` from the
  DB, computes Highest/Lowest/Total, and passes the top 4 most recent
  rows + a `DashboardViewModel` to the view.
- **Model**: `Sale` (EF entity) and `DashboardViewModel` (what the view
  binds to).
- **View**: `Views/Dashboard/Index.cshtml` renders the 4 summary
  cards, the Recent Sales list with avatar initials, and the sparkline
  (inline SVG, no chart library needed).
- **CSS**: `wwwroot/css/site.css` — all colors are CSS variables at the
  top (`--green-1/2`, `--red-1/2`, `--blue-1/2`, `--purple-1/2`,
  `--navy-900`, `--gold`) so you can retune the palette in one place
  without touching markup.
- **Hamburger / mobile**: below 720px the nav links collapse into a
  dropdown toggled by `#mlHamburger` (see `site.js`); the card grid
  drops from 4 → 2 columns below 900px, and the sparkline hides on
  small screens since there's no room for it.
- **Database**: `ApplicationDbContext` + EF Core migrations, seeded
  with the same customers/amounts shown in the mockup (John Smith,
  Sara Williams, Michael Brown, Emily Johnson, plus two more so
  Highest/Lowest match the $15,230 / $1,250 in the screenshot).

## Extending it

- Add a `SalesController` with a `Create` action for the "Add Sales"
  nav link and the "GO TO SALE" button (both already point to
  `/Sales/Create`).
- Add a `CustomersController` for "Customers Record".
- Wrap the bell icon's notification count in a real notifications
  table if you want it to be dynamic instead of hardcoded to 3.
