const API_URL = '/api';

// Login Logic
const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;

        try {
            const res = await fetch(`${API_URL}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            if (res.ok) {
                const data = await res.json();
                localStorage.setItem('token', data.token);
                localStorage.setItem('role', data.role);
                localStorage.setItem('username', data.username);
                window.location.href = '/dashboard.html';
            } else {
                document.getElementById('errorMsg').innerText = 'Invalid credentials';
            }
        } catch (err) {
            document.getElementById('errorMsg').innerText = 'Server error';
        }
    });
}

// Dashboard Logic
async function loadDashboard() {
    const token = localStorage.getItem('token');
    const role = localStorage.getItem('role');
    const username = localStorage.getItem('username');

    if (!token) {
        window.location.href = '/index.html';
        return;
    }

    document.getElementById('userRoleBadge').innerText = role;

    // Display Username
    const nameDisplay = document.getElementById('userNameDisplay');
    if (nameDisplay && username) {
        nameDisplay.innerText = `Welcome, ${username}`;
    }

    // Logout
    document.getElementById('logoutBtn').addEventListener('click', () => {
        localStorage.clear();
        window.location.href = '/index.html';
    });

    const isAdmin = role === 'Admin';
    if (isAdmin) {
        document.getElementById('addBtn').classList.remove('hidden');
    }

    // Setup Table Headers based on role
    const tableHeader = document.getElementById('tableHeader');
    if (isAdmin) {
        tableHeader.innerHTML = `
            <th>Product Name</th>
            <th>Price</th>
            <th>Quantity</th>
            <th>Fresh?</th>
            <th>Actions</th>
        `;
    } else {
        tableHeader.innerHTML = `
            <th>Product Name</th>
            <th>Price</th>
            <th>Quantity</th>
        `;
    }

    await fetchProducts(token, isAdmin);

    // Modal UI binding
    if (isAdmin) {
        document.getElementById('addBtn').addEventListener('click', () => {
            document.getElementById('addProductModal').classList.remove('hidden');
        });
        document.getElementById('cancelBtn').addEventListener('click', () => {
            document.getElementById('addProductModal').classList.add('hidden');
        });

        document.getElementById('saveProductBtn').addEventListener('click', async () => {
            const payload = {
                name: document.getElementById('pName').value || "",
                price: parseFloat(document.getElementById('pPrice').value) || 0,
                stockQuantity: parseInt(document.getElementById('pStock').value) || 0
            };

            await fetch(`${API_URL}/Grocery/products`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify(payload)
            });

            document.getElementById('addProductModal').classList.add('hidden');
            await fetchProducts(token, isAdmin);
        });
    }
}

async function fetchProducts(token, isAdmin) {
    const res = await fetch(`${API_URL}/Grocery/products`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });

    if (res.status === 401) {
        window.location.href = '/index.html';
        return;
    }

    const products = await res.json();
    const tbody = document.getElementById('productsBody');
    tbody.innerHTML = '';

    products.forEach(p => {
        const tr = document.createElement('tr');
        if (isAdmin) {
            tr.innerHTML = `
                <td>${p.name}</td>
                <td>$${p.price}</td>
                <td>${p.stockQuantity}</td>
                <td>${p.isFresh ? 'Yes' : 'No'}</td>
                <td><button class="btn-secondary btn-sm" onclick="deleteProduct('${p.productId}')">Delete</button></td>
            `;
        } else {
            tr.innerHTML = `
                <td>${p.name}</td>
                <td>$${p.price}</td>
                <td>${p.quantity}</td>
            `;
        }
        tbody.appendChild(tr);
    });
}

window.deleteProduct = async (id) => {
    const token = localStorage.getItem('token');
    await fetch(`${API_URL}/Grocery/products/${id}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    await fetchProducts(token, true);
};
