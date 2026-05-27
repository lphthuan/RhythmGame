require('dotenv').config();
const express = require('express');
const cors = require('cors');
const bodyParser = require('body-parser');
const crypto = require('crypto');
const axios = require('axios');
const moment = require('moment');
const qs = require('qs');

const app = express();
app.use(cors());
app.use(bodyParser.json());
app.use(bodyParser.urlencoded({ extended: true }));

const PORT = process.env.PORT || 5000;

// Fake database in-memory
let userBalance = 0;
const transactions = [];

const PACKAGES = [
    { id: 'p0', name: 'Gói Tân Thủ', price: 20000, amount: 200, description: '200 RC' },
    { id: 'p1', name: 'Gói Khởi Đầu', price: 50000, amount: 550, description: '500 RC + 50 Thưởng' },
    { id: 'p2', name: 'Gói Phổ Thông', price: 100000, amount: 1150, description: '1000 RC + 150 Thưởng' },
    { id: 'p3', name: 'Gói Chuyên Nghiệp', price: 200000, amount: 2400, description: '2000 RC + 400 Thưởng' },
    { id: 'p4', name: 'Gói Cao Cấp', price: 500000, amount: 6200, description: '5000 RC + 1200 Thưởng' },
    { id: 'p5', name: 'Gói Huyền Thoại', price: 1000000, amount: 13000, description: '10000 RC + 3000 Thưởng' },
];

const DEV_PACKAGES = [
    { id: 'dev_success', name: 'Dev: Vô Hạn Tiền', price: 0, amount: 99999999, description: 'Nạp thành công vô hạn tiền (Test)' },
    { id: 'dev_fail', name: 'Dev: Test Thất Bại', price: 0, amount: 0, description: 'Giả lập lỗi thanh toán (Test)' },
];

// --- Routes ---

app.get('/api/packages', (req, res) => {
    const isDev = req.query.dev === 'true';
    const hasBoughtFreeP0 = transactions.some(t => t.packageId === 'p0' && t.status === 'success');
    
    const dynamicPackages = PACKAGES.map(p => {
        if (p.id === 'p0') {
            return {
                ...p,
                price: hasBoughtFreeP0 ? p.price : 0,
                description: hasBoughtFreeP0 ? p.description : 'Nhận miễn phí lần đầu (100 Xu)'
            };
        }
        return p;
    });

    res.json({
        packages: dynamicPackages,
        devPackages: isDev ? DEV_PACKAGES : []
    });
});

app.get('/api/user/balance', (req, res) => {
    res.json({ balance: userBalance });
});

app.get('/api/user/transactions', (req, res) => {
    res.json({ transactions: transactions.slice().reverse() });
});

// --- Free Recharge logic ---
app.post('/api/payment/free', (req, res) => {
    const { packageId } = req.body;
    const pkg = PACKAGES.find(p => p.id === packageId);
    if (!pkg) return res.status(400).json({ message: 'Gói không hợp lệ' });

    if (packageId === 'p0') {
        const hasBoughtFreeP0 = transactions.some(t => t.packageId === 'p0' && t.status === 'success');
        if (hasBoughtFreeP0) {
            return res.status(400).json({ message: 'Bạn đã sử dụng lượt miễn phí cho gói này. Vui lòng thanh toán.' });
        }
    } else if (pkg.price !== 0) {
        return res.status(400).json({ message: 'Gói này không miễn phí' });
    }

    userBalance += pkg.amount;
    transactions.push({ orderId: 'FREE' + Date.now(), packageId, status: 'success', provider: 'free' });
    res.json({ success: true, message: 'Nhận gói miễn phí thành công!', balance: userBalance });
});

