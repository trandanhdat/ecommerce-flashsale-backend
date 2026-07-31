namespace FlashSale.Infrastructure.Caching
{
    public static class FlashSaleLuaScripts
    {
        // Tại sao cần Lua script thay vì cặp GET/DECR thường?
        // Nếu ta dùng lệnh GET bằng C# để kiểm tra số lượng, sau đó thấy đủ mới gọi DECR, thì sẽ gặp RACE CONDITION.
        // Giữa lúc GET và DECR, hàng ngàn thread khác có thể xen vào GET và thấy còn hàng, dẫn đến tất cả cùng gọi DECR -> OVERSELL.
        // Bằng cách gói gọn logic "Check-then-Decrement" vào 1 Lua Script (EVAL), Redis sẽ thực thi khối lệnh này 
        // một cách ATOMIC (tuyệt đối không bị gián đoạn), đảm bảo kho không bao giờ âm.
        
        public const string DecrementStockScript = @"
            local stock = redis.call('GET', KEYS[1])
            if (not stock) then
                return -1 -- StockNotInitialized
            end
            local quantity = tonumber(ARGV[1])
            local currentStock = tonumber(stock)
            if (currentStock >= quantity) then
                redis.call('DECRBY', KEYS[1], quantity)
                return currentStock - quantity -- Success (trả về số dư >= 0)
            else
                return -2 -- InsufficientStock
            end
        ";

        public const string IncrementStockScript = @"
            local stock = redis.call('GET', KEYS[1])
            if (not stock) then
                return -1 -- StockNotInitialized
            end
            local quantity = tonumber(ARGV[1])
            local newStock = redis.call('INCRBY', KEYS[1], quantity)
            return newStock
        ";
    }
}
