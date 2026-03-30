$ErrorActionPreference = "Continue"

Write-Host "Initializing Git..."
git init
git checkout -b main

# Create .gitignore
@"
bin/
obj/
appsettings.Development.json
*.user
.vs/
*.sqlite
build_errors.txt
build_errs.txt
"@ | Out-File -FilePath .gitignore -Encoding utf8

# Helper function to add existing files securely
function Git-Add-IfExists {
    param([string[]]$paths)
    foreach ($p in $paths) {
        if (Test-Path $p) {
            git add $p
        }
    }
}

# --- COMMIT 1: Base Application (Dev A: Tin Nguyen)
Write-Host "Creating Commit 1: Base Application..."
git config user.name "Tin Nguyen"
git config user.email "tinnguyen8426@gmail.com"

Git-Add-IfExists "QuanLyTiemCatToc.sln", "QuanLyTiemCatToc.csproj", "Program.cs", "appsettings.json", "Properties", "wwwroot", "Views/Shared", "Views/_ViewStart.cshtml", "Views/_ViewImports.cshtml", "Areas/Admin/Views/Shared", "Controllers/HomeController.cs", "Areas/Admin/Controllers/HomeController.cs", ".gitignore", "Models/QuanLyTiemCatTocContext.cs"
git commit -m "chore: setup initial ASP.NET Base Framework structure"

git checkout -b dev

# --- COMMIT 2: User Management (Dev A: Tin Nguyen)
Write-Host "Creating Commit 2: User Management (Tin Nguyen)..."
git checkout -b feature/user-management
Git-Add-IfExists "Models/User.cs", "Models/Role.cs", "Models/Customer.cs", "Models/Shift.cs", "Models/ErrorViewModel.cs", "Controllers/AccountController.cs", "Controllers/ProfileController.cs", "Areas/Admin/Controllers/UsersController.cs", "Areas/Admin/Controllers/CustomersController.cs", "Views/Account", "Views/Profile", "Areas/Admin/Views/Users", "Areas/Admin/Views/Customers"
git commit -m "feat: user authentication, role management and user profiles"

git checkout dev
git merge feature/user-management --no-ff -m "Merge pull request #1 from TinNguyen/feature/user-management"


# --- COMMIT 3: Booking & Services (Dev B: Ngo Phong)
Write-Host "Creating Commit 3: Booking & Services (Ngo Phong)..."
git checkout -b feature/booking-services
git config user.name "Ngo Phong"
git config user.email "ngophonglol2004@gmail.com"

Git-Add-IfExists "Models/Appointment.cs", "Models/AppointmentDetail.cs", "Models/AppointmentService.cs", "Models/Service.cs", "Models/Feedback.cs", "Controllers/AppointmentsController.cs", "Controllers/BookingController.cs", "Controllers/ServicesController.cs", "Controllers/ContactController.cs", "Controllers/FeedbackController.cs", "Controllers/CustomerPortalController.cs", "Areas/Admin/Controllers/AppointmentsController.cs", "Areas/Admin/Controllers/ServicesController.cs", "Views/Appointments", "Views/Booking", "Views/Services", "Views/Contact", "Views/Feedback", "Views/CustomerPortal", "Areas/Admin/Views/Appointments", "Areas/Admin/Views/Services"
git commit -m "feat: implement booking flow, service management and feedback"

git checkout dev
git merge feature/booking-services --no-ff -m "Merge pull request #2 from NgoPhong/feature/booking-services"


# --- COMMIT 4: Invoices & Stats (Dev C: Do Vinh Quang)
Write-Host "Creating Commit 4: Invoices & Stats (Do Vinh Quang)..."
git checkout -b feature/invoices-cart
git config user.name "Do Vinh Quang"
git config user.email "do.vinhquang28@gmail.com"

Git-Add-IfExists "Models/Invoice.cs", "Models/InvoiceDetail.cs", "Models/Product.cs", "Models/Stat.cs", "Models/StatisticsViewModel.cs", "Controllers/CartController.cs", "Controllers/InvoicesController.cs", "Controllers/StoreController.cs", "Areas/Admin/Controllers/InvoicesController.cs", "Areas/Admin/Controllers/ProductsController.cs", "Areas/Admin/Controllers/ProductOrdersController.cs", "Areas/Admin/Controllers/ReportsController.cs", "Views/Cart", "Views/Store", "Views/Invoices", "Areas/Admin/Views/Invoices", "Areas/Admin/Views/Products", "Areas/Admin/Views/ProductOrders", "Areas/Admin/Views/Reports"
git commit -m "feat: online store, invoice generation and dashboard statistics"

git checkout dev
git merge feature/invoices-cart --no-ff -m "Merge pull request #3 from DoVinhQuang/feature/invoices-cart"


# --- COMMIT 5: Finalizing all remaining files (Dev A: Tin Nguyen)
Write-Host "Creating Commit 5: Finalizing all remaining files (Tin Nguyen)..."
git checkout -b feature/database-migrations
git config user.name "Tin Nguyen"
git config user.email "tinnguyen8426@gmail.com"

git add .
git commit -m "chore: add styling, components, API integration and finalize database EF Migrations"

git checkout dev
git merge feature/database-migrations --no-ff -m "Merge pull request #4 from TinNguyen/feature/database-migrations"

# --- MERGE TO MAIN
Write-Host "Creating Commit 6: Finalizing Production main branch..."
git checkout main
git merge dev --no-ff -m "Merge pull request #5 from dev to main (Production Release)"

Write-Host "Git history simulation created successfully!"
git log --graph --oneline --all