// --- VNPay Logic ---
app.post('/api/payment/vnpay', (req, res) => {
    process.env.TZ = 'Asia/Ho_Chi_Minh';
    const { packageId } = req.body;
    const pkg = PACKAGES.find(p => p.id === packageId);
    if (!pkg) return res.status(404).json({ message: 'Package not found' });

    let finalPrice = pkg.price;
    if (packageId === 'p0') {
        const hasBoughtFreeP0 = transactions.some(t => t.packageId === 'p0' && t.status === 'success');
        if (!hasBoughtFreeP0) {
            finalPrice = 0;
        }
    }

    if (finalPrice === 0) {
        userBalance += pkg.amount;
        transactions.push({ orderId: 'FREE_VNPAY' + Date.now(), packageId, status: 'success', provider: 'vnpay' });
        const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:5173';
        return res.json({ paymentUrl: `${frontendUrl}?paymentStatus=success` });
    }

    let date = new Date();
    let createDate = moment(date).format('YYYYMMDDHHmmss');
    
    let ipAddr = req.headers['x-forwarded-for'] ||
        req.connection.remoteAddress ||
        req.socket.remoteAddress ||
        req.connection.socket.remoteAddress;

    let tmnCode = process.env.VNP_TMN_CODE;
    let secretKey = process.env.VNP_HASH_SECRET;
    let vnpUrl = process.env.VNP_URL;
    let returnUrl = process.env.VNP_RETURN_URL;

    let orderId = moment(date).format('DDHHmmss');
    let amount = finalPrice;
    
    let vnp_Params = {};
    vnp_Params['vnp_Version'] = '2.1.0';
    vnp_Params['vnp_Command'] = 'pay';
    vnp_Params['vnp_TmnCode'] = tmnCode;
    vnp_Params['vnp_Locale'] = 'vn';
    vnp_Params['vnp_CurrCode'] = 'VND';
    vnp_Params['vnp_TxnRef'] = orderId;
    vnp_Params['vnp_OrderInfo'] = 'Thanh toan don hang ' + orderId;
    vnp_Params['vnp_OrderType'] = 'billpayment';
    vnp_Params['vnp_Amount'] = amount * 100;
    vnp_Params['vnp_ReturnUrl'] = returnUrl;
    vnp_Params['vnp_IpAddr'] = ipAddr;
    vnp_Params['vnp_CreateDate'] = createDate;

    vnp_Params = sortObject(vnp_Params);

    let signData = qs.stringify(vnp_Params, { encode: false });
    let hmac = crypto.createHmac("sha512", secretKey);
    let signed = hmac.update(Buffer.from(signData, 'utf-8')).digest("hex"); 
    vnp_Params['vnp_SecureHash'] = signed;
    vnpUrl += '?' + qs.stringify(vnp_Params, { encode: false });

    transactions.push({ orderId, packageId, status: 'pending', provider: 'vnpay' });
    res.json({ paymentUrl: vnpUrl });
});

app.get('/api/payment/vnpay_return', (req, res) => {
    console.log('--- VNPay Return Hit ---');
    let vnp_Params = req.query;
    console.log(vnp_Params);
    let secureHash = vnp_Params['vnp_SecureHash'];

    delete vnp_Params['vnp_SecureHash'];
    delete vnp_Params['vnp_SecureHashType'];

    vnp_Params = sortObject(vnp_Params);
    let secretKey = process.env.VNP_HASH_SECRET;
    let signData = qs.stringify(vnp_Params, { encode: false });
    let hmac = crypto.createHmac("sha512", secretKey);
    let signed = hmac.update(Buffer.from(signData, 'utf-8')).digest("hex");     

    const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:5173';

    if (secureHash === signed) {
        const responseCode = vnp_Params['vnp_ResponseCode'];
        const orderId = vnp_Params['vnp_TxnRef'];
        
        console.log(`Signature Match! OrderId: ${orderId}, ResponseCode: ${responseCode}`);

        if (responseCode === '00') {
            const trans = transactions.find(t => t.orderId === orderId);
            if (trans) {
                if (trans.status !== 'success') {
                    const pkg = PACKAGES.find(p => p.id === trans.packageId);
                    if (pkg) {
                        userBalance += pkg.amount;
                        trans.status = 'success';
                        console.log(`Balance updated: +${pkg.amount}. New Balance: ${userBalance}`);
                    }
                } else {
                    console.log('Order already successful.');
                }
            } else {
                console.log('Transaction not found in memory.');
            }
            res.redirect(`${frontendUrl}?paymentStatus=success`);
        } else {
            res.redirect(`${frontendUrl}?paymentStatus=failed&code=${responseCode}`);
        }
    } else {
        console.log('VNPay Signature Mismatch!');
        res.redirect(`${frontendUrl}?paymentStatus=error&message=InvalidSignature`);
    }
});

