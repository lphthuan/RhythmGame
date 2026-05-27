import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { 
  CreditCard, 
  Wallet, 
  Zap, 
  ShieldCheck, 
  Coins, 
  ChevronRight,
  X,
  Info,
  CheckCircle2,
  AlertCircle,
  Crown
} from 'lucide-react';

const API_BASE = 'http://localhost:5000/api';

function App() {
  const [packages, setPackages] = useState([]);
  const [devPackages, setDevPackages] = useState([]);
  const [balance, setBalance] = useState(0);
  const [isDevMode, setIsDevMode] = useState(false);
  const [devClickCount, setDevClickCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadingText, setLoadingText] = useState('Đang xử lý...');
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  
  // Custom Toast State
  const [toast, setToast] = useState({ show: false, message: '', type: 'success' });

  const showToast = (message, type = 'success') => {
    setToast({ show: true, message, type });
    setTimeout(() => setToast({ show: false, message: '', type: 'success' }), 4000);
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchBalance, 3000);
    
    // Check for payment result in URL
    const urlParams = new URLSearchParams(window.location.search);
    const status = urlParams.get('paymentStatus'); 
    const momoResult = urlParams.get('resultCode'); 

    if (status || momoResult) {
      if (status === 'success' || momoResult === '0') {
        showToast('Thanh toán thành công! Cảm ơn bạn đã ủng hộ.', 'success');
      } else if (status === 'failed' || (momoResult && momoResult !== '0')) {
        showToast('Giao dịch thất bại hoặc đã bị hủy.', 'error');
      } else if (status === 'error') {
        showToast('Lỗi xử lý: ' + urlParams.get('message'), 'error');
      }
      window.history.replaceState({}, document.title, window.location.pathname);
      fetchBalance();
      fetchData();
    }

    return () => clearInterval(interval);
  }, [isDevMode]);

  const fetchData = async () => {
    try {
      const res = await axios.get(`${API_BASE}/packages?dev=${isDevMode}`);
      setPackages(res.data.packages);
      setDevPackages(res.data.devPackages);
      fetchBalance();
    } catch (err) {
      console.error('Fetch error:', err);
    }
  };

  const fetchBalance = async () => {
    try {
      const res = await axios.get(`${API_BASE}/user/balance`);
      setBalance(res.data.balance);
    } catch (err) {}
  };

  const handleDevToggle = () => {
    setDevClickCount(prev => {
      if (prev + 1 >= 5) {
        setIsDevMode(!isDevMode);
        showToast(isDevMode ? 'Đã tắt Dev Mode' : 'Đã bật Dev Mode!', 'success');
        return 0;
      }
      return prev + 1;
    });
  };

  const initiatePayment = async (packageId, provider) => {
    setLoading(true);
    setLoadingText('Đang kết nối cổng thanh toán...');
    try {
      let endpoint = '';
      if (provider === 'vnpay') endpoint = '/payment/vnpay';
      else if (provider === 'momo') endpoint = '/payment/momo';
      else if (provider === 'momo_atm') endpoint = '/payment/momo_atm';
      else if (provider === 'dev') endpoint = '/payment/dev_recharge';
      else if (provider === 'free') endpoint = '/payment/free';

      const res = await axios.post(`${API_BASE}${endpoint}`, { packageId });
      
      if (provider === 'dev' || provider === 'free') {
        showToast(res.data.message, res.data.success !== false ? 'success' : 'error');
        fetchBalance();
        setLoading(false);
        setShowPaymentModal(false);
      } else if (res.data.paymentUrl && res.data.paymentUrl.includes('paymentStatus=')) {
        setLoadingText('Đang xác nhận ưu đãi...');
        setTimeout(() => {
            window.location.href = res.data.paymentUrl;
        }, 1500);
      } else {
        window.location.href = res.data.paymentUrl;
      }
    } catch (err) {
      showToast('Lỗi kết nối đến máy chủ thanh toán.', 'error');
      setLoading(false);
      setShowPaymentModal(false);
    }
  };

  const renderPackage = (pkg, isDev = false) => {
    const isFree = pkg.price === 0;
    const isPopular = pkg.id === 'p3' || pkg.id === 'p4';
    const hasBonus = pkg.description.includes('+');

    return (
      <div
        key={pkg.id}
        className={`riot-card ${isDev ? 'dev-card' : ''} ${isFree ? 'free-card' : ''} ${isPopular && !isDev ? 'popular-card' : ''}`}
        onClick={() => {
          setSelectedPackage(pkg);
          if (!isDev) setShowPaymentModal(true);
          else initiatePayment(pkg.id, 'dev');
        }}
      >
        {isPopular && !isDev && <div className="popular-badge">PHỔ BIẾN</div>}
        {isFree && pkg.id === 'p0' && <div className="free-badge">TÂN THỦ</div>}
        
        <div className="riot-card-content">
          <div className="coin-icon-wrapper">
            <Coins color={isDev ? "#ff00ff" : "#c8aa6e"} size={42} strokeWidth={1.5} />
          </div>
          
          <div className="amount-display">
            <span className="amount-number">{pkg.amount.toLocaleString()}</span>
            <span className="amount-currency">RC</span>
          </div>
          
          <div className="bonus-desc">
            {hasBonus ? pkg.description : (isFree && pkg.id === 'p0' ? 'Nhận miễn phí lần đầu' : pkg.name)}
          </div>
        </div>

        <div className="price-button">
          {isFree && pkg.id === 'p0' ? (
            <div className="price-strike-group">
              <span className="price-strike">20.000đ</span>
              <span className="price-free-text">Miễn phí</span>
            </div>
          ) : isFree ? (
            'MIỄN PHÍ'
          ) : (
            `${pkg.price.toLocaleString()} đ`
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="mobile-container">
      {/* Toast Notification */}
      <div className={`toast-container ${toast.show ? 'toast-visible' : ''}`}>
        <div className={`toast ${toast.type}`}>
          {toast.type === 'success' ? <CheckCircle2 size={20} /> : <AlertCircle size={20} />}
          <span>{toast.message}</span>
        </div>
      </div>

      {/* Header */}
      <header className="header">
        <div onClick={handleDevToggle} className="logo">
          <span className="logo-accent">RHYTHM</span> STORE
        </div>
        <div className="balance-pill">
          <Coins color="#c8aa6e" size={16} />
          <span>{balance.toLocaleString()} RC</span>
        </div>
      </header>

      <main className="main-content">
        <div className="hero-banner">
          <div className="hero-content">
            <Crown color="#c8aa6e" size={32} className="hero-icon" />
            <h2>NẠP RHYTHM COINS</h2>
            <p>Sử dụng RC để mua trang phục, bản nhạc và Battle Pass!</p>
          </div>
        </div>

        <div className="section-label">GÓI NẠP TÀI KHOẢN</div>
        <div className="riot-grid">
          {packages.map(pkg => renderPackage(pkg))}
        </div>

        {isDevMode && (
          <>
            <div className="section-label dev-label">DEV TOOLS (TEST ONLY)</div>
            <div className="riot-grid">
              {devPackages.map(pkg => renderPackage(pkg, true))}
            </div>
          </>
        )}

        <footer className="footer-info">
          <div className="info-badge">
            <ShieldCheck color="#0397ab" size={16} />
            <span>Giao dịch mã hóa an toàn 100%</span>
          </div>
          <div className="info-badge">
            <Info color="#a09b8c" size={16} />
            <span>Hỗ trợ kỹ thuật: support@rhythm.game</span>
          </div>
        </footer>
      </main>

      {/* Payment Modal */}
      {showPaymentModal && (
        <div className="modal-overlay" onClick={() => setShowPaymentModal(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>CHỌN PHƯƠNG THỨC</h3>
              <button className="close-btn" onClick={() => setShowPaymentModal(false)}>
                <X size={24} />
              </button>
            </div>
            
            <div className="modal-body">
              <div className="order-summary">
                <span className="summary-label">Đang thanh toán cho:</span>
                <span className="summary-value">{selectedPackage?.name} ({selectedPackage?.amount.toLocaleString()} RC)</span>
                <div className="summary-price">
                  {selectedPackage?.price === 0 && selectedPackage?.id === 'p0' ? (
                    <>
                      <span className="strike-text">20.000đ</span>
                      <span className="free-text">0đ</span>
                    </>
                  ) : (
                    selectedPackage?.price.toLocaleString() + 'đ'
                  )}
                </div>
              </div>
              
              <button className="method-btn" onClick={() => initiatePayment(selectedPackage.id, 'vnpay')}>
                <div className="method-icon"><CreditCard size={24} color="#0397ab" /></div>
                <div className="method-details">
                  <span className="method-title">Thanh toán qua VNPay</span>
                  <span className="method-desc">Thẻ nội địa / QR Code / VNPay</span>
                </div>
                <ChevronRight size={20} color="#a09b8c" />
              </button>

              <button className="method-btn" onClick={() => initiatePayment(selectedPackage.id, 'momo')}>
                <div className="method-icon"><Wallet size={24} color="#A50064" /></div>
                <div className="method-details">
                  <span className="method-title">Ví MoMo (QR Code)</span>
                  <span className="method-desc">Quét mã nhanh chóng từ App</span>
                </div>
                <ChevronRight size={20} color="#a09b8c" />
              </button>

              <button className="method-btn" onClick={() => initiatePayment(selectedPackage.id, 'momo_atm')}>
                <div className="method-icon"><CreditCard size={24} color="#A50064" /></div>
                <div className="method-details">
                  <span className="method-title">Thẻ ATM qua MoMo</span>
                  <span className="method-desc">Nhập thông tin thẻ nội địa</span>
                </div>
                <ChevronRight size={20} color="#a09b8c" />
              </button>
            </div>
          </div>
        </div>
      )}

      {loading && (
        <div className="loader-overlay">
          <div className="loader-box">
            <div className="loader"></div>
            <span>{loadingText}</span>
          </div>
        </div>
      )}

      <style>{`
        :root {
          --bg-dark: #0a0a0c;
          --bg-card: #1e2328;
          --bg-card-hover: #2b3137;
          --primary: #c8aa6e;
          --primary-hover: #f0e6d2;
          --text-main: #f0e6d2;
          --text-muted: #a09b8c;
          --border-color: #3c3c41;
          --border-hover: #c8aa6e;
          --success: #0397ab;
          --error: #ff4655;
          --discount-bg: #e84057;
        }

        body {
          margin: 0;
          background: var(--bg-dark);
          color: var(--text-main);
          font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
          -webkit-font-smoothing: antialiased;
        }

        .mobile-container {
          max-width: 480px;
          margin: 0 auto;
          background: var(--bg-dark);
          min-height: 100vh;
          position: relative;
          box-shadow: 0 0 40px rgba(0,0,0,0.8);
          border-left: 1px solid rgba(200, 170, 110, 0.1);
          border-right: 1px solid rgba(200, 170, 110, 0.1);
        }

        /* Toast */
        .toast-container {
          position: fixed;
          top: 20px;
          left: 50%;
          transform: translateX(-50%) translateY(-150%);
          z-index: 1000;
          transition: transform 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
          pointer-events: none;
          width: 90%;
          max-width: 400px;
        }
        .toast-visible {
          transform: translateX(-50%) translateY(0);
        }
        .toast {
          display: flex;
          align-items: center;
          gap: 12px;
          padding: 14px 20px;
          border-radius: 4px;
          font-weight: 600;
          font-size: 14px;
          box-shadow: 0 10px 30px rgba(0,0,0,0.8);
          backdrop-filter: blur(10px);
          border: 1px solid;
        }
        .toast.success { background: rgba(3, 151, 171, 0.9); border-color: var(--success); color: #fff; }
        .toast.error { background: rgba(255, 70, 85, 0.9); border-color: var(--error); color: #fff; }

        /* Header */
        .header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 20px 24px;
          background: rgba(10, 10, 12, 0.95);
          backdrop-filter: blur(10px);
          position: sticky;
          top: 0;
          z-index: 10;
          border-bottom: 1px solid var(--border-color);
        }
        .logo {
          font-weight: 900;
          font-size: 18px;
          letter-spacing: 2px;
          cursor: pointer;
          user-select: none;
        }
        .logo-accent { color: var(--primary); }
        .balance-pill {
          background: rgba(200, 170, 110, 0.1);
          border: 1px solid rgba(200, 170, 110, 0.3);
          padding: 6px 14px;
          border-radius: 4px;
          display: flex;
          align-items: center;
          gap: 8px;
          color: var(--primary);
          font-weight: 700;
          font-size: 14px;
        }

        .main-content {
          padding: 24px;
          padding-bottom: 80px;
        }

        /* Hero Banner */
        .hero-banner {
          background: url('https://images.unsplash.com/photo-1542751371-adc38448a05e?auto=format&fit=crop&q=80&w=2070') center/cover;
          border-radius: 8px;
          margin-bottom: 30px;
          position: relative;
          overflow: hidden;
          border: 1px solid var(--border-color);
          box-shadow: 0 8px 20px rgba(0,0,0,0.5);
        }
        .hero-banner::before {
          content: '';
          position: absolute; inset: 0;
          background: linear-gradient(180deg, rgba(10,10,12,0.6) 0%, rgba(10,10,12,0.95) 100%);
        }
        .hero-content {
          position: relative;
          padding: 24px 20px;
          text-align: center;
          z-index: 1;
        }
        .hero-icon { margin-bottom: 8px; filter: drop-shadow(0 0 10px rgba(200,170,110,0.5)); }
        .hero-content h2 { margin: 0; font-size: 22px; font-weight: 900; color: #fff; letter-spacing: 1px; }
        .hero-content p { margin: 8px 0 0; font-size: 13px; color: var(--text-muted); }
        
        .section-label {
          font-size: 12px;
          font-weight: 800;
          color: var(--text-muted);
          margin-bottom: 16px;
          letter-spacing: 2px;
          text-transform: uppercase;
        }
        .dev-label { color: #ff00ff; margin-top: 32px; }

        /* Riot Grid & Cards */
        .riot-grid {
          display: grid;
          grid-template-columns: repeat(2, 1fr);
          gap: 12px;
        }
        .riot-card {
          background: var(--bg-card);
          border: 1px solid var(--border-color);
          border-radius: 8px;
          padding: 16px 12px;
          display: flex;
          flex-direction: column;
          align-items: center;
          cursor: pointer;
          position: relative;
          transition: all 0.2s ease;
          overflow: hidden;
        }
        .riot-card:hover {
          background: var(--bg-card-hover);
          border-color: var(--primary);
          transform: translateY(-4px);
          box-shadow: 0 8px 16px rgba(0,0,0,0.6);
        }
        .riot-card:active { transform: translateY(0); }
        
        .popular-card {
          border-color: var(--primary);
          background: linear-gradient(180deg, rgba(200,170,110,0.1) 0%, var(--bg-card) 100%);
        }
        .popular-badge {
          position: absolute;
          top: 0; left: 0; right: 0;
          background: var(--primary);
          color: #000;
          font-size: 10px;
          font-weight: 800;
          text-align: center;
          padding: 4px 0;
          letter-spacing: 1px;
          z-index: 2;
        }
        .free-badge {
          position: absolute;
          top: 0; left: 0; right: 0;
          background: var(--success);
          color: #fff;
          font-size: 10px;
          font-weight: 800;
          text-align: center;
          padding: 4px 0;
          letter-spacing: 1px;
          z-index: 2;
        }
        
        .riot-card-content {
          display: flex;
          flex-direction: column;
          align-items: center;
          width: 100%;
          flex: 1;
        }
        .coin-icon-wrapper {
          margin-top: 16px;
          margin-bottom: 8px;
          filter: drop-shadow(0 4px 8px rgba(200,170,110,0.2));
        }
        
        .amount-display {
          display: flex;
          align-items: baseline;
          gap: 4px;
        }
        .amount-number { font-size: 24px; font-weight: 900; color: #fff; }
        .amount-currency { font-size: 14px; font-weight: 700; color: var(--primary); }
        
        .bonus-desc {
          font-size: 11px;
          color: var(--discount-bg);
          font-weight: 700;
          margin-bottom: 16px;
          text-align: center;
          min-height: 16px;
        }
        
        .price-button {
          width: 100%;
          background: rgba(255,255,255,0.05);
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 4px;
          padding: 8px 0;
          text-align: center;
          font-size: 14px;
          font-weight: 700;
          color: #fff;
          transition: all 0.2s;
        }
        .riot-card:hover .price-button {
          background: var(--primary);
          color: #000;
          border-color: var(--primary);
        }

        .price-strike-group {
          display: flex;
          flex-direction: column;
          line-height: 1.2;
        }
        .price-strike { text-decoration: line-through; font-size: 11px; color: var(--text-muted); font-weight: normal; }
        .price-free-text { color: var(--success); }
        .riot-card:hover .price-free-text { color: #000; }

        /* Footer */
        .footer-info {
          margin-top: 40px;
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 12px;
          padding-top: 24px;
          border-top: 1px dashed var(--border-color);
        }
        .info-badge { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-muted); font-weight: 600; }

        /* Modal Riot Style */
        .modal-overlay {
          position: fixed; inset: 0;
          background: rgba(0,0,0,0.85);
          backdrop-filter: blur(8px);
          display: flex; align-items: flex-end; justify-content: center;
          z-index: 100;
          animation: fadeIn 0.2s ease-out;
        }
        @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

        .modal-content {
          background: var(--bg-card);
          width: 100%; max-width: 480px;
          border-top: 2px solid var(--primary);
          padding: 24px;
          box-shadow: 0 -10px 40px rgba(0,0,0,0.8);
          animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
        }
        @keyframes slideUp { from { transform: translateY(100%); } to { transform: translateY(0); } }
        
        .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
        .modal-header h3 { margin: 0; font-size: 16px; font-weight: 800; letter-spacing: 1px; color: var(--primary); }
        .close-btn { 
          background: transparent; border: 1px solid var(--border-color); color: var(--text-muted); 
          cursor: pointer; width: 32px; height: 32px; border-radius: 4px;
          display: flex; align-items: center; justify-content: center;
          transition: all 0.2s;
        }
        .close-btn:hover { background: rgba(255,255,255,0.1); color: #fff; border-color: #fff; }

        .order-summary {
          background: rgba(0,0,0,0.4);
          border-radius: 4px;
          padding: 20px;
          margin-bottom: 24px;
          text-align: center;
          border: 1px solid var(--border-color);
        }
        .summary-label { display: block; font-size: 12px; color: var(--text-muted); margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; }
        .summary-value { font-weight: 800; font-size: 16px; color: #fff; }
        .summary-price { font-size: 28px; font-weight: 900; color: var(--primary); margin-top: 12px; display: flex; justify-content: center; align-items: center; gap: 12px;}
        .strike-text { text-decoration: line-through; color: var(--text-muted); font-size: 18px; font-weight: normal; }
        .free-text { color: var(--success); }

        .method-btn {
          width: 100%;
          padding: 16px;
          border-radius: 4px;
          border: 1px solid var(--border-color);
          background: rgba(255,255,255,0.02);
          display: flex;
          align-items: center;
          gap: 16px;
          cursor: pointer;
          margin-bottom: 12px;
          transition: all 0.2s;
          text-align: left;
        }
        .method-btn:hover {
          background: rgba(255,255,255,0.05);
          border-color: var(--primary);
          transform: translateX(4px);
        }
        .method-icon {
          width: 48px; height: 48px;
          background: rgba(0,0,0,0.4);
          border-radius: 4px;
          display: flex; align-items: center; justify-content: center;
          border: 1px solid var(--border-color);
        }
        .method-details { flex: 1; }
        .method-title { display: block; font-size: 15px; font-weight: 700; color: #fff; margin-bottom: 4px; }
        .method-desc { display: block; font-size: 12px; color: var(--text-muted); }

        /* Loader Riot Style */
        .loader-overlay {
          position: fixed; inset: 0;
          background: rgba(0,0,0,0.9);
          backdrop-filter: blur(4px);
          display: flex; align-items: center; justify-content: center;
          z-index: 200;
        }
        .loader-box {
          background: transparent;
          display: flex; flex-direction: column; align-items: center; gap: 20px;
        }
        .loader-box span { font-weight: 700; color: var(--primary); letter-spacing: 1px; text-transform: uppercase; font-size: 14px; }
        .loader {
          width: 48px; height: 48px;
          border: 3px solid rgba(200, 170, 110, 0.2);
          border-top-color: var(--primary);
          border-radius: 50%;
          animation: spin 1s linear infinite;
        }
        @keyframes spin { to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
}

export default App;