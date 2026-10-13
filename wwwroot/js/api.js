async function apiFetch(url, options = {}) {
    const token = localStorage.getItem('token');
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    if (token) {
        headers['Authorization'] = 'Bearer ' + token;
    }

    const res = await fetch(url, { ...options, headers });

    const text = await res.text();
    let data = null;
    if (text) {
        try { data = JSON.parse(text); } catch { data = text; }
    }

    // Token hết hạn hoặc không hợp lệ: xóa phiên và về trang đăng nhập
    if (res.status === 401 && token && !location.pathname.endsWith('/login.html')) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        location.href = '/login.html';
        throw new Error('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
    }

    if (!res.ok) {
        throw new Error(getErrorMessage(data, res.status));
    }
    return data;
}

function getErrorMessage(data, status) {
    if (data && data.errors) return Object.values(data.errors).flat().join(' | ');
    if (data && data.error) return data.error + (data.traceId ? ` (Mã lỗi: ${data.traceId})` : '');
    if (typeof data === 'string' && data) return data;
    if (status === 401) return 'Bạn cần đăng nhập để thực hiện chức năng này.';
    if (status === 403) return 'Bạn không có quyền thực hiện chức năng này.';
    return 'Có lỗi xảy ra (mã ' + status + ')';
}

function showAlert(message, type = 'success') {
    const box = document.getElementById('alertBox');
    box.innerHTML = `<div class="alert alert-${type} alert-dismissible fade show">
        ${esc(message)}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>`;
}

// Mã hóa ký tự đặc biệt khi chèn chữ do người dùng nhập vào HTML
function esc(s) {
    return String(s ?? '').replace(/[&<>"']/g, c =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}