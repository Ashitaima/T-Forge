namespace TForge.Common
{
    /// <summary>
    /// Ознаки для моделей прогнозу — дзеркало ml/chess/features.py. Порядок
    /// колонок і є контрактом з ONNX-графом: rating_diff, rating_mean, потім
    /// one-hot контролю часу в порядку TimeControls.All. Розбіжність не падає,
    /// а дає впевнено хибні ймовірності — тому її ловить ChessModelParityTests.
    /// </summary>
    public static class ChessFeatures
    {
        public const int Count = 6;

        public static float[] Vector(int white, int black, string timeControl)
        {
            if (!TimeControls.IsValid(timeControl))
            {
                throw new ArgumentException($"Unknown time control: {timeControl}", nameof(timeControl));
            }

            var vector = new float[Count];
            vector[0] = white - black;
            vector[1] = (white + black) / 2f;

            for (var i = 0; i < TimeControls.All.Length; i++)
            {
                vector[2 + i] = TimeControls.All[i] == timeControl ? 1f : 0f;
            }

            return vector;
        }
    }
}
