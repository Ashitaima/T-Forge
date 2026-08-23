/**
 * Перемикач «увімкнено / вимкнено».
 *
 * Там, де вибір із двох названих станів, раніше стояли дві кнопки поруч —
 * і кожна форма малювала їх власноруч, трохи по-своєму. Це той самий вибір,
 * але одним контролом: видно і поточний стан, і те, що станів рівно два.
 *
 * Це справжній switch за ARIA, а не чекбокс із картинкою: aria-checked читає
 * програма читання з екрана, а Space і Enter працюють, бо це <button>.
 */
export const Toggle = ({
  checked,
  onChange,
  label,
  hint,
  disabled = false
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label: string;
  /** Підпис під назвою: що саме означає ввімкнений стан. */
  hint?: string;
  disabled?: boolean;
}) => (
  <div className="flex items-start justify-between gap-4">
    <span className="min-w-0">
      <span className="block text-lead text-text">{label}</span>
      {hint && <span className="muted mt-0.5 block text-micro">{hint}</span>}
    </span>

    <button
      type="button"
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
      className={`relative mt-0.5 flex h-6 w-11 shrink-0 items-center rounded-full border outline-none ring-ember/50 transition focus-visible:ring-2 disabled:cursor-not-allowed disabled:opacity-50 ${
        checked ? "border-ember/60 bg-ember/20" : "border-line bg-ink-800"
      }`}
    >
      <span
        className={`h-4 w-4 rounded-full transition-transform ${
          checked ? "translate-x-[1.4rem] bg-ember" : "translate-x-[0.2rem] bg-text-faint"
        }`}
      />
    </button>
  </div>
);

export default Toggle;
