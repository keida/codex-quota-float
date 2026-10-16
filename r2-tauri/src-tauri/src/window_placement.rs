#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum Edge {
    Left,
    Right,
    Top,
    Bottom,
}

#[derive(Clone, Copy, Debug, Default, PartialEq, Eq)]
pub(crate) enum Anchor {
    #[default]
    Initial,
    Free { x_dip: i32, y_dip: i32 },
    Docked { edge: Edge, inset_dip: u32, cross_axis_dip: i32 },
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) struct Rect {
    pub(crate) x: i32,
    pub(crate) y: i32,
    pub(crate) width: u32,
    pub(crate) height: u32,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) struct WorkArea {
    pub(crate) x: i32,
    pub(crate) y: i32,
    pub(crate) width: u32,
    pub(crate) height: u32,
}

pub(crate) fn fit_surface_size(area: WorkArea, width: u32, height: u32) -> (u32, u32) {
    (width.min(area.width.max(1)), height.min(area.height.max(1)))
}

pub(crate) fn position_for_surface(
    area: WorkArea,
    width: u32,
    height: u32,
    anchor: Anchor,
    scale: f64,
) -> (Rect, Anchor) {
    let scale = valid_scale(scale);
    match anchor {
        Anchor::Initial => {
            let anchor = Anchor::Docked { edge: Edge::Right, inset_dip: 20, cross_axis_dip: 20 };
            (dock(area, width, height, anchor, scale), anchor)
        }
        Anchor::Free { x_dip, y_dip } => {
            let rect = Rect {
                x: (area.x as i64 + dip_to_px(x_dip, scale)) as i32,
                y: (area.y as i64 + dip_to_px(y_dip, scale)) as i32,
                width,
                height,
            };
            (clamp(rect, area), anchor)
        }
        Anchor::Docked { .. } => (dock(area, width, height, anchor, scale), anchor),
    }
}

pub(crate) fn finish_drag(current: Rect, area: WorkArea, scale: f64) -> (Rect, Anchor) {
    let scale = valid_scale(scale);
    let clamped = clamp(current, area);
    let distances = [
        (Edge::Left, (clamped.x as i64 - area.x as i64).abs()),
        (Edge::Right, (right(area) - right_rect(clamped)).abs()),
        (Edge::Top, (clamped.y as i64 - area.y as i64).abs()),
        (Edge::Bottom, (bottom(area) - bottom_rect(clamped)).abs()),
    ];
    let nearest = distances.into_iter().min_by_key(|(_, distance)| *distance).unwrap();
    if nearest.1 <= (24.0 * scale).round() as i64 {
        let cross_axis_dip = match nearest.0 {
            Edge::Left | Edge::Right => px_to_dip(clamped.y as i64 - area.y as i64, scale),
            Edge::Top | Edge::Bottom => px_to_dip(clamped.x as i64 - area.x as i64, scale),
        };
        let anchor = Anchor::Docked { edge: nearest.0, inset_dip: 0, cross_axis_dip };
        (dock(area, clamped.width, clamped.height, anchor, scale), anchor)
    } else {
        let anchor = Anchor::Free {
            x_dip: px_to_dip(clamped.x as i64 - area.x as i64, scale),
            y_dip: px_to_dip(clamped.y as i64 - area.y as i64, scale),
        };
        (clamped, anchor)
    }
}

fn valid_scale(scale: f64) -> f64 {
    if scale.is_finite() && scale > 0.0 { scale } else { 1.0 }
}

fn dip_to_px(value: i32, scale: f64) -> i64 { (value as f64 * valid_scale(scale)).round() as i64 }
fn px_to_dip(value: i64, scale: f64) -> i32 { (value as f64 / valid_scale(scale)).round() as i32 }
fn right(area: WorkArea) -> i64 { area.x as i64 + area.width as i64 }
fn bottom(area: WorkArea) -> i64 { area.y as i64 + area.height as i64 }
fn right_rect(rect: Rect) -> i64 { rect.x as i64 + rect.width as i64 }
fn bottom_rect(rect: Rect) -> i64 { rect.y as i64 + rect.height as i64 }

fn clamp(rect: Rect, area: WorkArea) -> Rect {
    let max_x = (right(area) - rect.width as i64).max(area.x as i64);
    let max_y = (bottom(area) - rect.height as i64).max(area.y as i64);
    Rect {
        x: (rect.x as i64).clamp(area.x as i64, max_x) as i32,
        y: (rect.y as i64).clamp(area.y as i64, max_y) as i32,
        ..rect
    }
}

fn dock(area: WorkArea, width: u32, height: u32, anchor: Anchor, scale: f64) -> Rect {
    let Anchor::Docked { edge, inset_dip, cross_axis_dip } = anchor else { unreachable!() };
    let inset = dip_to_px(inset_dip as i32, scale);
    let cross_axis = dip_to_px(cross_axis_dip, scale);
    let mut rect = Rect { x: area.x, y: area.y, width, height };
    match edge {
        Edge::Left => { rect.x = (area.x as i64 + inset) as i32; rect.y = (area.y as i64 + cross_axis) as i32; }
        Edge::Right => { rect.x = (right(area) - width as i64 - inset) as i32; rect.y = (area.y as i64 + cross_axis) as i32; }
        Edge::Top => { rect.x = (area.x as i64 + cross_axis) as i32; rect.y = (area.y as i64 + inset) as i32; }
        Edge::Bottom => { rect.x = (area.x as i64 + cross_axis) as i32; rect.y = (bottom(area) - height as i64 - inset) as i32; }
    }
    clamp(rect, area)
}

