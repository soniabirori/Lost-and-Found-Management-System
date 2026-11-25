# 🔎 Lost & Found Management System

A web application that helps a community reunite people with the things they've lost.
Users can report lost or found items with a photo, search and filter everything that has been posted,
privately message the owner or finder, and mark an item as returned once it's back home.

Built with **ASP.NET Core Razor Pages**, **ADO.NET** and **SQL Server**.

---

## ✨ Features

| Requirement | What the app does |
|---|---|
| **User registration & login** | Create an account and log in securely. Passwords are hashed (PBKDF2 + salt) and never stored in plain text. Sessions use secure cookie authentication. |
| **Report lost items** | Post a lost item with title, category, description, location and date. |
| **Report found items** | Post a found item the same way, so the owner can find it. |
| **Upload item photos** | Attach a photo (JPG, PNG, GIF, WEBP, max 5 MB) to any report, and replace it later. |
| **Search & filter** | Search by keyword (title, description, location) and filter by type, category and status. |
| **Contact item owners** | Send a private message from the item page. Replies appear in **My Messages**, together with the other person's contact details. |
| **Update status when returned** | Mark an item as **Returned** (or reopen it) from **My Reports**. Returned items are hidden from the default search and can no longer be messaged about. |

**Extras**
- ✏️ Edit and 🗑️ delete your own reports. Deleting also removes the photo file and related messages, inside a database transaction.
- 📊 Home page with **live statistics** (items reunited, open lost and found items, members) and the latest reports.
- 🎨 Custom design: a "Forest & Sage" theme, responsive on mobile, item cards with hover effects, a FAQ accordion.
- ✅ Custom confirmation popups instead of the plain browser dialogs.
- 🔒 Users can only edit, delete or change the status of **their own** items.

---

## 🛠️ Tech stack

- **Framework:** ASP.NET Core Razor Pages (.NET 10)
- **Language:** C#
- **Database:** Microsoft SQL Server Express
- **Data access:** ADO.NET with `Microsoft.Data.SqlClient` (parameterised queries, no Entity Framework)
- **Authentication:** ASP.NET Core cookie authentication + PBKDF2 password hashing
- **Front end:** Bootstrap 5, Bootstrap Icons, custom CSS, Google Font *Outfit*
- **Tools:** Visual Studio, SQL Server Management Studio (SSMS), Git & GitHub

---

## 🗄️ Database design

Three tables in the `LostAndFoundDB` database:

```
Users                     Items                          Messages
─────                     ─────                          ────────
Id (PK)          ◄──┐     Id (PK)              ◄──┐      Id (PK)
FullName            ├──── UserId (FK)             ├───── ItemId (FK)
Email (unique)      │     ItemType  (Lost/Found)  │      SenderId (FK → Users)
Phone               │     Title                   │      ReceiverId (FK → Users)
PasswordHash        │     Description             │      Body
CreatedAt           │     Category                │      SentAt
                    │     Location                │
                    │     EventDate               │
                    │     PhotoPath               │
                    │     Status (Open/Returned)  │
                    │     CreatedAt               │
                    └─────────────────────────────┘
```

- One **user** can report many **items**.
- One **item** can have many **messages** between users.
- `CHECK` constraints make sure `ItemType` is only *Lost/Found* and `Status` is only *Open/Returned*.

The full script is in [`LostAndFoundSystem/Database/CreateTables.sql`](LostAndFoundSystem/Database/CreateTables.sql).

---

## 🚀 Getting started

### Prerequisites
- Visual Studio with the **ASP.NET and web development** workload and the **.NET 10 SDK**
- **SQL Server Express** (instance name `SQLEXPRESS`)
- **SQL Server Management Studio (SSMS)**

### 1. Clone the repository
In Visual Studio: **File → Clone Repository**, then paste:
```
https://github.com/soniabirori/Lost-and-Found-Management-System.git
```

### 2. Create the database
1. Open **SSMS** and connect to `.\SQLEXPRESS`.
2. Create a new database named **`LostAndFoundDB`**.
3. Open `LostAndFoundSystem/Database/CreateTables.sql`, make sure `LostAndFoundDB` is selected, and press **F5**.

