import math

import pytest

from features import (CLASS_ORDER, FEATURE_ORDER, elo_baseline, feature_vector,
                      outcome_class, parse_increment_code, time_control)


@pytest.mark.parametrize("base,inc,expected", [
    (1, 0, "Bullet"), (2, 1, "Bullet"), (3, 0, "Blitz"), (5, 3, "Blitz"),
    (8, 0, "Rapid"), (15, 10, "Rapid"), (25, 0, "Classical"), (30, 20, "Classical"),
])
def test_time_control_matches_the_csharp_thresholds(base, inc, expected):
    # Ті самі випадки, що й у TimeControlsTests.cs — розбіжність тут означає,
    # що модель вчилася на інших категоріях, ніж ті, що їй подає сервер.
    assert time_control(base, inc) == expected


def test_parse_increment_code():
    assert parse_increment_code("15+2") == "Rapid"
    assert parse_increment_code("3+0") == "Blitz"


def test_feature_vector_order_and_one_hot():
    assert FEATURE_ORDER == ["rating_diff", "rating_mean", "tc_bullet", "tc_blitz", "tc_rapid", "tc_classical"]
    assert feature_vector(1600, 1500, "Blitz") == [100.0, 1550.0, 0.0, 1.0, 0.0, 0.0]


def test_feature_vector_rejects_unknown_time_control():
    with pytest.raises(ValueError):
        feature_vector(1500, 1500, "Armageddon")


def test_elo_baseline_equal_ratings_is_symmetric():
    w, d, b = elo_baseline(1500, 1500, 0.1)
    assert math.isclose(w, b) and math.isclose(d, 0.1) and math.isclose(w + d + b, 1.0)


def test_elo_baseline_huge_gap_never_goes_negative():
    w, d, b = elo_baseline(2800, 800, 0.1)
    assert b >= 0 and math.isclose(w + d + b, 1.0)


def test_outcome_class_order():
    assert CLASS_ORDER == ["white", "draw", "black"]
    assert [outcome_class(x) for x in ("white", "draw", "black")] == [0, 1, 2]