#[cfg(test)]
mod tests {
    use super::*;

    fn area() -> WorkArea { WorkArea { x: -1920, y: -120, width: 1920, height: 1080 } }

    #[test]
    fn initializes_at_dpi_scaled_top_right_and_returns_to_compact_anchor() {
        let area = area();
        let (small, anchor) = position_for_surface(area, 192, 192, Anchor::Initial, 2.0);
        assert_eq!((small.x, small.y), (-232, -80));
        let (large, _) = position_for_surface(area, 680, 888, anchor, 2.0);
        assert_eq!((large.x, large.y), (-720, -80));
        let (settings, _) = position_for_surface(area, 680, 1080, anchor, 2.0);
        assert_eq!(settings.y, -120);
        let (returned, _) = position_for_surface(area, 192, 192, anchor, 2.0);
        assert_eq!((returned.x, returned.y), (small.x, small.y));
    }

    #[test]
    fn caps_high_dpi_settings_size_to_work_area_before_positioning() {
        let area = WorkArea { x: -1920, y: -40, width: 1920, height: 1040 };
        let (width, height) = fit_surface_size(area, 680, 1080);
        assert_eq!((width, height), (680, 1040));

        let (placed, _) = position_for_surface(
            area,
            width,
            height,
            Anchor::Docked { edge: Edge::Right, inset_dip: 20, cross_axis_dip: 20 },
            2.0,
        );
        assert!(placed.x >= area.x);
        assert!(placed.y >= area.y);
        assert!(right_rect(placed) <= right(area));
        assert!(bottom_rect(placed) <= bottom(area));
    }

    #[test]
    fn snaps_all_edges_using_dpi_scaled_threshold_with_negative_origins() {
        let area = area();
        for (edge, rect, expected) in [
            (Edge::Left, Rect { x: -1897, y: 200, width: 100, height: 100 }, (-1920, 200)),
            (Edge::Right, Rect { x: -123, y: 200, width: 100, height: 100 }, (-100, 200)),
            (Edge::Top, Rect { x: -1000, y: -98, width: 100, height: 100 }, (-1000, -120)),
            (Edge::Bottom, Rect { x: -1000, y: 883, width: 100, height: 100 }, (-1000, 860)),
        ] {
            let (placed, anchor) = finish_drag(rect, area, 1.0);
            assert_eq!((placed.x, placed.y), expected);
            assert!(matches!(anchor, Anchor::Docked { edge: actual, inset_dip: 0, .. } if actual == edge));
        }
        let near_right_at_150_percent = Rect { x: -136, y: 200, width: 100, height: 100 };
        assert!(matches!(finish_drag(near_right_at_150_percent, area, 1.5).1,
            Anchor::Docked { edge: Edge::Right, inset_dip: 0, .. }));
    }

    #[test]
    fn keeps_each_docked_edge_during_surface_changes_and_clamps_orthogonal_axis() {
        let area = area();
        let cases = [
            (Edge::Left, Rect { x: -1920, y: 850, width: 100, height: 100 }, (-1920, 760)),
            (Edge::Right, Rect { x: -100, y: 850, width: 100, height: 100 }, (-300, 760)),
            (Edge::Top, Rect { x: -1800, y: -120, width: 100, height: 100 }, (-1800, -120)),
            (Edge::Bottom, Rect { x: -1800, y: 860, width: 100, height: 100 }, (-1800, 760)),
        ];
        for (edge, current, expected) in cases {
            let (_, anchor) = finish_drag(current, area, 1.0);
            assert!(matches!(anchor, Anchor::Docked { edge: actual, .. } if actual == edge));
            let (placed, same_anchor) = position_for_surface(area, 300, 200, anchor, 1.0);
            assert_eq!((placed.x, placed.y), expected);
            assert_eq!(same_anchor, anchor);
        }
    }

    #[test]
    fn preserves_free_position_when_large_surface_is_clamped_then_shrunk() {
        let area = area();
        let compact = Rect { x: -1200, y: 830, width: 100, height: 100 };
        let (_, anchor) = finish_drag(compact, area, 1.0);
        assert!(matches!(anchor, Anchor::Free { .. }));
        let (expanded, _) = position_for_surface(area, 340, 900, anchor, 1.0);
        assert_eq!(expanded.y, 60);
        let (returned, _) = position_for_surface(area, 100, 100, anchor, 1.0);
        assert_eq!((returned.x, returned.y), (compact.x, compact.y));
    }

    #[test]
    fn preserves_docked_cross_axis_when_large_surface_is_clamped_then_shrunk() {
        let area = area();
        let compact = Rect { x: -100, y: 850, width: 100, height: 100 };
        let (_, anchor) = finish_drag(compact, area, 1.0);
        assert!(matches!(anchor, Anchor::Docked { edge: Edge::Right, .. }));
        let (expanded, _) = position_for_surface(area, 340, 900, anchor, 1.0);
        assert_eq!(expanded.y, 60);
        let (returned, _) = position_for_surface(area, 100, 100, anchor, 1.0);
        assert_eq!((returned.x, returned.y), (compact.x, compact.y));
    }
}
