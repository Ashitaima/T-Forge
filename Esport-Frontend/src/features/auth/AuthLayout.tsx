import { Anvil } from "lucide-react";
import type { ReactNode } from "react";

/**
 * Вхідний екран: ліворуч — клеймо кузні, праворуч — форма.
 * Єдине місце, де дисплейний шрифт звучить на повну гучність.
 */
export const AuthLayout = ({
  title,
  subtitle,
  children,
  footer
}: {
  title: string;
  subtitle: string;
  children: ReactNode;
  footer: ReactNode;
}) => (
  <div className="min-h-screen lg:grid lg:grid-cols-2">
    <aside className="relative hidden overflow-hidden border-r border-line bg-ink-950 lg:flex lg:flex-col lg:justify-between lg:p-12">
      {/* Жар горна: єдине тепле джерело світла на холодному залізі */}
      <div
        className="pointer-events-none absolute -left-20 top-1/3 h-[28rem] w-[28rem] rounded-full opacity-60 blur-3xl"
        style={{ background: "radial-gradient(circle, rgba(255,107,26,0.28), transparent 65%)" }}
        aria-hidden
      />

      <div className="relative flex items-center gap-2.5">
        <span className="flex h-8 w-8 items-center justify-center rounded-md bg-ember/15 text-ember">
          <Anvil className="h-4 w-4" />
        </span>
        <span className="font-display text-[1.0625rem] font-extrabold tracking-tight">T&#8209;Forge</span>
      </div>

      <div className="relative max-w-md">
        <h2 className="font-display text-display font-extrabold leading-[1.08] text-text">
          Турнір
          <br />
          виковується
          <br />
          <span className="text-ember">тут</span>
        </h2>
        <p className="muted mt-5 text-lead">
          Реєстрація команд, турнірна сітка на вибування та результати матчів — від першої заявки до фіналу.
        </p>
      </div>

      {/* Три кроки — це реальна послідовність роботи з турніром, а не декор */}
      <ol className="relative flex gap-8 border-t border-line-soft pt-6 font-mono text-micro text-text-faint">
        <li>
          <span className="text-ember">01</span> Заявки
        </li>
        <li>
          <span className="text-ember">02</span> Сітка
        </li>
        <li>
          <span className="text-ember">03</span> Фінал
        </li>
      </ol>
    </aside>

    <main className="flex min-h-screen items-center justify-center px-5 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-8 flex items-center gap-2.5 lg:hidden">
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-ember/15 text-ember">
            <Anvil className="h-4 w-4" />
          </span>
          <span className="font-display text-[1.0625rem] font-extrabold tracking-tight">T&#8209;Forge</span>
        </div>

        <h1 className="font-display text-h1 text-text">{title}</h1>
        <p className="muted mt-2 text-body">{subtitle}</p>

        {children}

        <p className="muted mt-6 text-body">{footer}</p>
      </div>
    </main>
  </div>
);
