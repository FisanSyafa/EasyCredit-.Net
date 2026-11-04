-- ============================================================
-- PostgreSQL Schema & Seed Data for EasyCredit (Sistem Baru)
-- ============================================================

-- 1. Table MST_USER
CREATE TABLE IF NOT EXISTS mst_user (
    user_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) NOT NULL, -- ADMIN | STAFF
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 2. Table MST_CUSTOMER
CREATE TABLE IF NOT EXISTS mst_customer (
    customer_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    nik VARCHAR(20) NOT NULL UNIQUE,
    phone VARCHAR(20),
    email VARCHAR(100),
    address VARCHAR(255),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 3. Seed Users
-- Password admin: admin123
-- Password staff: staff123
INSERT INTO mst_user (name, email, password_hash, role, created_at)
VALUES 
    ('Admin EasyCredit', 'admin@easycredit.test', '$2a$11$68uFwF2g1e2E6iC.O5K.Z.dGZt9o2T9w2q2w2m6t8v.O2hW4K9nK.', 'ADMIN', NOW()),
    ('Staff EasyCredit', 'staff@easycredit.test', '$2a$11$68uFwF2g1e2E6iC.O5K.Z.dGZt9o2T9w2q2w2m6t8v.O2hW4K9nK.', 'STAFF', NOW())
ON CONFLICT (email) DO NOTHING;

-- 4. Seed Sample Customers
INSERT INTO mst_customer (name, nik, phone, email, address, created_at)
VALUES
    ('Budi Santoso', '3171012345670001', '081234567890', 'budi.santoso@example.com', 'Jl. Sudirman No. 12, Jakarta', NOW()),
    ('Siti Aminah', '3171012345670002', '081234567891', 'siti.aminah@example.com', 'Jl. Thamrin No. 45, Jakarta', NOW()),
    ('Ahmad Dahlan', '3273012345670003', '081234567892', 'ahmad.dahlan@example.com', 'Jl. Asia Afrika No. 8, Bandung', NOW()),
    ('Dewi Lestari', '3374012345670004', '081234567893', 'dewi.lestari@example.com', 'Jl. Pemuda No. 22, Semarang', NOW())
ON CONFLICT (nik) DO NOTHING;

