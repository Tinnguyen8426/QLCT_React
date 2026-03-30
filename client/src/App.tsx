import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { useEffect } from 'react';
import { useAuthStore } from './stores/authStore';

// Layouts
import PublicLayout from './layouts/PublicLayout';
import AdminLayout from './layouts/AdminLayout';
import ProtectedRoute from './components/ProtectedRoute';

// Public Pages
import HomePage from './pages/public/HomePage';
import ServicesPage from './pages/public/ServicesPage';
import StorePage from './pages/public/StorePage';
import BookingPage from './pages/public/BookingPage';
import BookingSuccessPage from './pages/public/BookingSuccessPage';
import CartPage from './pages/public/CartPage';
import FeedbackPage from './pages/public/FeedbackPage';

// Auth Pages
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';

// Customer Pages
import CustomerDashboardPage from './pages/customer/DashboardPage';
import CustomerAppointmentsPage from './pages/customer/AppointmentsPage';
import CustomerInvoicesPage from './pages/customer/InvoicesPage';
import CustomerProductsPage from './pages/customer/ProductsPage';

// Admin Pages
import AdminDashboardPage from './pages/admin/DashboardPage';
import AdminAppointmentsPage from './pages/admin/AppointmentsPage';
import AdminCustomersPage from './pages/admin/CustomersPage';
import AdminServicesPage from './pages/admin/ServicesPage';
import AdminProductsPage from './pages/admin/ProductsPage';
import AdminInvoicesPage from './pages/admin/InvoicesPage';
import AdminUsersPage from './pages/admin/UsersPage';
import AdminReportsPage from './pages/admin/ReportsPage';

// Staff Pages
import StaffDashboardPage from './pages/staff/DashboardPage';
import StaffAppointmentsPage from './pages/staff/AppointmentsPage';

// Profile Page
import ProfilePage from './pages/profile/ProfilePage';

export default function App() {
  const { loadUser, isLoading } = useAuthStore();

  useEffect(() => { loadUser(); }, []);

  if (isLoading) return <div className="d-flex justify-content-center align-items-center vh-100"><span className="spinner-border text-light"></span></div>;

  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes with Layout */}
        <Route element={<PublicLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/services" element={<ServicesPage />} />
          <Route path="/store" element={<StorePage />} />
          <Route path="/feedback" element={<FeedbackPage />} />
          
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />

          {/* Customer / Shared Routes */}
          <Route path="/booking" element={<BookingPage />} />
          <Route path="/booking/success" element={<BookingSuccessPage />} />
          <Route path="/cart" element={<CartPage />} />

          <Route path="/profile" element={<ProtectedRoute><ProfilePage /></ProtectedRoute>} />
          
          <Route path="/customer/dashboard" element={<ProtectedRoute><CustomerDashboardPage /></ProtectedRoute>} />
          <Route path="/customer/appointments" element={<ProtectedRoute><CustomerAppointmentsPage /></ProtectedRoute>} />
          <Route path="/customer/invoices" element={<ProtectedRoute><CustomerInvoicesPage /></ProtectedRoute>} />
          <Route path="/customer/products" element={<ProtectedRoute><CustomerProductsPage /></ProtectedRoute>} />
        </Route>

        {/* Admin Routes with Layout */}
        <Route path="/admin" element={<ProtectedRoute role="Admin"><AdminLayout /></ProtectedRoute>}>
          <Route index element={<AdminDashboardPage />} />
          <Route path="appointments" element={<AdminAppointmentsPage />} />
          <Route path="customers" element={<AdminCustomersPage />} />
          <Route path="services" element={<AdminServicesPage />} />
          <Route path="products" element={<AdminProductsPage />} />
          <Route path="invoices" element={<AdminInvoicesPage />} />
          <Route path="users" element={<AdminUsersPage />} />
          <Route path="reports" element={<AdminReportsPage />} />
        </Route>

        {/* Staff Routes */}
        <Route path="/staff" element={<ProtectedRoute role="Staff"><AdminLayout /></ProtectedRoute>}>
          <Route index element={<StaffDashboardPage />} />
          <Route path="appointments" element={<StaffAppointmentsPage />} />
        </Route>

        {/* Fallback */}
        <Route path="*" element={<div className="text-center py-5 text-white"><h3>404 - Không tìm thấy trang</h3></div>} />
      </Routes>
    </BrowserRouter>
  );
}
