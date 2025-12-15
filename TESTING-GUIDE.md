# Enterprise Home Assignment - Testing Guide

## Prerequisites
1. SQL Server LocalDB or SQL Server running
2. .NET 8 SDK installed
3. Connection string configured in `appsettings.json`

## Database Setup

### Apply Migrations
Run from the project directory:
```powershell
# If dotnet-ef tool is not installed
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update
```

## Testing Flow

### 1. Create Admin User
The system expects an admin user with email `admin@example.com`.

**Option A: Register via UI**
1. Navigate to `/Identity/Account/Register`
2. Register with email: `admin@example.com`
3. Password: (choose a strong password)
4. Confirm the account in the database:
   ```sql
   UPDATE AspNetUsers SET EmailConfirmed = 1 WHERE Email = 'admin@example.com'
   ```

**Option B: Seed Admin (Recommended for Development)**
Add this code to `Program.cs` after `var app = builder.Build();`:
```csharp
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var email = "admin@example.com";
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(u, "Admin@123!");
    }
}
```

### 2. Bulk Import Flow

#### Step 1: Upload JSON
1. Navigate to `/BulkImport/Upload`
2. Upload the sample JSON file (see sample below)
3. View the preview at `/BulkImport/Preview`

#### Step 2: Download ZIP Template
1. Click "Download ZIP" on the Preview page
2. Extract the ZIP - you'll see folders like:
   - `item-R-1001/default.jpg`
   - `item-R-1002/default.jpg`
   - `item-M-2001/default.jpg`
   - `item-M-2002/default.jpg`

#### Step 3: Replace Images (Optional)
1. Replace `default.jpg` in each folder with your own images
2. Keep the folder structure intact
3. Re-zip the folders

#### Step 4: Commit
1. On the Preview page, upload your modified ZIP
2. Click "Commit"
3. System will:
   - Extract images
   - Link MenuItem ? Restaurant relationships
   - Save to database
   - Redirect to `/Items/Catalog`

### 3. Approval Flow

#### Admin Approval (Restaurants)
1. Log in as `admin@example.com`
2. Navigate to `/Items/Verification`
3. You'll see pending restaurants
4. Select restaurants to approve
5. Click "Approve selected"

#### Owner Approval (Menu Items)
1. Log in as a restaurant owner (e.g., `luca.owner@example.com`)
2. Navigate to `/Items/Verification`
3. You'll see your restaurants
4. Click "View pending menu items" for a restaurant
5. Select menu items to approve
6. Click "Approve selected"

### 4. View Catalog
1. Navigate to `/Items/Catalog`
2. By default shows approved items in card view
3. Add `?view=row` for row view: `/Items/Catalog?view=row`

---

## Sample JSON Data

Save this as `sample-data.json`:

```json
[
  {
    "type": "restaurant",
    "id": "R-1001",
    "name": "Trattoria Luca",
    "description": "Pasta & grill with fresh daily specials",
    "ownerEmailAddress": "luca.owner@example.com",
    "address": "123 Harbor Road, Valletta",
    "phone": "+356 1234 5678"
  },
  {
    "type": "restaurant",
    "id": "R-1002",
    "name": "Sushi Wave",
    "description": "Classic nigiri and creative rolls",
    "ownerEmailAddress": "hana.owner@example.com",
    "address": "45 Marina Street, Sliema",
    "phone": "+356 9876 5432"
  },
  {
    "type": "menuitem",
    "id": "M-2001",
    "title": "Tagliatelle al Ragù",
    "price": 11.50,
    "currency": "EUR",
    "restaurantId": "R-1001"
  },
  {
    "type": "menuitem",
    "id": "M-2002",
    "title": "Ribeye 300g",
    "price": 24.00,
    "currency": "EUR",
    "restaurantId": "R-1001"
  },
  {
    "type": "menuitem",
    "id": "M-2003",
    "title": "Salmon Nigiri (6pcs)",
    "price": 8.50,
    "currency": "EUR",
    "restaurantId": "R-1002"
  },
  {
    "type": "menuitem",
    "id": "M-2004",
    "title": "Dragon Roll",
    "price": 12.00,
    "currency": "EUR",
    "restaurantId": "R-1002"
  }
]
```