app.get('/api/payment/vnpay_ipn', (req, res) => {
    console.log('--- VNPay IPN Hit ---');
    let vnp_Params = req.query;
    let secureHash = vnp_Params['vnp_SecureHash'];

    let orderId = vnp_Params['vnp_TxnRef'];
    let rspCode = vnp_Params['vnp_ResponseCode'];

    delete vnp_Params['vnp_SecureHash'];
    delete vnp_Params['vnp_SecureHashType'];

    vnp_Params = sortObject(vnp_Params);
    let secretKey = process.env.VNP_HASH_SECRET;
    let signData = qs.stringify(vnp_Params, { encode: false });
    let hmac = crypto.createHmac("sha512", secretKey);
    let signed = hmac.update(Buffer.from(signData, 'utf-8')).digest("hex");     
    
    if (secureHash === signed) {
        const trans = transactions.find(t => t.orderId === orderId);
        if (trans) {
            const pkg = PACKAGES.find(p => p.id === trans.packageId);
            const amount = pkg ? (pkg.price > 0 ? pkg.price * 100 : 10000 * 100) : 0;

            if (parseInt(vnp_Params['vnp_Amount']) === amount) {
                if (trans.status !== 'success') {
                    if (rspCode === '00') {
                        // Thanh cong
                        userBalance += pkg.amount;
                        trans.status = 'success';
                        console.log(`IPN: Balance updated: +${pkg.amount}. New Balance: ${userBalance}`);
                        res.status(200).json({ RspCode: '00', Message: 'Success' });
                    } else {
                        // That bai
                        trans.status = 'failed';
                        res.status(200).json({ RspCode: '00', Message: 'Success' });
                    }
                } else {
                    res.status(200).json({ RspCode: '02', Message: 'Order already confirmed' });
                }
            } else {
                res.status(200).json({ RspCode: '04', Message: 'Invalid amount' });
            }
        } else {
            res.status(200).json({ RspCode: '01', Message: 'Order not found' });
        }
    } else {
        res.status(200).json({ RspCode: '97', Message: 'Invalid signature' });
    }
});

// --- MoMo Logic ---
app.get('/api/payment/momo_return', (req, res) => {
    console.log('--- MoMo Return Hit ---');
    const { orderId, resultCode, message } = req.query;
    console.log(req.query);
    
    const trans = transactions.find(t => t.orderId === orderId);
    const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:5173';

    if (resultCode == '0') {
        if (trans && trans.status !== 'success') {
            const pkg = PACKAGES.find(p => p.id === trans.packageId);
            if (pkg) {
                userBalance += pkg.amount;
                trans.status = 'success';
                console.log(`MoMo Return: Balance updated: +${pkg.amount}. New Balance: ${userBalance}`);
            }
        }
        res.redirect(`${frontendUrl}?paymentStatus=success`);
    } else {
        console.log(`MoMo Payment Failed: ${message}`);
        res.redirect(`${frontendUrl}?paymentStatus=failed&code=${resultCode}`);
    }
});
app.post(['/api/payment/momo', '/api/payment/momo_atm'], async (req, res) => {
    const isAtm = req.path.includes('momo_atm');
    const { packageId } = req.body;
    const pkg = PACKAGES.find(p => p.id === packageId);
    if (!pkg) return res.status(404).json({ message: 'Package not found' });

    let finalPrice = pkg.price;
    if (packageId === 'p0') {
        const hasBoughtFreeP0 = transactions.some(t => t.packageId === 'p0' && t.status === 'success');
        if (!hasBoughtFreeP0) {
            finalPrice = 0;
        }
    }

    if (finalPrice === 0) {
        userBalance += pkg.amount;
        transactions.push({ orderId: 'FREE_MOMO' + Date.now(), packageId, status: 'success', provider: isAtm ? 'momo_atm' : 'momo' });
        const frontendUrl = process.env.FRONTEND_URL || 'http://localhost:5173';
        return res.json({ paymentUrl: `${frontendUrl}?paymentStatus=success` });
    }

    const partnerCode = process.env.MOMO_PARTNER_CODE;
    const accessKey = process.env.MOMO_ACCESS_KEY;
    const secretKey = process.env.MOMO_SECRET_KEY;
    const orderId = 'MOMO' + Date.now();
    const requestId = orderId;
    const orderInfo = 'Pay for ' + pkg.id; 
    const redirectUrl = process.env.MOMO_REDIRECT_URL;
    const ipnUrl = process.env.MOMO_NOTIFY_URL;
    const amount = finalPrice; // API requires amount > 0, handled by finalPrice condition
    const requestType = isAtm ? "payWithATM" : "captureWallet";
    const extraData = ""; 

    // EXACT ORDER REQUIRED BY MOMO
    const rawSignature = `accessKey=${accessKey}&amount=${amount}&extraData=${extraData}&ipnUrl=${ipnUrl}&orderId=${orderId}&orderInfo=${orderInfo}&partnerCode=${partnerCode}&redirectUrl=${redirectUrl}&requestId=${requestId}&requestType=${requestType}`;
    
    console.log('--- Raw Signature String ---');
    console.log(rawSignature);

    const signature = crypto.createHmac('sha256', secretKey)
        .update(rawSignature)
        .digest('hex');

    const requestBody = {
        partnerCode,
        accessKey,
        requestId,
        amount,
        orderId,
        orderInfo,
        redirectUrl,
        ipnUrl,
        requestType,
        extraData,
        signature,
        lang: 'vi'
    };

    console.log('--- MoMo Request ---');
    console.log(JSON.stringify(requestBody, null, 2));

    try {
        const response = await axios.post(process.env.MOMO_ENDPOINT, requestBody, {
            headers: { 'Content-Type': 'application/json; charset=UTF-8' },
            timeout: 30000
        });
        
        console.log('--- MoMo Response ---');
        console.log(JSON.stringify(response.data, null, 2));

        if (response.data && response.data.payUrl) {
            transactions.push({ orderId, packageId, status: 'pending', provider: 'momo' });
            res.json({ paymentUrl: response.data.payUrl });
        } else {
            res.status(400).json({ message: response.data.message || 'MoMo rejected the request' });
        }
    } catch (error) {
        console.error('--- MoMo API Error ---');
        if (error.response) {
            console.error(error.response.data);
            res.status(400).json({ message: error.response.data.message });
        } else {
            console.error(error.message);
            res.status(500).json({ message: 'Connection to MoMo failed' });
        }
    }
});

