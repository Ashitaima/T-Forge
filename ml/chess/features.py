"""The single Python definition of the chess prediction features.

Mirrored by Esport-Backend/Common/ChessFeatures.cs, EloBaseline.cs and
TimeControls.cs. The parity test (Esport-Backend.Tests/ChessModelParityTests.cs)
fails if the two drift — a drifted feature gives confident wrong probabilities
and no error, so do not change one side without the other.
"""

TIME_CONTROLS = ["Bullet", "Blitz", "Rapid", "Classical"]
FEATURE_ORDER = ["rating_diff", "rating_mean", "tc_bullet", "tc_blitz", "tc_rapid", "tc_classical"]
CLASS_ORDER = ["white", "draw", "black"]


def time_control(base_minutes: float, increment_seconds: float) -> str:
    """Lichess thresholds on the estimated game length: base + 40 moves x increment."""
    estimated = base_minutes * 60 + 40 * increment_seconds
    if estimated < 180:
        return "Bullet"
    if estimated < 480:
        return "Blitz"
    if estimated < 1500:
        return "Rapid"
    return "Classical"


def parse_increment_code(code: str) -> str:
    """'15+2' (minutes + seconds per move) -> 'Rapid'."""
    base, increment = code.split("+")
    return time_control(float(base), float(increment))


def feature_vector(white: float, black: float, tc: str) -> list[float]:
    if tc not in TIME_CONTROLS:
        raise ValueError(f"unknown time control: {tc}")
    return [float(white - black), float((white + black) / 2),
            *[1.0 if tc == t else 0.0 for t in TIME_CONTROLS]]


def elo_baseline(white: float, black: float, draw_rate: float) -> tuple[float, float, float]:
    """Elo expected score, with a constant draw share taken from the training set."""
    expected = 1.0 / (1.0 + 10 ** ((black - white) / 400.0))
    w = max(expected - draw_rate / 2, 0.0)
    b = max(1.0 - expected - draw_rate / 2, 0.0)
    total = w + draw_rate + b
    return w / total, draw_rate / total, b / total


def outcome_class(winner: str) -> int:
    return CLASS_ORDER.index(winner)