---

## Troubleshooting

### Issue: ZIP Download Fails
**Cause**: `wwwroot/default.jpg` doesn't exist  
**Fix**: The system creates a placeholder automatically. If it still fails, manually create `wwwroot/default.jpg` with any small image.

### Issue: Commit Fails with FK Error
**Cause**: MenuItem ? Restaurant relationship not resolved  
**Fix**: Ensure `RestaurantExternalId` is populated in the factory and the Commit action matches relationships before calling `SaveAsync`.

### Issue: Catalog is Empty
**Causes**:
1. Items haven't been approved yet ? Approve via `/Items/Verification`
2. No items in database ? Complete the bulk import + commit flow
3. Database not migrated ? Run `dotnet ef database update`

### Issue: Login Required Errors
**Cause**: Not logged in  
**Fix**: Log in at `/Identity/Account/Login` or register at `/Identity/Account/Register`

### Issue: Approve Returns 403 Forbidden
**Causes**:
1. User email doesn't match validators
2. Logged in as wrong user (e.g., trying to approve someone else's menu items)  
**Fix**: Log in with the correct user account (admin for restaurants, owner for menu items)

---

## Architecture Summary

### Design Patterns
- **Factory Pattern**: `ImportItemFactory` creates `IItemValidating` instances
- **Repository Pattern**: `IItemsRepository` with in-memory and DB implementations
- **Keyed Services**: DI uses `[FromKeyedServices]` to inject correct repository

### Key Files
- **Models**: `Restaurant.cs`, `MenuItem.cs`, `IItemValidating.cs`
- **Repositories**: `ItemsInMemoryRepository.cs`, `ItemsDbRepository.cs`
- **Controllers**: `BulkImportController.cs`, `ItemsController.cs`, `OwnerController.cs`
- **Views**: 
  - `/Views/BulkImport/Upload.cshtml`, `Preview.cshtml`
  - `/Views/Items/Catalog.cshtml`, `VerifyRestaurants.cshtml`, `VerifyOwner.cshtml`
  - `/Views/Owner/VerifyRestaurantItems.cshtml`
  - `/Views/Shared/_RestaurantCard.cshtml`, `_MenuItemRow.cshtml`

---

## Assignment Compliance Checklist

### SE1.3 — Enterprise Standards (10 marks)
- [x] **a)** Two models with EF annotations: `Restaurant` (int Id), `MenuItem` (Guid Id)
- [x] **b)** `IItemValidating` interface with `GetValidators()` and `GetCardPartial()`
  - [x] Restaurant ? site admin
  - [x] MenuItem ? restaurant owner
- [x] **c)** Single `Catalog.cshtml` using `IEnumerable<IItemValidating>` with query-string toggle
- [ ] **d)** Catalog with approve + multi-select (not yet implemented)

### AA2.3 — Design Patterns (7 marks)
- [x] **1)** `BulkImportController` with Factory pattern (`ImportItemFactory`)
- [x] **2)** Two repositories (`ItemsInMemoryRepository`, `ItemsDbRepository`) with keyed services

### AA4.3 — Uploads & Storage (7 marks)
- [x] **3)** ZIP generation with folders per item
- [x] **4)** Commit action: extract ZIP, save images, link to items
- [x] **5)** Call `ItemsDbRepository.SaveAsync()`
- [x] **6)** Catalog shows approved restaurants/menu items

### SE3.3 — Security (10 marks)
- [x] **1)** Login + Registration (Identity)
- [x] **2)** Verification action (admin ? restaurants, owner ? menu items)
- [x] **3)** Approve action with validator check (returns 403 if unauthorized)

---

## Next Steps for Full Compliance

1. **Integrate Approve into Catalog (SE1.3.d)**: Add multi-select checkboxes and approve functionality directly in `Catalog.cshtml` when showing pending items.

2. **Deploy to myasp.net (AA3.2)**: Follow hosting provider's instructions to deploy the app.

3. **Testing**: Verify all flows work end-to-end with the sample data provided.