app.post('/api/payment/momo_ipn', (req, res) => {
    const { orderId, resultCode } = req.body;
    const trans = transactions.find(t => t.orderId === orderId);
    if (trans && resultCode === 0 && trans.status !== 'success') {
        const pkg = PACKAGES.find(p => p.id === trans.packageId);
        userBalance += pkg.amount;
        trans.status = 'success';
    }
    res.status(204).send();
});

// --- Dev Mode Logic ---
app.post('/api/payment/dev_recharge', (req, res) => {
    const { packageId } = req.body;
    const pkg = DEV_PACKAGES.find(p => p.id === packageId);
    
    if (!pkg) return res.status(404).json({ message: 'Gói Dev không tồn tại' });

    if (packageId === 'dev_success') {
        userBalance += pkg.amount;
        transactions.push({ orderId: 'DEV' + Date.now(), packageId, status: 'success', provider: 'dev' });
        return res.json({ success: true, message: 'Nạp vô hạn tiền thành công!', balance: userBalance });
    } else {
        transactions.push({ orderId: 'DEV' + Date.now(), packageId, status: 'failed', provider: 'dev' });
        return res.json({ success: false, message: 'Thanh toán thất bại (Mô phỏng lỗi không có tiền).' });
    }
});

// Utils
function sortObject(obj) {
	let sorted = {};
	let str = [];
	let key;
	for (key in obj){
		if (obj.hasOwnProperty(key)) {
		str.push(encodeURIComponent(key));
		}
	}
	str.sort();
    for (key = 0; key < str.length; key++) {
        sorted[str[key]] = encodeURIComponent(obj[str[key]]).replace(/%20/g, "+");
    }
    return sorted;
}

// ============================================================
// SCORE & SAVE SYSTEM ENDPOINTS (in-memory stub)
// TODO: Thay bằng DB thực (MongoDB / PostgreSQL) khi production
// ============================================================

// In-memory storage cho scores và player data
const playerScores    = {};  // { playerId: { songKey: { score, accuracy, rank, ... } } }
const playerSettings  = {};  // { playerId: PlayerSettings }
const playerProgress  = {};  // { playerId: { songKey: { isUnlocked, isPurchased } } }

// ── Score Endpoints ──────────────────────────────────────────

/**
 * POST /api/scores
 * Body: { playerId, songKey, difficulty, score, accuracy, rank, timestamp }
 * Lưu điểm cao nhất. Nếu điểm mới thấp hơn → bỏ qua (không override).
 */
app.post('/api/scores', (req, res) => {
    const { playerId, songKey, score, accuracy, rank, difficulty, timestamp } = req.body;

    if (!playerId || !songKey || score === undefined) {
        return res.status(400).json({ message: 'Thiếu playerId, songKey hoặc score.' });
    }

    if (!playerScores[playerId])  playerScores[playerId] = {};

    const existing = playerScores[playerId][songKey];
    const isNewHighScore = !existing || score > existing.score;

    if (isNewHighScore) {
        playerScores[playerId][songKey] = { songKey, score, accuracy, rank, difficulty, updatedAt: timestamp || Date.now() };
        console.log(`[Score] New high score for ${playerId}/${songKey}: ${score} (${rank})`);
    }

    res.json({ success: true, isNewHighScore });
});

