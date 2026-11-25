using FluentValidation;
using TForge.DTOs;

namespace TForge.Validators
{
    /// <summary>
    /// Про вміст файлу тут не сказано нічого, і це навмисно: схему знає
    /// ml/chess/datasets.py, і друга її копія на C# розійшлася б із першою.
    /// Тут — лише те, що бачить форма.
    /// </summary>
    public class UploadChessDatasetDtoValidator : AbstractValidator<UploadChessDatasetDto>
    {
        public UploadChessDatasetDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва датасету є обов'язковою")
                .Must(name => name.Trim().Length >= 3)
                    .WithMessage("Назва датасету повинна містити щонайменше 3 символи")
                .MaximumLength(100).WithMessage("Назва датасету не може перевищувати 100 символів");
        }
    }

    public class CreateTrainingRunDtoValidator : AbstractValidator<CreateTrainingRunDto>
    {
        public CreateTrainingRunDtoValidator()
        {
            RuleFor(x => x.DatasetId)
                .GreaterThan(0).WithMessage("Потрібно обрати датасет");
        }
    }
}
