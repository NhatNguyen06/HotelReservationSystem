function saveSession(data) {
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify({ fullName: data.fullName, role: data.role }));
}

function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    location.href = '/';
}

// Trang chủ theo vai trò (theo UC-002)
function homeFor(role) {
    if (role === 'Staff' || role === 'Manager') return '/staff-rooms.html';
    if (role === 'Admin') return '/admin-users.html';
    return '/';
}

// Chặn vào trang nếu chưa đăng nhập hoặc sai vai trò. Trả về true nếu được phép
function requireRole(roles) {
    const user = getUser();
    if (!user) {
        location.href = '/login.html';
        return false;
    }
    if (!roles.includes(user.role)) {
        alert('Bạn không có quyền truy cập trang này.');
        location.href = homeFor(user.role);
        return false;
    }
    return true;
}

function renderNavbar() {
    const user = getUser();

    let links = '<a class="nav-link" href="/">Tìm phòng</a>';
    if (user && user.role === 'Customer') {
        links += '<a class="nav-link" href="/my-bookings.html">Booking của tôi</a>';
        links += '<a class="nav-link" href="/payment-history.html">Lịch sử thanh toán</a>';
    }
    if (user && (user.role === 'Staff' || user.role === 'Manager')) {
        links += '<a class="nav-link" href="/staff-rooms.html">Quản lý phòng</a>';
        links += '<a class="nav-link" href="/staff-payments.html">Thanh toán</a>';
        links += '<a class="nav-link" href="/staff-reservations.html">Đặt phòng</a>';
    }
    if (user && user.role === 'Manager') {
        links += '<a class="nav-link" href="/manager.html">Báo cáo & giá</a>';
    }
    if (user && user.role === 'Admin') {
        links += '<a class="nav-link" href="/admin-users.html">Quản lý người dùng</a>';
    }

    const right = user
        ? `<span class="navbar-text me-3">${esc(user.fullName)} (${esc(user.role)})</span>
           <a class="btn btn-outline-light btn-sm me-2" href="/profile.html">Hồ sơ</a>
           <button class="btn btn-light btn-sm" onclick="logout()">Đăng xuất</button>`
        : `<a class="btn btn-outline-light btn-sm me-2" href="/login.html">Đăng nhập</a>
           <a class="btn btn-light btn-sm" href="/register.html">Đăng ký</a>`;

    document.getElementById('navbar').innerHTML = `
        <nav class="navbar navbar-expand navbar-dark bg-dark mb-4">
            <div class="container">
                <a class="navbar-brand" href="/">Hotel Reservation System</a>
                <div class="navbar-nav me-auto">${links}</div>
                <div class="d-flex align-items-center">${right}</div>
            </div>
        </nav>`;
}