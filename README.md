# QrOrderingSuite v2 – Multi-Project Demo-Ready (PostgreSQL, .NET 8, Blazor Server)

Projects:
- **Infrastructure**: EF Core models, DbContext, initial migration, seeding.
- **GuestApp**: QR menu, cart & checkout, place order, demo payment.
- **AdminApp**: Login (cookie, configurable), menu & tables CRUD, QR generator, orders board.
- **KdsApp**: Kitchen display (polling), status updates (New → InProgress → Ready).

## Connection string
All apps use:
```
Host=localhost;Port=5432;Database=qr_ordering;Username=postgres;Password=$amir001
```

## Run (first time)
Open three terminals:
```bash
dotnet run --project GuestApp   # applies migrations, seeds demo
dotnet run --project AdminApp
dotnet run --project KdsApp
```
Then open:
- Guest: https://localhost:5001/t/demo/T1  (add items → Checkout → Place Order → Pay demo)
- Admin: https://localhost:5002 (login with Email: admin@demo.com, Password: Admin@123)
- KDS:   https://localhost:5003 (auto-refreshing board)

> If ports differ, see console output for actual URLs.

## Notes
- Multi-tenant ready (multiple restaurants seeded: `demo`, `bistro`). Admin selects a restaurant from dropdowns.
- Demo Payment: marks orders as Paid; replace later with Stripe/Payme provider.
- QR generator: Admin → Tables shows QR PNGs (data URLs) you can download (right-click Save Image).

## Next steps for production
- Real payment provider + webhooks
- Role-based users per restaurant
- Printer integration or auto-print agent
- Logging (Serilog) + error pages
- Docker compose for PG + apps
