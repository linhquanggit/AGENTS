# Knowledge: Unity UI & UX Patterns

Best practices for building efficient and scalable UI using UGUI or UI Toolkit.

## Performance
- **Canvas Splitting**: Split the UI into multiple Canvases. Moving elements should be on a separate Canvas from static ones to avoid full-canvas dirtying.
- **Graphic Raycasters**: Disable `GraphicRaycaster` on Canvases that don't need user interaction.
- **Raycast Target**: Uncheck "Raycast Target" on all Text and Images that don't need to be clickable.
- **Pixel Perfect**: Disable "Pixel Perfect" on dynamic Canvases as it adds overhead.

## Scalability
- **Popup Management**: Use a `PopupBase` class (per `Conventions.md`) and a `UIManager` to handle stack, layering, and animations.
- **UI Prefabs**: Build UI elements as reusable prefabs (e.g., `CustomButton`, `RewardItem`).
- **Localization**: Use `I2 Localization` (per `Conventions.md`) for all text elements.

## Workflow
- **Naming**: UI elements should be named logically (e.g., `btn_Claim`, `txt_Title`, `img_Icon`).
- **Anchors**: Always use Anchors and Pivots correctly to ensure UI scales across different aspect ratios (Mobile first).
