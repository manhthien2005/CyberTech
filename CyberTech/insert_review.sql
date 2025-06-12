
WITH RandomUsers AS (
    SELECT TOP 20 UserID
    FROM [cybertech].[dbo].[Users]
    ORDER BY NEWID()
)
INSERT INTO [cybertech].[dbo].[Reviews] ([UserID], [ProductID], [Rating], [Comment], [CreatedAt])
SELECT 
    UserID,
    21, -- ProductID cho ViewSonic XG2409
    CASE 
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) % 5 = 1 THEN 4
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) % 5 = 2 THEN 3
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) % 5 = 3 THEN 4
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) % 5 = 4 THEN 3
        ELSE 4
    END AS Rating,
    CASE 
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 1 THEN N'Màn hình chơi game mượt nhờ 180Hz và Gsync, nhưng viền hơi dày làm tổng thể kém sang.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 2 THEN N'Màu sắc của XG2409 khá chuẩn, nhưng độ sáng tối đa hơi thấp khi dùng trong phòng nhiều ánh sáng.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 3 THEN N'Gsync hoạt động tốt, không bị xé hình, nhưng chân đế chiếm nhiều không gian trên bàn.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 4 THEN N'Chất lượng hình ảnh ổn trong tầm giá, nhưng loa tích hợp quá yếu, gần như không dùng được.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 5 THEN N'Tần số quét 180Hz cho trải nghiệm chơi game tuyệt vời, nhưng góc nhìn IPS chưa thực sự ấn tượng.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 6 THEN N'Màn hình này build chắc chắn, màu sắc đẹp, nhưng cáp đi kèm hơi ngắn, hơi bất tiện.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 7 THEN N'Chơi FPS rất mượt, Gsync hỗ trợ tốt, nhưng cần chỉnh màu sắc ban đầu để đạt hiệu quả tốt hơn.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 8 THEN N'ViewSonic XG2409 có thiết kế ổn, nhưng mình gặp chút hở sáng ở góc, không quá nghiêm trọng.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 9 THEN N'Hiệu năng gaming ấn tượng, nhưng giá hơi cao so với một số màn hình cùng phân khúc.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 10 THEN N'Màn hình đáp ứng tốt nhu cầu chơi game và làm việc, nhưng nút điều khiển hơi khó thao tác.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 11 THEN N'Màu sắc và độ nét rất tốt, nhưng mình mong chân đế có thể điều chỉnh độ cao linh hoạt hơn.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 12 THEN N'180Hz và Gsync là điểm cộng lớn, nhưng mình thấy viền màn hình hơi thô, không hiện đại.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 13 THEN N'Màn hình này cho trải nghiệm chơi game ổn, nhưng loa tích hợp gần như vô dụng.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 14 THEN N'Chất lượng tổng thể tốt, nhưng mình gặp vấn đề nhỏ với cổng HDMI, phải kiểm tra lại.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 15 THEN N'ViewSonic làm tốt ở tần số quét và Gsync, nhưng thiết kế tổng thể chưa thực sự nổi bật.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 16 THEN N'Màn hình phù hợp cho game thủ, nhưng cần cải thiện góc nhìn khi ngồi lệch một chút.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 17 THEN N'Hiệu năng chơi game đáng khen, nhưng mình nghĩ giá có thể cạnh tranh hơn.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 18 THEN N'Màu sắc và tốc độ phản hồi tốt, nhưng chân đế hơi cồng kềnh so với bàn nhỏ.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 19 THEN N'Gsync và 180Hz hoạt động mượt mà, nhưng mình mong có thêm cổng USB cho tiện ích.'
        WHEN ROW_NUMBER() OVER (ORDER BY UserID) = 20 THEN N'Màn hình này đáp ứng tốt nhu cầu gaming, nhưng thiết kế viền và nút bấm cần cải thiện.'
    END AS Comment,
    DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY UserID), '2025-06-01') AS CreatedAt
FROM RandomUsers;


