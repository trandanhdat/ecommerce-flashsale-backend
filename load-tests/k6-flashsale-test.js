import http from 'k6/http';
import { check, sleep } from 'k6';

// =========================================================================
// HƯỚNG DẪN CHẠY TEST TẢI (K6)
// =========================================================================
// 1. Cài đặt k6 (nếu chưa có): https://k6.io/docs/getting-started/installation/
// 2. Thay GUID của FlashSaleItem và Token JWT ở bên dưới.
// 3. Chạy lệnh: k6 run load-tests/k6-flashsale-test.js
// =========================================================================

export const options = {
    vus: 500,           // 500 virtual users cùng lúc
    duration: '15s',    // Bắn phá liên tục trong 15 giây
};

export default function () {
    // Sửa PORT này nếu WebAPI của bạn chạy ở port khác
    const url = 'http://localhost:5044/api/flash-sale-orders';

    // TODO: THAY BẰNG ID CỦA FlashSaleItem (Đã được Activate bởi Hangfire)
    const flashSaleItemId = '2F81EDD0-9126-4D67-BEA2-6C6E747A0CED';

    // Dùng mã VU (Virtual User) để giả lập 500 người dùng có UserId (Guid) khác nhau
    const vuString = __VU.toString().padStart(12, '0');
    const fakeUserId = `11111111-2222-3333-4444-${vuString}`;

    const payload = JSON.stringify({
        flashSaleItemId: flashSaleItemId,
        quantity: 1
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'X-Cheat-UserId': fakeUserId // Gửi ID ảo cho API thay vì dùng JWT Token
        },
    };

    const res = http.post(url, payload, params);

    // Kiểm tra kết quả trả về
    check(res, {
        'is status 200 (Success)': (r) => r.status === 200,
        'is status 400 (Bad Request)': (r) => r.status === 400,
        'is status 409 (Conflict/Hết hàng/Giữ chỗ)': (r) => r.status === 409,
    });
}
