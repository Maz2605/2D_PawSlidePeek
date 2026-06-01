# Game Flow

Tài liệu này mô tả luồng game hiện tại theo code trong project.

Nguồn chính:
- `AppBootstrap`
- `GameAppFlowManager`
- `GameFlowManager`
- `Match3LevelManager`
- `Match3GameManager`
- `MapSubScreen`
- `GameplayScreen`
- `PausePopup`
- `WinPopup`
- `LosePopup`

## Flowchart

Phiên bản này chỉ giữ luồng mà người chơi nhìn thấy trên màn hình và các hành động họ có thể bấm.

```mermaid
flowchart TD
    A["Mở game"] --> B["Màn hình menu"]
    B --> C{"Người chơi chọn gì?"}

    C -->|Xem tab| D["Map / Rewards / Wheel / Shop"]
    D --> B

    C -->|Bấm level ở Map| E["Vào màn gameplay"]

    E --> F{"Trong lúc chơi"}
    F -->|Trượt line / tap tile| G["Board cập nhật, hiệu ứng chạy"]
    G --> H{"Đã thắng chưa?"}
    H -->|Chưa| I{"Hết lượt chưa?"}
    I -->|Chưa| F
    I -->|Rồi| J["Lose popup"]
    H -->|Rồi| K["Win popup"]

    F -->|Dùng charged ability| L["Chọn ô / chọn combo rồi áp dụng"]
    L --> G

    F -->|Bấm pause| M["Pause popup"]
    M -->|Resume| F
    M -->|Restart| E
    M -->|Quit| B

    J -->|Repeat| E
    J -->|Home| B

    K -->|Repeat| E
    K -->|Levels| B
    K -->|Next Level| E
```

## Ghi chú

- Đây là bản đơn giản hóa theo góc nhìn người chơi, không show manager, bootstrap hay state nội bộ.
- `Next Level` trên `WinPopup` hiện đang hoạt động như `Repeat`, tức là quay lại gameplay của level hiện tại.