### 3. Check the connection string
In `LostAndFoundSystem/appsettings.json`:
```json
"ConnectionStrings": {
  "LostAndFoundDB": "Server=.\\SQLEXPRESS;Database=LostAndFoundDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```
If your SQL Server instance has a different name, change `.\\SQLEXPRESS` to match.

### 4. Run the app
Press **Ctrl + F5** in Visual Studio. If asked to trust the development SSL certificate, click **Yes**.

### 5. Try it out
1. **Register** two accounts (use a private/incognito window for the second one).
2. As user A, **report** a found item with a photo.
3. As user B, **search** for it, open it and **send a message**.
4. As user A, open **Messages** and **reply**.
5. As user A, go to **My Reports** and **mark it as returned**. 🎉

---

## 📁 Project structure

```
LostAndFoundSystem/
├── Database/
│   └── CreateTables.sql        # Script that creates the 3 tables
├── Helpers/
│   ├── PasswordHelper.cs       # PBKDF2 password hashing & verification
│   └── PhotoHelper.cs          # Validate, save and delete uploaded photos
├── Models/
│   └── ItemInfo.cs             # Shared item model + reusable SELECT query
├── Pages/
│   ├── Account/                # Register, Login, Logout
│   ├── Items/
│   │   ├── Index               # Browse, search & filter
│   │   ├── Details             # Item details + contact form
│   │   ├── Report              # Report a lost/found item
│   │   ├── MyReports           # Manage your reports (status, edit, delete)
│   │   └── Edit                # Edit a report
│   ├── Messages/
│   │   └── Index               # Inbox, sent messages and replies
│   ├── Shared/
│   │   ├── _Layout.cshtml      # Header, footer, confirm popup
│   │   └── _ItemCard.cshtml    # Reusable item card
│   └── Index                   # Home page
├── wwwroot/
│   ├── css/site.css            # Theme
│   ├── js/site.js              # Confirm popup & auto-hiding alerts
│   └── uploads/                # Uploaded photos (not committed to Git)
├── appsettings.json            # Connection string
└── Program.cs                  # App setup: Razor Pages + cookie authentication
```

---

## 🔐 Security

- **Password hashing:** PBKDF2 (SHA-256, 100,000 iterations) with a random salt per user, compared in constant time.
- **SQL injection protection:** every query uses parameters (`@Email`, `@Id`, and so on); user input is never glued straight into SQL.
- **Authorization:** reporting, messaging and managing items require login (`[Authorize]`).
- **Ownership checks:** every edit, delete and status update includes `WHERE UserId = @UserId`, so users can only change their own items.
- **Anti-forgery tokens:** all forms are protected against CSRF automatically by Razor Pages.
- **Upload validation:** only image file types up to 5 MB are accepted, saved under random names (GUIDs).
- **Privacy:** contact details are never shown on public item pages, only to people you exchange messages with.

---

## ⚠️ Troubleshooting

| Problem | Fix |
|---|---|
| `Cannot open database "LostAndFoundDB"` | Create the database and run `CreateTables.sql` (step 2). |
| `A network-related or instance-specific error` | Check the server name in `appsettings.json` matches your SQL Server instance. |
| `An Application Control policy has blocked this file (0x800711C7)` | Windows Smart App Control is blocking the build. Try **Build → Clean**, then **Rebuild**. If it continues, Smart App Control must be turned off (Windows Security → App & browser control). |
| Styles look old after an update | Press **Ctrl + Shift + R** in the browser to reload without the cache. |

---

## 👥 Team

| Member | Area |
|---|---|
| [Name] (Team leader) | Architecture & security |
| [Name] | Database design |
| [Name] | Accounts & reporting items |
| [Name] | Browse, search & home page |
| [Name] | Messages & My Reports |

---

*Developed as a group project. Inspired by the "Step by Step CRUD in ASP.NET Core Web Application" tutorial by Gwiza Erick.*
