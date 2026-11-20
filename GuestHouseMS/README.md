#  Guest House Management System (WinForms + SQLite, MVC)

Lab 3 – desktop implementation of the Guest House Management System.
C# / .NET 8 Windows Forms, SQLite database, MVC architecture.

## Run it
1. Install **Visual Studio 2022** (workload ".NET desktop development") or the **.NET 8 SDK**.
2. Open `GuestHouseMS.csproj` in Visual Studio and press **F5**
   (or run `dotnet run` in this folder). NuGet restores `Microsoft.Data.Sqlite` automatically.
3. On first start the database file `guesthouse.db` is created next to the .exe with all tables,
   8 demo rooms and 3 demo users.

| Username     | Password       | Role         | Can access                                   |
|--------------|----------------|--------------|----------------------------------------------|
| admin        | admin123       | Admin        | Everything (also Users, room add/edit/delete)|
| reception    | reception123   | Receptionist | Rooms (view/search), Guests, Reservations, Billing, Reports |
| housekeeping | clean123       | Housekeeping | Housekeeping only                            |

## MVC structure
```
Models/        Plain classes: User, Room, Guest, Reservation, Result + status constants
Views/         WinForms screens (LoginForm, MainForm, RoomsView, GuestsView, ReservationsView,
               BillingView, HousekeepingView, ReportsView, UsersView). Views contain NO SQL;
               they only read inputs, call a controller and display the result.
Controllers/   Business rules + database operations (validation, double-booking check,
               check-in/out rules, billing, reports).
Data/          Db.cs (parameterised SQLite helper + schema + seed data), Security.cs (password hashing)
Program.cs     Starts the app: Login -> MainForm -> logout loop
```
Flow: **View -> Controller -> Database -> Controller -> View**.

## Database tables
Users, Rooms, Guests, Reservations, Charges, Payments, Maintenance
(Reservations link Guests and Rooms; Charges and Payments link to Reservations).

## Features covered (from the requirements document)
- Login, role-based access, admin user management, change password, 15-minute session timeout
- Rooms: add/update/delete, types, capacity, price, status, availability search by dates + guests
- Guests: register, search, update, delete, stay history
- Reservations: create, cancel (auto refund record), extend/shorten stay, **double-booking prevention**
- Check-in / check-out with automatic room status changes (Occupied -> Needs Cleaning)
- Billing: nights x rate + extra services, partial payments (cash/card/mobile money/bank), balance,
  printable receipt/invoice; check-out blocked while a balance is due
- Housekeeping: clean/dirty status, maintenance issue log + resolve
- Reports: arrivals/departures, occupancy, revenue (daily/weekly/monthly/yearly), outstanding
  balances, all bookings; export to CSV (opens in Excel)

## Security notes
Passwords are salted + hashed (PBKDF2-SHA256), every SQL statement is parameterised.

## Upload to GitHub
```
cd GuestHouseMS
git init
git add .
git commit -m "Lab 3: Guest House Management System (WinForms, SQLite, MVC)"
git branch -M main
git remote add origin https://github.com/<your-username>/GuestHouseMS.git
git push -u origin main
```
(Create the empty repository on github.com first, then paste its URL above.)