/**
 * GET /api/scores/:songKey
 * Lấy top 10 điểm cao nhất của một bài từ tất cả người dùng.
 * Dùng cho bảng xếp hạng cloud (tương lai).
 */
app.get('/api/scores/:songKey', (req, res) => {
    const { songKey } = req.params;

    const allEntries = [];
    for (const [pid, songs] of Object.entries(playerScores)) {
        if (songs[songKey]) {
            allEntries.push({ playerId: pid, ...songs[songKey] });
        }
    }

    const top10 = allEntries
        .sort((a, b) => b.score - a.score || b.accuracy - a.accuracy)
        .slice(0, 10);

    res.json({ songKey, leaderboard: top10 });
});

// ── Player Data Endpoints ────────────────────────────────────

/**
 * GET /api/player/data
 * Header: X-Player-Id: <playerId>
 * Trả về toàn bộ dữ liệu save của người chơi (để download khi login thiết bị mới).
 */
app.get('/api/player/data', (req, res) => {
    const playerId = req.headers['x-player-id'];
    if (!playerId) return res.status(401).json({ message: 'Thiếu X-Player-Id header.' });

    const songs    = Object.values(playerScores[playerId]   || {});
    const settings = playerSettings[playerId] || {};
    const progress = playerProgress[playerId] || {};

    res.json({
        playerId,
        songs,
        settings,
        progress,
        lastSyncedAt: Date.now()
    });
});

/**
 * POST /api/player/settings
 * Header: X-Player-Id: <playerId>
 * Body: PlayerSettings JSON
 */
app.post('/api/player/settings', (req, res) => {
    const playerId = req.headers['x-player-id'];
    if (!playerId) return res.status(401).json({ message: 'Thiếu X-Player-Id header.' });

    playerSettings[playerId] = req.body;
    console.log(`[Settings] Saved settings for ${playerId}`);
    res.json({ success: true });
});

/**
 * POST /api/player/progress
 * Header: X-Player-Id: <playerId>
 * Body: { songKey, isUnlocked, isPurchased }
 */
app.post('/api/player/progress', (req, res) => {
    const playerId = req.headers['x-player-id'];
    if (!playerId) return res.status(401).json({ message: 'Thiếu X-Player-Id header.' });

    const { songKey, isUnlocked, isPurchased } = req.body;
    if (!songKey) return res.status(400).json({ message: 'Thiếu songKey.' });

    if (!playerProgress[playerId]) playerProgress[playerId] = {};
    playerProgress[playerId][songKey] = { isUnlocked, isPurchased, updatedAt: Date.now() };

    console.log(`[Progress] ${playerId}/${songKey}: unlocked=${isUnlocked}`);
    res.json({ success: true });
});

/**
 * POST /api/sync
 * Header: X-Player-Id: <playerId>
 * Body: { operations: [{ operationType, payload }] }
 * Batch sync — gửi nhiều operations 1 lần (dùng khi flush SyncQueue).
 */
app.post('/api/sync', async (req, res) => {
    const playerId = req.headers['x-player-id'];
    if (!playerId) return res.status(401).json({ message: 'Thiếu X-Player-Id header.' });

    const { operations } = req.body;
    if (!Array.isArray(operations)) return res.status(400).json({ message: 'operations phải là array.' });

    const results = [];

    for (const op of operations) {
        try {
            const payload = JSON.parse(op.payload || '{}');

            if (op.operationType === 'upload_score') {
                if (!playerScores[playerId]) playerScores[playerId] = {};
                const existing = playerScores[playerId][payload.songKey];
                if (!existing || payload.score > existing.score) {
                    playerScores[playerId][payload.songKey] = payload;
                }
                results.push({ type: op.operationType, success: true });
            } else if (op.operationType === 'upload_settings') {
                playerSettings[playerId] = payload;
                results.push({ type: op.operationType, success: true });
            } else if (op.operationType === 'upload_progress') {
                if (!playerProgress[playerId]) playerProgress[playerId] = {};
                playerProgress[playerId][payload.songKey] = payload;
                results.push({ type: op.operationType, success: true });
            } else {
                results.push({ type: op.operationType, success: false, error: 'Unknown op type' });
            }
        } catch (e) {
            results.push({ type: op.operationType, success: false, error: e.message });
        }
    }

    console.log(`[Sync] Batch sync for ${playerId}: ${results.filter(r => r.success).length}/${results.length} succeeded.`);
    res.json({ success: true, results });
});

app.listen(PORT, () => {
    console.log(`Backend is running on port ${PORT}`);
});
